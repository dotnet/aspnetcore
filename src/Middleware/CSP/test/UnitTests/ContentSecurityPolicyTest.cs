using Xunit;

namespace Microsoft.AspNetCore.Csp.Test
{
    public class ContentSecurityPolicyTest
    {
        /// <summary>
        ///  Mocking a INonce for generating policies with a testable, fixed nonce.
        /// </summary>
        private class TestNonce : INonce
        {
            private readonly string _val;

            public TestNonce(string value)
            {
                _val = value;
            }

            public string GetValue()
            {
                return _val;
            }
        }

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

            Assert.DoesNotContain("strict-dynamic", policy.GetPolicy(new InertNonce()));
        }

        [Fact]
        public void WhenStrictDynamicSet_BuildsPolicyCorrectly()
        {
            var policy = new ContentSecurityPolicyBuilder()
                .WithCspMode(CspMode.ENFORCING)
                .WithStrictDynamic()
                .Build();

            Assert.Contains("strict-dynamic", policy.GetPolicy(new InertNonce()));
        }

        [Fact]
        public void WhenUnsafeEvalNotSet_BuildsPolicyCorrectly()
        {
            var policy = new ContentSecurityPolicyBuilder()
                .WithCspMode(CspMode.ENFORCING)
                .Build();

            Assert.DoesNotContain("unsafe-eval", policy.GetPolicy(new InertNonce()));
        }

        [Fact]
        public void WhenUnsafeEvalSet_BuildsPolicyCorrectly()
        {
            var policy = new ContentSecurityPolicyBuilder()
                .WithCspMode(CspMode.ENFORCING)
                .WithUnsafeEval()
                .Build();

            Assert.Contains("unsafe-eval", policy.GetPolicy(new InertNonce()));
        }

        // TODO: Add more coverage around reporting URLs
        [Fact]
        public void WhenReportingUriSet_BuildsPolicyCorrectly()
        {
            var policy = new ContentSecurityPolicyBuilder()
                .WithCspMode(CspMode.ENFORCING)
                .WithReportingUri("/cspreport")
                .Build();

            Assert.Contains("report-uri /cspreport", policy.GetPolicy(new InertNonce()));
        }

        [Fact]
        public void AlwaysRestrictsBaseUriAndObjectSrcToNone()
        {
            var policy = new ContentSecurityPolicyBuilder()
                .WithCspMode(CspMode.ENFORCING)
                .Build();

            Assert.Contains("base-uri 'none'; object-src 'none';", policy.GetPolicy(new InertNonce()));
        }

        [Fact]
        public void AlwaysSetsFallbackHttpAndHttpsProtocolsInScriptSrc()
        {
            var policy = new ContentSecurityPolicyBuilder()
                .WithCspMode(CspMode.ENFORCING)
                .Build();

            Assert.Matches("script-src .* https: http:", policy.GetPolicy(new InertNonce()));
        }

        [Theory]
        [InlineData("ABCDE")]
        [InlineData("1234567890")]
        public void SetNonceIfProvided(string nonce)
        {
            var policy = new ContentSecurityPolicyBuilder()
                .WithCspMode(CspMode.ENFORCING)
                .Build();

            Assert.Matches(string.Format("'nonce-{0}", nonce), policy.GetPolicy(new TestNonce(nonce)));
        }
    }

    public class InertNonce : INonce
    {
        public string GetValue()
        {
            return "inert";
        }
    }
}
