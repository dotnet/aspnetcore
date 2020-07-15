using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Csp
{
    public class ContentSecurityPolicyBuilder
    {
        private readonly ContentSecurityPolicy _policy = new ContentSecurityPolicy();

        public ContentSecurityPolicyBuilder WithCspMode(CspMode cspMode)
        {
            _policy.CspMode = cspMode;
            return this;
        }

        public ContentSecurityPolicyBuilder WithStrictDynamic()
        {
            _policy.StrictDynamic = true;
            return this;
        }

        public ContentSecurityPolicyBuilder WithUnsafeEval()
        {
            _policy.UnsafeEval = true;
            return this;
        }

        public ContentSecurityPolicyBuilder WithReportOnly()
        {
            _policy.ReportOnly = true;
            return this;
        }
        public ContentSecurityPolicyBuilder WithReportingUri(string reportingUri)
        {
            // TODO: normalize URL
            _policy.ReportingUri = reportingUri;
            return this;
        }

        public ContentSecurityPolicyBuilder WithLoggingConfiguration(LoggingConfiguration loggingConfiguration)
        {
            _policy.LoggingConfiguration = loggingConfiguration;
            return this;
        }

        public ContentSecurityPolicy Build()
        {
            if (_policy.CspMode == CspMode.NONE)
            {
                // TODO: Error message
                throw new InvalidOperationException();
            }

            if (_policy.ReportOnly && _policy.ReportingUri == null)
            {
                // TODO: Error message
                throw new InvalidOperationException();
            }

            if (_policy.ReportOnly && _policy.LoggingConfiguration == null)
            {
                // TODO: Error message
                throw new InvalidOperationException();
            }

            return _policy;
        }
    }
}
