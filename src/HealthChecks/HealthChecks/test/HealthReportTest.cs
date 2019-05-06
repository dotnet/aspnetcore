// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        public void Status_MatchesWorstStatusInResults(HealthStatus status)
        {
            var result = new HealthReport(new Dictionary<string, HealthReportEntry>()
            {
                {"Foo", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null) },
                {"Bar", new HealthReportEntry(HealthStatus.Healthy, null, TimeSpan.MinValue,null, null) },
                {"Baz", new HealthReportEntry(status, exception: null, description: null,duration:TimeSpan.MinValue, data: null) },
                {"Quick", new HealthReportEntry(HealthStatus.Healthy, null, TimeSpan.MinValue, null, null) },
                {"Quack", new HealthReportEntry(HealthStatus.Healthy, null, TimeSpan.MinValue, null, null) },
                {"Quock", new HealthReportEntry(HealthStatus.Healthy, null, TimeSpan.MinValue, null, null) },
            }, totalDuration: TimeSpan.MinValue);

            Assert.Equal(status, result.Status);
        }

        [Theory]
        [InlineData(200)]
        [InlineData(300)]
        [InlineData(400)]
        public void TotalDuration_MatchesTotalDurationParameter(int milliseconds)
        {
            var result = new HealthReport(new Dictionary<string, HealthReportEntry>()
            {
                {"Foo", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null) }
            }, totalDuration: TimeSpan.FromMilliseconds(milliseconds));

            Assert.Equal(TimeSpan.FromMilliseconds(milliseconds), result.TotalDuration);
        }
    }
}
