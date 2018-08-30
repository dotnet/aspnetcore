// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
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
            var checks = services.BuildServiceProvider().GetRequiredService<IEnumerable<IHealthCheck>>();

            // Assert
            Assert.Collection(
                checks,
                actual => Assert.Equal("Foo", actual.Name),
                actual => Assert.Equal("Bar", actual.Name));
        }
    }
}
