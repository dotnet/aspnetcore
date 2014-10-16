// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;

namespace Microsoft.AspNet.Hosting
{
    public static class WebApplication
    {
        public static IDisposable Start()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Add(HostingServices.GetDefaultServices());

            var context = new HostingContext
            {
                Services = serviceCollection.BuildServiceProvider()
            };

            var engine = context.Services.GetRequiredService<IHostingEngine>();
            if (engine == null)
            {
                throw new Exception("TODO: IHostingEngine service not available exception");
            }

            return engine.Start(context);
        }
    }
}