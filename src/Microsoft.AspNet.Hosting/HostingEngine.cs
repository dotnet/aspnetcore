// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting
{
    public class HostingEngine : IHostingEngine
    {
        private readonly IServerManager _serverManager;
        private readonly IStartupManager _startupManager;
        private readonly IApplicationBuilderFactory _builderFactory;
        private readonly IHttpContextFactory _httpContextFactory;

        public HostingEngine(
            IServerManager serverManager,
            IStartupManager startupManager,
            IApplicationBuilderFactory builderFactory,
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
            InitalizeServerFactory(context);
            EnsureApplicationDelegate(context);

            var applicationLifetime = (ApplicationLifetime)context.Services.GetRequiredService<IApplicationLifetime>();
            var pipeline = new PipelineInstance(_httpContextFactory, context.ApplicationDelegate);
            var server = context.ServerFactory.Start(context.Server, pipeline.Invoke);
           
            return new Disposable(() =>
            {
                applicationLifetime.SignalStopping();
                server.Dispose();
                pipeline.Dispose();
                applicationLifetime.SignalStopped();
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
            if (context.ServerFactory != null)
            {
                return;
            }

            context.ServerFactory = _serverManager.GetServerFactory(context.ServerName);
        }

        private void InitalizeServerFactory(HostingContext context)
        {
            if (context.Server == null)
            {
                context.Server = context.ServerFactory.Initialize(context.Configuration);
            }

            if (context.Builder.Server == null)
            {
                context.Builder.Server = context.Server;
            }
        }

        private void EnsureApplicationDelegate(HostingContext context)
        {
            if (context.ApplicationDelegate != null)
            {
                return;
            }

            EnsureApplicationStartup(context);

            context.ApplicationStartup.Invoke(context.Builder);
            context.ApplicationDelegate = context.Builder.Build();
        }

        private void EnsureApplicationStartup(HostingContext context)
        {
            if (context.ApplicationStartup != null)
            {
                return;
            }

            context.ApplicationStartup = _startupManager.LoadStartup(
                context.ApplicationName,
                context.EnvironmentName);
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