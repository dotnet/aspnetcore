

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

        // TODO: Add logging configuration test

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
