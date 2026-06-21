using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace TatehamaATS_v1.Utils
{
    /// <summary>
    /// メモリリーク調査用: Bitmap 生成箇所を tag 別にカウントし、
    /// 1 秒毎に logs/memleak/alloc.csv へ累積カウントを追記する。
    /// 主犯コード行を特定したら本クラスごと削除して構わない。
    /// </summary>
    internal static class BitmapAllocTracker
    {
        private static readonly ConcurrentDictionary<string, long> _counts = new();
        private static Task? _dumper;
        private static CancellationTokenSource? _cts;
        private static string _outPath = "";

        public static void Inc(string tag)
        {
            _counts.AddOrUpdate(tag, 1, (_, v) => v + 1);
        }

        public static void Start()
        {
            if (_dumper != null) return;
            try
            {
                var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "logs", "memleak");
                Directory.CreateDirectory(dir);
                _outPath = Path.Combine(dir, $"alloc_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                File.WriteAllText(_outPath, "timestamp,tag,cumulative_count\n");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BitmapAllocTracker] init failed: {ex.Message}");
                return;
            }

            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _dumper = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(1000, token);
                        var sb = new StringBuilder();
                        var ts = DateTime.Now.ToString("HH:mm:ss");
                        foreach (var kv in _counts)
                        {
                            sb.Append(ts).Append(',').Append(kv.Key).Append(',').Append(kv.Value).Append('\n');
                        }
                        if (sb.Length > 0)
                        {
                            File.AppendAllText(_outPath, sb.ToString());
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex) { Debug.WriteLine($"[BitmapAllocTracker] dump failed: {ex.Message}"); }
                }
            }, token);
        }
    }
}
