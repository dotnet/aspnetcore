// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class ServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddHealthChecks_RegistersSingletonHealthCheckServiceIdempotently()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddHealthChecks();
            services.AddHealthChecks();

            // Assert
            Assert.Collection(services.OrderBy(s => s.ServiceType.FullName),
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(HealthCheckService), actual.ServiceType);
                    Assert.Equal(typeof(DefaultHealthCheckService), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(IHostedService), actual.ServiceType);
                    Assert.Equal(typeof(HealthCheckPublisherHostedService), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                });
        }
    }
}
