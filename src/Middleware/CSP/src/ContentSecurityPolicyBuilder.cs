using System;

namespace Microsoft.AspNetCore.Csp
{
    public class ContentSecurityPolicyBuilder
    {
        private LoggingConfiguration _loggingConfiguration;
        private CspMode _cspMode;
        private bool _strictDynamic;
        private bool _unsafeEval;
        private string _reportingUri;

        public ContentSecurityPolicyBuilder()
        {
            // TODO: Consider adding builder for logging config
            _loggingConfiguration = new LoggingConfiguration();
            _loggingConfiguration.LogLevel = Extensions.Logging.LogLevel.Information;
        }

        public ContentSecurityPolicyBuilder WithCspMode(CspMode cspMode)
        {
            _cspMode = cspMode;
            return this;
        }

        public ContentSecurityPolicyBuilder WithStrictDynamic()
        {
            _strictDynamic = true;
            return this;
        }

        public ContentSecurityPolicyBuilder WithUnsafeEval()
        {
            _unsafeEval = true;
            return this;
        }
        public ContentSecurityPolicyBuilder WithReportingUri(string reportingUri)
        {
            // TODO: normalize URL
            _reportingUri = reportingUri;
            return this;
        }

        public ContentSecurityPolicyBuilder WithLoggingConfiguration(LoggingConfiguration loggingConfiguration)
        {
            _loggingConfiguration = loggingConfiguration;
            return this;
        }

        public bool HasReporting()
        {
            return _reportingUri != null;
        }

        public ContentSecurityPolicy Build()
        {
            if (_cspMode == CspMode.NONE)
            {
                // TODO: Error message
                throw new InvalidOperationException();
            }

            if (_cspMode == CspMode.REPORTING && _reportingUri == null)
            {
                // TODO: Error message
                throw new InvalidOperationException();
            }

            return new ContentSecurityPolicy(
                _cspMode,
                _strictDynamic,
                _unsafeEval,
                _reportingUri
            );
        }
    }
}
