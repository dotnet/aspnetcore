// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Context = Microsoft.AspNet.Hosting.Internal.HostingApplication.Context;

namespace Microsoft.AspNet.TestHost
{
    public class TestServer : IServer
    {
        private const string DefaultEnvironmentName = "Development";
        private const string ServerName = nameof(TestServer);
        private IDisposable _appInstance;
        private bool _disposed = false;
        private IHttpApplication<Context> _application;

        public TestServer(WebHostBuilder builder)
        {
            var hostingEngine = builder.UseServer(this).Build();
            _appInstance = hostingEngine.Start();
        }

        public Uri BaseAddress { get; set; } = new Uri("http://localhost/");

        IFeatureCollection IServer.Features { get; }

        public static TestServer Create()
        {
            return Create(config: null, configureApp: null, configureServices: null);
        }

        public static TestServer Create(Action<IApplicationBuilder> configureApp)
        {
            return Create(config: null, configureApp: configureApp, configureServices: null);
        }

        public static TestServer Create(Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices)
        {
            return Create(config: null, configureApp: configureApp, configureServices: configureServices);
        }

        public static TestServer Create(Action<IApplicationBuilder> configureApp, Func<IServiceCollection, IServiceProvider> configureServices)
        {
            return new TestServer(CreateBuilder(config: null, configureApp: configureApp, configureServices: configureServices, configureHostServices: null));
        }
        public static TestServer Create(Action<IApplicationBuilder> configureApp, Func<IServiceCollection, IServiceProvider> configureServices, Action<IServiceCollection> configureHostServices)
        {
            return new TestServer(CreateBuilder(config: null, configureApp: configureApp, configureServices: configureServices, configureHostServices: configureHostServices));
        }

        public static TestServer Create(IConfiguration config, Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices)
        {
            return new TestServer(CreateBuilder(config, configureApp, configureServices));
        }

        public static WebHostBuilder CreateBuilder(IConfiguration config, Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices)
        {
            return CreateBuilder(config, configureApp,
                s =>
                {
                    if (configureServices != null)
                    {
                        configureServices(s);
                    }
                    return s.BuildServiceProvider();
                }, null);
        }
        public static WebHostBuilder CreateBuilder(IConfiguration config, Action<IApplicationBuilder> configureApp, Func<IServiceCollection, IServiceProvider> configureServices)
        {
            return CreateBuilder(config, configureApp, configureServices, null);
        }

        public static WebHostBuilder CreateBuilder(IConfiguration config, Action<IApplicationBuilder> configureApp, Func<IServiceCollection, IServiceProvider> configureServices, Action<IServiceCollection> configureHostServices)
        {
            return CreateBuilder(config).UseStartup(configureApp, configureServices).UseServices(configureHostServices);
        }

        public static WebHostBuilder CreateBuilder(IConfiguration config)
        {
            return new WebHostBuilder(
                config ?? new ConfigurationBuilder().Build());
        }

        public static WebHostBuilder CreateBuilder()
        {
            return CreateBuilder(config: null);
        }

        public HttpMessageHandler CreateHandler()
        {
            var pathBase = BaseAddress == null ? PathString.Empty : PathString.FromUriComponent(BaseAddress);
            return new ClientHandler(pathBase, _application);
        }

        public HttpClient CreateClient()
        {
            return new HttpClient(CreateHandler()) { BaseAddress = BaseAddress };
        }

        public WebSocketClient CreateWebSocketClient()
        {
            var pathBase = BaseAddress == null ? PathString.Empty : PathString.FromUriComponent(BaseAddress);
            return new WebSocketClient(pathBase, _application);
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

        public void Dispose()
        {
            _disposed = true;
            _appInstance.Dispose();
        }

        void IServer.Start<TContext>(IHttpApplication<TContext> application)
        {
            _application = new ApplicationWrapper<Context>((IHttpApplication<Context>)application, () =>
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
            });
        }

        private class ApplicationWrapper<TContext> : IHttpApplication<TContext>
        {
            IHttpApplication<TContext> _application;
            Action _preProcessRequestAsync;

            public ApplicationWrapper(IHttpApplication<TContext> application, Action preProcessRequestAsync)
            {
                _application = application;
                _preProcessRequestAsync = preProcessRequestAsync;
            }

            public TContext CreateContext(IFeatureCollection contextFeatures)
            {
                return _application.CreateContext(contextFeatures);
            }

            public void DisposeContext(TContext context, Exception exception)
            {
                _application.DisposeContext(context, exception);
            }

            public Task ProcessRequestAsync(TContext context)
            {
                _preProcessRequestAsync();
                return _application.ProcessRequestAsync(context);
            }
        }
    }
}