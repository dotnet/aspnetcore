// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.Runtime.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.Hosting.Tests
{
    public class UseServicesFacts
    {
        [Fact]
        public void OptionsAccessorCanBeResolvedAfterCallingUseServicesWithAction()
        {
            var builder = new ApplicationBuilder(CallContextServiceLocator.Locator.ServiceProvider);

            builder.UseServices(serviceCollection => { });

            var optionsAccessor = builder.ApplicationServices.GetRequiredService<IOptions<object>>();
            Assert.NotNull(optionsAccessor);
        }


        [Fact]
        public void OptionsAccessorCanBeResolvedAfterCallingUseServicesWithFunc()
        {
            var builder = new ApplicationBuilder(CallContextServiceLocator.Locator.ServiceProvider);
            IServiceProvider serviceProvider = null;

            builder.UseServices(serviceCollection =>
            {
                serviceProvider = serviceCollection.BuildServiceProvider();
                return serviceProvider;
            });

            Assert.Same(serviceProvider, builder.ApplicationServices);
            var optionsAccessor = builder.ApplicationServices.GetRequiredService<IOptions<object>>();
            Assert.NotNull(optionsAccessor);
        }
    }
}