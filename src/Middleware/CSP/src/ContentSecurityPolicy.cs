// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.AspNetCore.Csp
{
    /// <summary>
    /// A greedy Content Security Policy generator
    /// </summary>
    public class ContentSecurityPolicy
    {
        private readonly string _baseAndObject = "base-uri 'none'; object-src 'none'";
        private readonly Func<INonce, string> policyBuilder;

        private readonly CspMode _cspMode;
        private readonly bool _strictDynamic;
        private readonly bool _unsafeEval;
        private readonly string _reportingUri;

        /// <summary>
        /// Instantiates a new <see cref="ContentSecurityPolicy"/>.
        /// </summary>
        /// <param name="cspMode">Represents whether the current policy is in enforcing or reporting mode.</param>
        /// <param name="strictDynamic">Whether the policy should enable nonce propagation.</param>
        /// <param name="unsafeEval">Whether JavaScript's eval should be allowed to run.</param>
        /// <param name="reportingUri">An absolute or relative URI representing the reporting endpoint</param>
        public ContentSecurityPolicy(
            CspMode cspMode,
            bool strictDynamic,
            bool unsafeEval,
            string reportingUri
        )
        {
            _cspMode = cspMode;
            _strictDynamic = strictDynamic;
            _unsafeEval = unsafeEval;
            _reportingUri = reportingUri;

            // compute the static directives of the policy up front to avoid doing so on every request
            var policyFormat = new StringBuilder()
                .Append("script-src")
                .Append(" 'nonce-{0}' ")  // nonce
                .Append(_strictDynamic ? "'strict-dynamic'" : "")
                .Append(_unsafeEval ? "'unsafe-eval'" : "")
                .Append(" https: http:;")  // fall-back allowlist-based CSP for browsers that don't support nonces
                .Append(_baseAndObject)
                .Append("; ")               // end of script-src
                .Append(_reportingUri != null ? "report-uri " + _reportingUri : "")
                .ToString();

            policyBuilder = nonce => string.Format(policyFormat, nonce.GetValue());
        }

        public string GetHeaderName()
        {
            return _cspMode == CspMode.REPORTING ? CspConstants.CspReportingHeaderName : CspConstants.CspEnforcedHeaderName;
        }
        public string GetPolicy(INonce nonce)
        {
            return policyBuilder.Invoke(nonce);
        }
    }

    public enum CspMode
    {
        NONE,
        REPORTING,
        ENFORCING
    }
}
