namespace Microsoft.AspNet.Hosting.Tests.Fakes
{
    public interface IFakeStartupCallback
    {
        void ConfigurationMethodCalled(object instance);
    }
}