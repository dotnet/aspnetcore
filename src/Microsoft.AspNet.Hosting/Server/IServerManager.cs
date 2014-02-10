namespace Microsoft.AspNet.Hosting.Server
{
    public interface IServerManager
    {
        IServerFactory GetServer(string serverName);
    }
}