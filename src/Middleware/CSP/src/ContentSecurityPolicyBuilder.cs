// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Csp
{
    /// <summary>
    /// Allows customizing content security policies
    /// </summary>
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

        /// <summary>
        /// Whether the policy specifies a relative reporting URI.
        /// </summary>
        /// <remarks>
        /// If this method returns true, a handler for the reporting endpoint will be automatically added to this application.
        /// </remarks>
        public bool HasLocalReporting()
        {
            return _reportingUri != null && _reportingUri.StartsWith("/");
        }

        public CspReportLogger ReportLogger(ICspReportLoggerFactory loggerFactory)
        {
            return loggerFactory.BuildLogger(_logLevel, _reportingUri);
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
