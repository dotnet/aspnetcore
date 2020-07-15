using Xunit;

namespace Microsoft.AspNetCore.Csp.Test
{
    public class ContentSecurityPolicyTest
    {
        [Fact]
        public void SetsCorrectHeaderNameInReportingMode()
        {
            var policy = new ContentSecurityPolicyBuilder()
                .WithCspMode(CspMode.REPORTING)
                .WithReportingUri("/csp")
                .Build();

            Assert.Equal(CspConstants.CspReportingHeaderName, policy.GetHeaderName());
        }

        [Fact]
        public void SetsCorrectHeaderNameInEnforcementMode()
        {
            var policy = new ContentSecurityPolicyBuilder()
                .WithCspMode(CspMode.ENFORCING)
                .Build();

            Assert.Equal(CspConstants.CspEnforcedHeaderName, policy.GetHeaderName());
        }

        [Fact]
        public void WhenStrictDynamicNotSet_BuildsPolicyCorrectly()
        {
            var policy = new ContentSecurityPolicyBuilder()
                .WithCspMode(CspMode.ENFORCING)
                .Build();

            Assert.DoesNotContain("strict-dynamic", policy.GetPolicy());
        }

        [Fact]
        public void WhenStrictDynamicSet_BuildsPolicyCorrectly()
        {
            var policy = new ContentSecurityPolicyBuilder()
                .WithCspMode(CspMode.ENFORCING)
                .WithStrictDynamic()
                .Build();

            Assert.Contains("strict-dynamic", policy.GetPolicy());
        }

        [Fact]
        public void WhenUnsafeEvalNotSet_BuildsPolicyCorrectly()
        {
            var policy = new ContentSecurityPolicyBuilder()
                .WithCspMode(CspMode.ENFORCING)
                .Build();

            Assert.DoesNotContain("unsafe-eval", policy.GetPolicy());
        }

        [Fact]
        public void WhenUnsafeEvalSet_BuildsPolicyCorrectly()
        {
            var policy = new ContentSecurityPolicyBuilder()
                .WithCspMode(CspMode.ENFORCING)
                .WithUnsafeEval()
                .Build();

            Assert.Contains("unsafe-eval", policy.GetPolicy());
        }

        // TODO: Add more coverage around reporting URLs
        [Fact]
        public void WhenReportingUriSet_BuildsPolicyCorrectly()
        {
            var policy = new ContentSecurityPolicyBuilder()
                .WithCspMode(CspMode.ENFORCING)
                .WithReportingUri("/cspreport")
                .Build();

            Assert.Contains("report-uri /cspreport", policy.GetPolicy());
        }

        [Fact]
        public void AlwaysRestrictsBaseUriAndObjectSrcToNone()
        {
            var policy = new ContentSecurityPolicyBuilder()
                .WithCspMode(CspMode.ENFORCING)
                .Build();

            Assert.Contains("base-uri 'none'; object-src 'none';", policy.GetPolicy());
        }

        [Fact]
        public void AlwaysSetsFallbackHttpAndHttpsProtocolsInScriptSrc()
        {
            var policy = new ContentSecurityPolicyBuilder()
                .WithCspMode(CspMode.ENFORCING)
                .Build();

            Assert.Matches("script-src .* https: http:", policy.GetPolicy());
        }
    }
}
