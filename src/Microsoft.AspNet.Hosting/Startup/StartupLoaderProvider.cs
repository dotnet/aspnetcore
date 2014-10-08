// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Hosting.Startup
{
    public class StartupLoaderProvider : IStartupLoaderProvider
    {
        private readonly IServiceProvider _services;

        public StartupLoaderProvider(IServiceProvider services)
        {
            _services = services;
        }

        public int Order { get { return -100; } }

        public IStartupLoader CreateStartupLoader(IStartupLoader next)
        {
            return new StartupLoader(_services, next);
        }
    }
}
