namespace Microsoft.AspNet.Hosting.Fakes
{
    public interface IFakeStartupCallback
    {
        void ConfigurationMethodCalled(object instance);
    }
}