using Microsoft.AspNetCore.SignalR.Client;
using TrainCrewAPI;

namespace TatehamaATS_v1.Network;

/// <summary>
/// 現行サーバーと同じSignalR Hubを使うATSサーバー通信。
/// </summary>
internal sealed class SignalRServerConnection : IServerConnection
{
    private readonly HubConnection _connection;

    /// <summary>
    /// SignalR Hubへの接続を初期化する。
    /// </summary>
    /// <param name="serverAddress">ATSサーバーの基底URL。</param>
    /// <param name="accessToken">認証済みアクセストークン。</param>
    public SignalRServerConnection(string serverAddress, string accessToken)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl($"{serverAddress}/hub/train?access_token={accessToken}")
            .Build();

        _connection.Closed += error => Closed.Invoke(error);
    }

    /// <summary>
    /// サーバーとの通信路が現在接続済みかどうか。
    /// </summary>
    public bool IsConnected => _connection.State == HubConnectionState.Connected;

    /// <summary>
    /// 通信路が閉じられた時に通知されるイベント。
    /// </summary>
    public event Func<Exception?, Task> Closed = _ => Task.CompletedTask;

    /// <summary>
    /// SignalR Hubへの接続を開始する。
    /// </summary>
    /// <param name="cancellationToken">キャンセル通知。</param>
    public Task StartAsync(CancellationToken cancellationToken) => _connection.StartAsync(cancellationToken);

    /// <summary>
    /// SignalR Hubとの接続を停止する。
    /// </summary>
    /// <param name="cancellationToken">キャンセル通知。</param>
    public Task StopAsync(CancellationToken cancellationToken) => _connection.StopAsync(cancellationToken);

    /// <summary>
    /// ATS状態をSignalR Hubへ送信し、制御情報を受け取る。
    /// </summary>
    /// <param name="data">サーバーに送るATS状態。</param>
    /// <param name="cancellationToken">キャンセル通知。</param>
    /// <returns>サーバーから返されたATS制御情報。</returns>
    public Task<DataFromServer> SendAtsDataAsync(DataToServer data, CancellationToken cancellationToken) =>
        _connection.InvokeAsync<DataFromServer>("SendData_ATS", data, cancellationToken);

    /// <summary>
    /// 乗務終了をSignalR Hubへ通知する。
    /// </summary>
    /// <param name="diaName">通知対象の列番。</param>
    /// <param name="cancellationToken">キャンセル通知。</param>
    public Task NotifyDriverGetsOffAsync(string diaName, CancellationToken cancellationToken) =>
        _connection.InvokeAsync<DataFromServer>("DriverGetsOff", diaName, cancellationToken);

    /// <summary>
    /// ダイヤ時刻基準の制御情報を受け取る処理を登録する。
    /// </summary>
    /// <param name="handler">受信時に実行する処理。</param>
    public void OnScheduleData(Func<DataFromServerBySchedule, Task> handler)
    {
        _connection.On<DataFromServerBySchedule>("ReceiveData", data => handler(data));
    }

    /// <summary>
    /// 信号現示情報を受け取る処理を登録する。
    /// </summary>
    /// <param name="handler">受信時に実行する処理。</param>
    public void OnSignalData(Func<List<SignalData>, Task> handler)
    {
        _connection.On<List<SignalData>>("ReceiveSignalData", signalData => handler(signalData));
    }

    /// <summary>
    /// SignalR Hub接続を破棄する。
    /// </summary>
    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
