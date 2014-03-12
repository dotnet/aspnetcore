namespace Microsoft.AspNet.Hosting.Server
{
    public interface IServerManager
    {
        IServerFactory GetServerFactory(string serverName);
    }
}