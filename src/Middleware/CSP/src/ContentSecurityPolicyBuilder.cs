using System;

namespace Microsoft.AspNetCore.Csp
{
    public class ContentSecurityPolicyBuilder
    {
        private readonly ContentSecurityPolicy _policy = new ContentSecurityPolicy();
        private LoggingConfiguration _LoggingConfiguration;

        public ContentSecurityPolicyBuilder()
        {
            // TODO: Consider adding builder for logging config
            _LoggingConfiguration = new LoggingConfiguration();
            _LoggingConfiguration.LogLevel = Extensions.Logging.LogLevel.Information;
        }

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
        public ContentSecurityPolicyBuilder WithReportingUri(string reportingUri)
        {
            // TODO: normalize URL
            _policy.ReportingUri = reportingUri;
            return this;
        }

        public ContentSecurityPolicyBuilder WithLoggingConfiguration(LoggingConfiguration loggingConfiguration)
        {
            _LoggingConfiguration = loggingConfiguration;
            return this;
        }

        public ContentSecurityPolicy Build()
        {
            if (_policy.CspMode == CspMode.NONE)
            {
                // TODO: Error message
                throw new InvalidOperationException();
            }

            if (_policy.CspMode == CspMode.REPORTING && _policy.ReportingUri == null)
            {
                // TODO: Error message
                throw new InvalidOperationException();
            }

            _policy.LoggingConfiguration = _LoggingConfiguration;
            return _policy;
        }
    }
}
