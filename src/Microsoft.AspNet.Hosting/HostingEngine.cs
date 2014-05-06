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
            InitalizeServerFactory(context);
            EnsureApplicationDelegate(context);

            var pipeline = new PipelineInstance(_httpContextFactory, context.ApplicationDelegate);
            var server = context.ServerFactory.Start(context.Server, pipeline.Invoke);
           
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