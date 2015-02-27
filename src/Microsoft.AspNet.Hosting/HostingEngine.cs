// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;

namespace Microsoft.AspNet.Hosting
{
    public class HostingEngine : IHostingEngine
    {
        private readonly IServerLoader _serverManager;
        private readonly IStartupLoader _startupLoader;
        private readonly IApplicationBuilderFactory _builderFactory;
        private readonly IHttpContextFactory _httpContextFactory;
        private readonly IHttpContextAccessor _contextAccessor;

        public HostingEngine(
            IServerLoader serverManager,
            IStartupLoader startupLoader,
            IApplicationBuilderFactory builderFactory,
            IHttpContextFactory httpContextFactory,
            IHttpContextAccessor contextAccessor)
        {
            _serverManager = serverManager;
            _startupLoader = startupLoader;
            _builderFactory = builderFactory;
            _httpContextFactory = httpContextFactory;
            _contextAccessor = contextAccessor;
        }

        public IDisposable Start(HostingContext context)
        {
            EnsureBuilder(context);
            EnsureServerFactory(context);
            InitalizeServerFactory(context);
            EnsureApplicationDelegate(context);

            var applicationLifetime = (ApplicationLifetime)context.ApplicationLifetime;
            var pipeline = new PipelineInstance(_httpContextFactory, context.ApplicationDelegate, _contextAccessor);
            var server = context.ServerFactory.Start(context.Server, pipeline.Invoke);

            return new Disposable(() =>
            {
                applicationLifetime.NotifyStopping();
                server.Dispose();
                pipeline.Dispose();
                applicationLifetime.NotifyStopped();
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

            context.ServerFactory = _serverManager.LoadServerFactory(context.ServerFactoryLocation);
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

            var diagnosticMessages = new List<string>();
            context.ApplicationStartup = _startupLoader.LoadStartup(
                context.ApplicationName,
                context.EnvironmentName,
                diagnosticMessages);

            if (context.ApplicationStartup == null)
            {
                throw new ArgumentException(
                    diagnosticMessages.Aggregate("Failed to find an entry point for the web application.", (a, b) => a + "\r\n" + b),
                    nameof(context));
            }
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