// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;

namespace Microsoft.AspNet.Hosting.Startup
{
    public class StartupMethods
    {
        // TODO: switch to ConfigureDelegate eventually
        public StartupMethods(Action<IApplicationBuilder> configure, ConfigureServicesDelegate configureServices)
        {
            ConfigureDelegate = configure;
            ConfigureServicesDelegate = configureServices ?? ApplicationStartup.DefaultBuildServiceProvider;
        }

        public ConfigureServicesDelegate ConfigureServicesDelegate { get; }
        public Action<IApplicationBuilder> ConfigureDelegate { get; }

    }
}