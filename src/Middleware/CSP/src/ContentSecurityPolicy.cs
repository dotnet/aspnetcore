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
        }

        public string GetHeaderName()
        {
            return _cspMode == CspMode.REPORTING ? CspConstants.CspReportingHeaderName : CspConstants.CspEnforcedHeaderName;
        }
        public string GetPolicy()
        {
            return string.Format(
                "script-src 'nonce-random' {0} {1} https: http:; {2}; {3}",
                _strictDynamic ? "'strict-dynamic'" : "",
                _unsafeEval ? "'unsafe-eval'" : "",
                _baseAndObject,
                _reportingUri != null ? "report-uri " + _reportingUri : "");
        }
    }
}
