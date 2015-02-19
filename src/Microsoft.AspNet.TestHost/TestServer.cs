// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Http;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.TestHost
{
    public class TestServer : IServerFactory, IDisposable
    {
        private const string DefaultEnvironmentName = "Development";
        private const string ServerName = nameof(TestServer);
        private static readonly ServerInformation ServerInfo = new ServerInformation();
        private Func<object, Task> _appDelegate;
        private IDisposable _appInstance;
        private bool _disposed = false;

        public TestServer(IConfiguration config, IServiceProvider serviceProvider, Action<IApplicationBuilder> appStartup)
        {
            var appEnv = serviceProvider.GetRequiredService<IApplicationEnvironment>();

            HostingContext hostContext = new HostingContext()
            {
                ApplicationName = appEnv.ApplicationName,
                Configuration = config,
                ServerFactory = this,
                Services = serviceProvider,
                ApplicationStartup = appStartup
            };

            var engine = serviceProvider.GetRequiredService<IHostingEngine>();
            _appInstance = engine.Start(hostContext);
        }

        public Uri BaseAddress { get; set; } = new Uri("http://localhost/");

        public static TestServer Create(Action<IApplicationBuilder> app)
        {
            return Create(CallContextServiceLocator.Locator.ServiceProvider, app, configureHostServices: null);
        }

        public static TestServer Create(Action<IApplicationBuilder> app, Action<IServiceCollection> configureHostServices)
        {
            return Create(CallContextServiceLocator.Locator.ServiceProvider, app, configureHostServices);
        }

        public static TestServer Create(IServiceProvider serviceProvider, Action<IApplicationBuilder> app)
        {
            return Create(serviceProvider, app, configureHostServices: null);
        }

        public static TestServer Create(IServiceProvider serviceProvider, Action<IApplicationBuilder> app, Action<IServiceCollection> configureHostServices)
        {
            var appServices = HostingServices.Create(serviceProvider, configureHostServices).BuildServiceProvider();
            var config = new Configuration();
            return new TestServer(config, appServices, app);
        }

        public HttpMessageHandler CreateHandler()
        {
            var pathBase = BaseAddress == null ? PathString.Empty : PathString.FromUriComponent(BaseAddress);
            return new ClientHandler(Invoke, pathBase);
        }

        public HttpClient CreateClient()
        {
            return new HttpClient(CreateHandler()) { BaseAddress = BaseAddress };
        }

        /// <summary>
        /// Begins constructing a request message for submission.
        /// </summary>
        /// <param name="path"></param>
        /// <returns><see cref="RequestBuilder"/> to use in constructing additional request details.</returns>
        public RequestBuilder CreateRequest(string path)
        {
            return new RequestBuilder(this, path);
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

        public Task Invoke(object env)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            return _appDelegate(env);
        }

        public void Dispose()
        {
            _disposed = true;
            _appInstance.Dispose();
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
