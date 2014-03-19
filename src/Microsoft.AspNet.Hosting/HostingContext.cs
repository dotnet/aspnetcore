using System;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.Hosting.Server;

namespace Microsoft.AspNet.Hosting
{
    public class HostingContext
    {
        public IServiceProvider Services { get; set; }
        public IConfiguration Configuration { get; set; }

        public IBuilder Builder { get; set; }

        public string ApplicationName { get; set; }
        public Action<IBuilder> ApplicationStartup { get; set; }
        public RequestDelegate ApplicationDelegate { get; set; }

        public string ServerName { get; set; }
        public IServerFactory ServerFactory { get; set; }
        public IServerInformation Server { get; set; }
    }
}