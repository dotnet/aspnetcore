using System;
using System.Threading;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;

namespace Microsoft.AspNet.Hosting
{
    public class HostingEngine : IHostingEngine
    {
        private readonly IServerManager _serverManager;
        private readonly IStartupManager _startupManager;
        private readonly IBuilderFactory _builderFactory;
        private readonly IHttpContextFactory _httpContextFactory;

        public HostingEngine(
            IServerManager serverManager,
            IStartupManager startupManager,
            IBuilderFactory builderFactory,
            IHttpContextFactory httpContextFactory)
        {
            _serverManager = serverManager;
            _startupManager = startupManager;
            _builderFactory = builderFactory;
            _httpContextFactory = httpContextFactory;
        }

        public IDisposable Start(HostingContext context)
        {
            EnsureBuilder(context);
            EnsureServerFactory(context);
            EnsureApplicationDelegate(context);

            var pipeline = new PipelineInstance(_httpContextFactory, context.ApplicationDelegate);
            var server = context.ServerFactory.Start(pipeline.Invoke);
           
            return new Disposable(() =>
            {
                server.Dispose();
                pipeline.Dispose();
            });
        }

        private void EnsureBuilder(HostingContext context)
        {
            if (context.Builder != null)
            {
                return;
            }

            context.Builder = _builderFactory.CreateBuilder();
        }

        private void EnsureServerFactory(HostingContext context)
        {
            if (context.ServerFactory == null)
            {
                context.ServerFactory = context.Services.GetService<IServerFactory>();
            }
            if (context.ServerFactory != null)
            {
                return;
            }

            context.ServerFactory = _serverManager.GetServerFactory(context.ServerName);
        }

        private void EnsureApplicationDelegate(HostingContext context)
        {
            if (context.ApplicationDelegate != null)
            {
                return;
            }

            EnsureApplicationStartup(context);
            EnsureBuilder(context);

            context.ApplicationStartup.Invoke(context.Builder);
            context.ApplicationDelegate = context.Builder.Build();
        }

        private void EnsureApplicationStartup(HostingContext context)
        {
            if (context.ApplicationStartup != null)
            {
                return;
            }

            context.ApplicationStartup = _startupManager.LoadStartup(context.ApplicationName);
        }

        private class Disposable : IDisposable
        {
            private Action _dispose;

            public Disposable(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _dispose, () => { }).Invoke();
            }
        }
    }
}