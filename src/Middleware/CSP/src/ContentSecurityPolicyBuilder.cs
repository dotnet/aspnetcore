using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Csp
{
    public class ContentSecurityPolicyBuilder
    {
        private CspMode _cspMode;
        private bool _strictDynamic;
        private bool _unsafeEval;
        private string _reportingUri;
        private LogLevel _logLevel = LogLevel.Information;

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

        public ContentSecurityPolicyBuilder WithLogLevel(LogLevel logLevel)
        {
            _logLevel = logLevel;
            return this;
        }

        public bool HasReporting()
        {
            return _reportingUri != null;
        }

        public LoggingConfiguration LoggingConfiguration()
        {
            return new LoggingConfiguration
            {
                LogLevel = _logLevel,
                ReportUri = _reportingUri
            };
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
