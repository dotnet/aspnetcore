// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
using Xunit;

namespace Microsoft.AspNet.Hosting.Tests
{
    public class UseServicesFacts
    {
        [Fact]
        public void OptionsAccessorCanBeResolvedAfterCallingUseServicesWithAction()
        {
            var baseServiceProvider = new ServiceCollection().BuildServiceProvider();
            var builder = new ApplicationBuilder(baseServiceProvider);

            builder.UsePerRequestServices(serviceCollection => { });

            var optionsAccessor = builder.ApplicationServices.GetService<IOptionsAccessor<object>>();
            Assert.NotNull(optionsAccessor);
        }


        [Fact]
        public void OptionsAccessorCanBeResolvedAfterCallingUseServicesWithFunc()
        {
            var baseServiceProvider = new ServiceCollection().BuildServiceProvider();
            var builder = new ApplicationBuilder(baseServiceProvider);
            IServiceProvider serviceProvider = null;

            builder.UsePerRequestServices(serviceCollection =>
            {
                serviceProvider = serviceCollection.BuildServiceProvider(builder.ApplicationServices);
                return serviceProvider;
            });

            Assert.Same(serviceProvider, builder.ApplicationServices);
            var optionsAccessor = builder.ApplicationServices.GetService<IOptionsAccessor<object>>();
            Assert.NotNull(optionsAccessor);
        }
    }
}