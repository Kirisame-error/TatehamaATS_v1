using TrainCrewAPI;

namespace TatehamaATS_v1.Network;

/// <summary>
/// ATSサーバーとの入出力手段を表す通信境界。
/// </summary>
internal interface IServerConnection : IAsyncDisposable
{
    /// <summary>
    /// サーバーとの通信路が現在接続済みかどうか。
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 通信路が閉じられた時に通知されるイベント。
    /// </summary>
    event Func<Exception?, Task> Closed;

    /// <summary>
    /// サーバーへの接続を開始する。
    /// </summary>
    /// <param name="cancellationToken">キャンセル通知。</param>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// サーバーとの接続を停止する。
    /// </summary>
    /// <param name="cancellationToken">キャンセル通知。</param>
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// ATS状態をサーバーに送信し、制御情報を受け取る。
    /// </summary>
    /// <param name="data">サーバーに送るATS状態。</param>
    /// <param name="cancellationToken">キャンセル通知。</param>
    /// <returns>サーバーから返されたATS制御情報。</returns>
    Task<DataFromServer> SendAtsDataAsync(DataToServer data, CancellationToken cancellationToken);

    /// <summary>
    /// 乗務終了をサーバーに通知する。
    /// </summary>
    /// <param name="diaName">通知対象の列番。</param>
    /// <param name="cancellationToken">キャンセル通知。</param>
    Task NotifyDriverGetsOffAsync(string diaName, CancellationToken cancellationToken);

    /// <summary>
    /// ダイヤ時刻基準の制御情報を受け取る処理を登録する。
    /// </summary>
    /// <param name="handler">受信時に実行する処理。</param>
    void OnScheduleData(Func<DataFromServerBySchedule, Task> handler);

    /// <summary>
    /// 信号現示情報を受け取る処理を登録する。
    /// </summary>
    /// <param name="handler">受信時に実行する処理。</param>
    void OnSignalData(Func<List<SignalData>, Task> handler);
}
