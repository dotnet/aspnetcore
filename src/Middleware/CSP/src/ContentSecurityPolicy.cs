using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Csp
{
    public enum CspMode
    {
        NONE,
        REPORTING,
        ENFORCING
    }

    public class ContentSecurityPolicy
    {
        private readonly string _baseAndObject = "base-uri 'none'; object-src 'none'";
        private readonly Func<INonce, string> policyBuilder;

        private readonly CspMode _cspMode;
        private readonly bool _strictDynamic;
        private readonly bool _unsafeEval;
        private readonly string _reportingUri;

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

            var policyFormat = new StringBuilder()
                .Append("script-src")
                .Append(" 'nonce-{0}' ")  // nonce
                .Append(_strictDynamic ? "'strict-dynamic'" : "")
                .Append(_unsafeEval ? "'unsafe-eval'" : "")
                .Append(" https: http:;")  // fall-back allowlist-based CSP for browsers that don't support nonces
                .Append(_baseAndObject)
                .Append(";")               // end of script-src
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
}
