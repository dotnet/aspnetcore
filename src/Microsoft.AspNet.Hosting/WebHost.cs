// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.Hosting
{
    public static class WebHost
    {
        public static IHostingEngine CreateEngine()
        {
            return CreateEngine(new Configuration());
        }

        public static IHostingEngine CreateEngine(Action<IServiceCollection> configureServices)
        {
            return CreateEngine(new Configuration(), configureServices);
        }

        public static IHostingEngine CreateEngine(IConfiguration config)
        {
            return CreateEngine(config, configureServices: null);
        }

        public static IHostingEngine CreateEngine(IConfiguration config, Action<IServiceCollection> configureServices)
        {
            return CreateEngine(fallbackServices: null, config: config, configureServices: configureServices);
        }

        public static IHostingEngine CreateEngine(IServiceProvider fallbackServices, IConfiguration config)
        {
            return CreateEngine(fallbackServices, config, configureServices: null);
        }

        public static IHostingEngine CreateEngine(IServiceProvider fallbackServices, IConfiguration config, Action<IServiceCollection> configureServices)
        {
            return CreateFactory(fallbackServices, configureServices).Create(config);
        }

        public static IHostingFactory CreateFactory()
        {
            return CreateFactory(fallbackServices: null, configureServices: null);
        }

        public static IHostingFactory CreateFactory(Action<IServiceCollection> configureServices)
        {
            return CreateFactory(fallbackServices: null, configureServices: configureServices);
        }

        public static IHostingFactory CreateFactory(IServiceProvider fallbackServices, Action<IServiceCollection> configureServices)
        {
            fallbackServices = fallbackServices ?? CallContextServiceLocator.Locator.ServiceProvider;
            return new RootHostingServiceCollectionInitializer(fallbackServices, configureServices)
                .Build()
                .BuildServiceProvider()
                .GetRequiredService<IHostingFactory>();
        }
    }
}