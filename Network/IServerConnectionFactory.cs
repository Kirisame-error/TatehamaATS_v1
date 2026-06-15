namespace TatehamaATS_v1.Network;

/// <summary>
/// ATSサーバーとの通信路を生成する工場。
/// </summary>
internal interface IServerConnectionFactory
{
    /// <summary>
    /// 認証済みアクセストークンを使って通信路を生成する。
    /// </summary>
    /// <param name="accessToken">サーバー接続に使うアクセストークン。</param>
    /// <returns>ATSサーバー用の通信路。</returns>
    IServerConnection Create(string accessToken);
}
