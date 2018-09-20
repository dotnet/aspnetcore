// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    public class HealthReportTest
    {
        [Theory]
        [InlineData(HealthStatus.Healthy)]
        [InlineData(HealthStatus.Degraded)]
        [InlineData(HealthStatus.Unhealthy)]
        [InlineData(HealthStatus.Failed)]
        public void Status_MatchesWorstStatusInResults(HealthStatus status)
        {
            var result = new HealthReport(new Dictionary<string, HealthReportEntry>()
            {
                {"Foo", new HealthReportEntry(HealthStatus.Healthy, null, null, null) },
                {"Bar", new HealthReportEntry(HealthStatus.Healthy, null, null, null) },
                {"Baz", new HealthReportEntry(status, exception: null, description: null, data: null) },
                {"Quick", new HealthReportEntry(HealthStatus.Healthy, null, null, null) },
                {"Quack", new HealthReportEntry(HealthStatus.Healthy, null, null, null) },
                {"Quock", new HealthReportEntry(HealthStatus.Healthy, null, null, null) },
            });

            Assert.Equal(status, result.Status);
        }
    }
}
