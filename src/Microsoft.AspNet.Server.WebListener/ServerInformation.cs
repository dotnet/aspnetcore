using System.Reflection;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Server.WebListener
{
    public class ServerInformation : IServerInformation
    {
        private MessagePump _messagePump;

        internal ServerInformation(MessagePump messagePump)
        {
            _messagePump = messagePump;
        }

        internal MessagePump MessagePump
        {
            get { return _messagePump; }
        }

        // Microsoft.AspNet.Server.WebListener
        public string Name
        {
            get { return GetType().GetTypeInfo().Assembly.GetName().Name; }
        }

        public Microsoft.Net.Server.WebListener Listener
        {
            get { return _messagePump.Listener; }
        }

        public int MaxAccepts
        {
            get { return _messagePump.MaxAccepts; }
            set { _messagePump.MaxAccepts = value; }
        }
    }
}
