using System.Reflection;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Server.WebListener
{
    public class ServerInformation : IServerInformation
    {
        private WebListenerWrapper _webListenerWrapper;

        internal ServerInformation(WebListenerWrapper webListenerWrapper)
        {
            _webListenerWrapper = webListenerWrapper;
        }

        internal WebListenerWrapper Wrapper
        {
            get { return _webListenerWrapper; }
        }

        // Microsoft.AspNet.Server.WebListener
        public string Name
        {
            get { return GetType().GetTypeInfo().Assembly.GetName().Name; }
        }

        public OwinWebListener Listener
        {
            get { return _webListenerWrapper.Listener; }
        }

        public int MaxAccepts
        {
            get { return _webListenerWrapper.MaxAccepts; }
            set { _webListenerWrapper.MaxAccepts = value; }
        }
    }
}
