// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    public class ServiceCollectionExtensionsTests
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
            Assert.Collection(services,
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(IHealthCheckService), actual.ServiceType);
                    Assert.Equal(typeof(HealthCheckService), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                });
        }
    }
}
