namespace Microsoft.AspNet.Hosting.Startup
{
    public interface IStartupLoaderProvider
    {
        int Order { get; }

        IStartupLoader CreateStartupLoader(IStartupLoader next);
    }
}