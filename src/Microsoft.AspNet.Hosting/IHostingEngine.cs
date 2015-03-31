// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting
{
    public interface IHostingEngine
    {
        IDisposable Start();

        // Accessing this will block Use methods
        IServiceProvider ApplicationServices { get; }

        // Use methods blow up after any of the above methods are called
        IHostingEngine UseEnvironment(string environment);

        // Mutually exclusive
        IHostingEngine UseServer(string assemblyName);
        IHostingEngine UseServer(IServerFactory factory);

        // Mutually exclusive
        IHostingEngine UseStartup(string startupAssemblyName);
        IHostingEngine UseStartup(Action<IApplicationBuilder> configureApp);
        IHostingEngine UseStartup(Action<IApplicationBuilder> configureApp, ConfigureServicesDelegate configureServices);
        IHostingEngine UseStartup(Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices);
    }
}