// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.TestHost
{
    public class TestServer : IServerFactory, IDisposable
    {
        private static readonly string ServerName = typeof(TestServer).FullName;
        private static readonly ServerInformation ServerInfo = new ServerInformation();
        private Func<object, Task> _appDelegate;
        private TestClient _handler;

        public TestServer(IConfiguration config, IServiceProvider serviceProvider, Action<IBuilder> appStartup)
        {
            var env = serviceProvider.GetService<IApplicationEnvironment>();
            if (env == null)
            {
                throw new ArgumentException("IApplicationEnvironment couldn't be resolved.", "serviceProvider");
            }

            HostingContext hostContext = new HostingContext()
            {
                ApplicationName = env.ApplicationName,
                Configuration = config,
                ServerFactory = this,
                Services = serviceProvider,
                ApplicationStartup = appStartup
            };

            var engine = serviceProvider.GetService<IHostingEngine>();
            var disposable = engine.Start(hostContext);
        }

        public static TestServer Create<TStartup>(IServiceProvider provider)
        {
            var startupLoader = new StartupLoader(provider, new NullStartupLoader());
            var name = typeof(TStartup).AssemblyQualifiedName;
            var diagnosticMessages = new List<string>();
            return Create(provider, startupLoader.LoadStartup(name, diagnosticMessages));
        }

        public static TestServer Create(IServiceProvider provider, Action<IBuilder> app)
        {
            var collection = new ServiceCollection();
            var hostingServices = HostingServices.GetDefaultServices();

            var config = new Configuration();
            collection.Add(hostingServices);

            var serviceProvider = collection.BuildServiceProvider(provider);
            return new TestServer(config, serviceProvider, app);
        }

        public TestClient Handler
        {
            get
            {
                if (_handler == null)
                {
                    _handler = new TestClient(_appDelegate);
                }

                return _handler;
            }
        }

        public IServerInformation Initialize(IConfiguration configuration)
        {
            return ServerInfo;
        }

        public IDisposable Start(IServerInformation serverInformation, Func<object, Task> application)
        {
            if (!(serverInformation.GetType() == typeof(ServerInformation)))
            {
                throw new ArgumentException(string.Format("The server must be {0}", ServerName), "serverInformation");
            }

            _appDelegate = application;

            return this;
        }

        public void Dispose()
        {
            // IServerFactory.Start needs to return an IDisposable. Typically this IDisposable instance is used to 
            // clear any server resources when tearing down the host. In our case we don't have anything to clear
            // so we just implement IDisposable and do nothing.
        }

        private class ServerInformation : IServerInformation
        {
            public string Name
            {
                get { return TestServer.ServerName; }
            }
        }
    }
}
