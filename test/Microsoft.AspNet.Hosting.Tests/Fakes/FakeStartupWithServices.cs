using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Hosting.Fakes
{
    public class FakeStartupWithServices
    {
        private readonly IFakeStartupCallback _fakeStartupCallback;

        public FakeStartupWithServices(IFakeStartupCallback fakeStartupCallback)
        {
            _fakeStartupCallback = fakeStartupCallback;
        }

        public void Configuration(IBuilder builder)
        {
            _fakeStartupCallback.ConfigurationMethodCalled(this);
        }
    }
}