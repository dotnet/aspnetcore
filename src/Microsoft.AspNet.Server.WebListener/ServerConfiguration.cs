using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting.Server;

namespace Microsoft.AspNet.Server.WebListener
{
    internal class ServerConfiguration : IServerConfiguration
    {
        internal ServerConfiguration()
        {
            Addresses = new List<IDictionary<string, object>>(1);
        }

        public IList<IDictionary<string, object>> Addresses
        {
            get;
            internal set;
        }

        public object AdvancedConfiguration
        {
            get;
            internal set;
        }
    }
}
