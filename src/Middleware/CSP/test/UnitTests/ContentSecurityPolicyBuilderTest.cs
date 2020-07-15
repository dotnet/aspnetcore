

using System;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Csp.Test
{
    public class ContentSecurityPolicyBuilderTest
    {
        [Fact]
        public void IfCspModeNotSet_thenExceptionThrown()
        {
            Assert.Throws<InvalidOperationException>(
                () => new ContentSecurityPolicyBuilder()
                .Build()
            );
        }

        [Fact]
        public void WhenNoLoggingConfigurationSet_thenDefaultLoggingConfigurationUsed()
        {
            var policy = new ContentSecurityPolicyBuilder()
                .WithCspMode(CspMode.ENFORCING)
                .Build();

            //TODO: Define default logging config
            Assert.Equal(LogLevel.Information, policy.LoggingConfiguration.LogLevel);
        }

        [Fact]
        public void WhenModeSetToReporting_IfNoReportingUriSet_thenExceptionThrown()
        {
            Assert.Throws<InvalidOperationException>(
                () => new ContentSecurityPolicyBuilder()
                .WithCspMode(CspMode.REPORTING)
                .Build()
            );
        }
    }
}
