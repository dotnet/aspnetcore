namespace Microsoft.AspNet.Hosting.Server
{
    public interface IServerFactoryProvider
    {
        IServerFactory GetServerFactory(string serverName);
    }
}
