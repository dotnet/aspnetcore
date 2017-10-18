// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    public class HealthChecksBuilderTests
    {
        [Fact]
        public void ChecksCanBeRegisteredInMultipleCallsToAddHealthChecks()
        {
            var services = new ServiceCollection();
            services.AddHealthChecks()
                .AddCheck("Foo", () => Task.FromResult(HealthCheckResult.Healthy()));
            services.AddHealthChecks()
                .AddCheck("Bar", () => Task.FromResult(HealthCheckResult.Healthy()));

            // Act
            var healthCheckService = services.BuildServiceProvider().GetRequiredService<IHealthCheckService>();

            // Assert
            Assert.Collection(healthCheckService.Checks,
                actual => Assert.Equal("Foo", actual.Key),
                actual => Assert.Equal("Bar", actual.Key));
        }
    }
}
