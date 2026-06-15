namespace TatehamaATS_v1.Network;

/// <summary>
/// SignalRを使うATSサーバー通信路を生成する工場。
/// </summary>
internal sealed class SignalRServerConnectionFactory : IServerConnectionFactory
{
    private readonly string _serverAddress;

    /// <summary>
    /// SignalR通信路の生成に必要なサーバーアドレスを保持する。
    /// </summary>
    /// <param name="serverAddress">ATSサーバーの基底URL。</param>
    public SignalRServerConnectionFactory(string serverAddress)
    {
        _serverAddress = serverAddress;
    }

    /// <summary>
    /// 認証済みアクセストークンを使ってSignalR通信路を生成する。
    /// </summary>
    /// <param name="accessToken">サーバー接続に使うアクセストークン。</param>
    /// <returns>SignalR Hub用の通信路。</returns>
    public IServerConnection Create(string accessToken) => new SignalRServerConnection(_serverAddress, accessToken);
}
