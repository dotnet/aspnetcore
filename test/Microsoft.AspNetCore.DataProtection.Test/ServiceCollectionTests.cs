// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection
{
    public class ServiceCollectionTests
    {
        [Fact]
        public void AddsOptions()
        {
            var services = new ServiceCollection()
                .AddDataProtection()
                .Services
                .BuildServiceProvider();

            Assert.NotNull(services.GetService<IOptions<DataProtectionOptions>>());
        }

        [Fact]
        public void DoesNotOverrideLogging()
        {
            var services1 = new ServiceCollection()
                .AddLogging()
                .AddDataProtection()
                .Services
                .BuildServiceProvider();

            var services2 = new ServiceCollection()
                .AddDataProtection()
                .Services
                .AddLogging()
                .BuildServiceProvider();

            Assert.Equal(
                services1.GetRequiredService<ILoggerFactory>().GetType(),
                services2.GetRequiredService<ILoggerFactory>().GetType());
        }

        [Fact]
        public void CanResolveAllRegisteredServices()
        {
            var serviceCollection = new ServiceCollection()
                .AddDataProtection()
                .Services;
            var services = serviceCollection.BuildServiceProvider(validateScopes: true);

            Assert.Null(services.GetService<ILoggerFactory>());

            foreach (var descriptor in serviceCollection)
            {
                if (descriptor.ServiceType.Assembly.GetName().Name == "Microsoft.Extensions.Options")
                {
                    // ignore any descriptors added by the call to .AddOptions()
                    continue;
                }

                Assert.NotNull(services.GetService(descriptor.ServiceType));
            }
        }
    }
}
