// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Http;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.TestHost
{
    public class TestServer : IServerFactory, IDisposable
    {
        private const string DefaultEnvironmentName = "Development";
        private const string ServerName = nameof(TestServer);
        private static readonly ServerInformation ServerInfo = new ServerInformation();
        private Func<IFeatureCollection, Task> _appDelegate;
        private IDisposable _appInstance;
        private bool _disposed = false;

        public TestServer(WebHostBuilder builder)
        {
            _appInstance = builder.UseServer(this).Build().Start();
        }

        public Uri BaseAddress { get; set; } = new Uri("http://localhost/");

        public static TestServer Create()
        {
            return Create(services: null, config: null, configureApp: null, configureServices: null);
        }

        public static TestServer Create(Action<IApplicationBuilder> configureApp)
        {
            return Create(services: null, config: null, configureApp: configureApp, configureServices: null);
        }

        public static TestServer Create(Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices)
        {
            return Create(services: null, config: null, configureApp: configureApp, configureServices: configureServices);
        }

        public static TestServer Create(IServiceProvider services, Action<IApplicationBuilder> configureApp, ConfigureServicesDelegate configureServices)
        {
            return new TestServer(CreateBuilder(services, config: null, configureApp: configureApp, configureServices: configureServices));
        }

        public static TestServer Create(IServiceProvider services, IConfiguration config, Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices)
        {
            return new TestServer(CreateBuilder(services, config, configureApp, configureServices));
        }

        public static WebHostBuilder CreateBuilder(IServiceProvider services, IConfiguration config, Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices)
        {
            return CreateBuilder(services, config, configureApp,
                s =>
                {
                    if (configureServices != null)
                    {
                        configureServices(s);
                    }
                    return s.BuildServiceProvider();
                });
        }

        public static WebHostBuilder CreateBuilder(IServiceProvider services, IConfiguration config, Action<IApplicationBuilder> configureApp, ConfigureServicesDelegate configureServices)
        {
            return CreateBuilder(services, config).UseStartup(configureApp, configureServices);
        }

        public static WebHostBuilder CreateBuilder(IServiceProvider services, IConfiguration config)
        {
            return new WebHostBuilder(
                services ?? CallContextServiceLocator.Locator.ServiceProvider,
                config ?? new Configuration());
        }

        public static WebHostBuilder CreateBuilder()
        {
            return CreateBuilder(services: null, config: null);
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

        public IDisposable Start(IServerInformation serverInformation, Func<IFeatureCollection, Task> application)
        {
            if (!(serverInformation.GetType() == typeof(ServerInformation)))
            {
                throw new ArgumentException(string.Format("The server must be {0}", ServerName), "serverInformation");
            }

            _appDelegate = application;

            return this;
        }

        public Task Invoke(IFeatureCollection featureCollection)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            return _appDelegate(featureCollection);
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
                get { return ServerName; }
            }
        }
    }
}