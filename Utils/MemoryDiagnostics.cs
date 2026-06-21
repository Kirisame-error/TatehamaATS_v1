using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace TatehamaATS_v1.Utils
{
    /// <summary>
    /// メモリリーク調査用の常時ロガー。
    /// プロセスのWorkingSet/PrivateBytes/GCヒープ/GDI・USERハンドル数を定期的に
    /// logs/memory_yyyyMMdd_HHmmss.log に書き出す。
    /// しきい値を超えた場合は警告行を追加し、コンソールにも出す。
    /// </summary>
    internal static class MemoryDiagnostics
    {
        private const int IntervalMs = 10_000;       // 10秒間隔
        private const long WarnWorkingSetBytes = 1L * 1024 * 1024 * 1024; // 1GB
        private const long FatalWorkingSetBytes = 4L * 1024 * 1024 * 1024; // 4GB (異常検知)

        private static string? _logPath;
        private static long _lastWarnLevel = 0;
        private static readonly object _sync = new();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);
        private const uint GR_GDIOBJECTS = 0;
        private const uint GR_USEROBJECTS = 1;

        internal static void Start()
        {
            try
            {
                var dir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                Directory.CreateDirectory(dir);
                var ts = DateTimeUtils.GetNowJst().ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                _logPath = Path.Combine(dir, $"memory_{ts}_{Environment.ProcessId}.log");

                WriteHeader();

                Task.Run(LoopAsync);
            }
            catch
            {
                // 診断機能はアプリ本体を絶対に止めない
            }
        }

        private static async Task LoopAsync()
        {
            while (true)
            {
                try
                {
                    Snapshot(false);
                }
                catch { }
                await Task.Delay(IntervalMs);
            }
        }

        /// <summary>
        /// 任意のタイミングで呼び出してスナップショットを取る。
        /// </summary>
        internal static void Snapshot(bool forceMarker, string? note = null)
        {
            if (_logPath == null) return;

            using var proc = Process.GetCurrentProcess();
            proc.Refresh();

            long ws = proc.WorkingSet64;
            long priv = proc.PrivateMemorySize64;
            long paged = proc.PagedMemorySize64;
            long virtualMem = proc.VirtualMemorySize64;
            int handles = proc.HandleCount;
            int threads = proc.Threads.Count;

            var gcInfo = GC.GetGCMemoryInfo();
            long gcHeap = GC.GetTotalMemory(false);
            long gen0 = GC.CollectionCount(0);
            long gen1 = GC.CollectionCount(1);
            long gen2 = GC.CollectionCount(2);

            uint gdi = 0, user = 0;
            try
            {
                gdi = GetGuiResources(proc.Handle, GR_GDIOBJECTS);
                user = GetGuiResources(proc.Handle, GR_USEROBJECTS);
            }
            catch { }

            var sb = new StringBuilder();
            sb.Append(DateTimeUtils.GetNowJst().ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
            sb.Append('\t');
            sb.Append("WS=").Append(Mb(ws)).Append("MB");
            sb.Append('\t');
            sb.Append("Priv=").Append(Mb(priv)).Append("MB");
            sb.Append('\t');
            sb.Append("Paged=").Append(Mb(paged)).Append("MB");
            sb.Append('\t');
            sb.Append("Virtual=").Append(Mb(virtualMem)).Append("MB");
            sb.Append('\t');
            sb.Append("GCHeap=").Append(Mb(gcHeap)).Append("MB");
            sb.Append('\t');
            sb.Append("GCTotalCommitted=").Append(Mb(gcInfo.TotalCommittedBytes)).Append("MB");
            sb.Append('\t');
            sb.Append("Gen0/1/2=").Append(gen0).Append('/').Append(gen1).Append('/').Append(gen2);
            sb.Append('\t');
            sb.Append("Handles=").Append(handles);
            sb.Append('\t');
            sb.Append("Threads=").Append(threads);
            sb.Append('\t');
            sb.Append("GDI=").Append(gdi);
            sb.Append('\t');
            sb.Append("USER=").Append(user);
            if (!string.IsNullOrEmpty(note))
            {
                sb.Append('\t');
                sb.Append("Note=").Append(note);
            }

            long level = 0;
            if (ws >= FatalWorkingSetBytes) level = 2;
            else if (ws >= WarnWorkingSetBytes) level = 1;

            if (level > 0 && (forceMarker || level > _lastWarnLevel))
            {
                sb.Append("\t*** WARN level=").Append(level).Append(" ***");
                _lastWarnLevel = level;
            }

            AppendLine(sb.ToString());
        }

        private static double Mb(long bytes) => Math.Round(bytes / 1024.0 / 1024.0, 1);

        private static void WriteHeader()
        {
            using var proc = Process.GetCurrentProcess();
            var sb = new StringBuilder();
            sb.AppendLine("# MemoryDiagnostics log");
            sb.AppendLine($"# Process: {proc.ProcessName} (PID={proc.Id})");
            sb.AppendLine($"# Machine: {Environment.MachineName}  OS={Environment.OSVersion}  64bit={Environment.Is64BitOperatingSystem}");
            sb.AppendLine($"# CLR: {Environment.Version}  ProcArch={RuntimeInformation.ProcessArchitecture}");
            sb.AppendLine($"# Start(JST): {DateTimeUtils.GetNowJst():yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("# Columns: time\\tWS\\tPriv\\tPaged\\tVirtual\\tGCHeap\\tGCTotalCommitted\\tGen0/1/2\\tHandles\\tThreads\\tGDI\\tUSER");
            AppendLine(sb.ToString());
        }

        private static void AppendLine(string line)
        {
            if (_logPath == null) return;
            lock (_sync)
            {
                try
                {
                    File.AppendAllText(_logPath, line + Environment.NewLine, Encoding.UTF8);
                }
                catch { }
            }
        }
    }
}
