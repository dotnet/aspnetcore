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

        public string GetHeaderName()
        {
            return CspMode == CspMode.REPORTING ? CspConstants.CspReportingHeaderName : CspConstants.CspEnforcedHeaderName;
        }
        public string GetPolicy()
        {
            return string.Format(
                "script-src 'nonce-random' {0} {1} https: http:; {2}; {3}",
                StrictDynamic ? "'strict-dynamic'" : "",
                UnsafeEval ? "'unsafe-eval'" : "",
                _baseAndObject,
                ReportingUri != null ? "report-uri " + ReportingUri : "");
        }

        //TODO: Remove getters
        public CspMode CspMode { get; internal set; }
        public bool StrictDynamic { get; internal set; }
        public bool UnsafeEval { get; internal set; }
        public string ReportingUri { get; internal set; }
        public LoggingConfiguration LoggingConfiguration { get; internal set; }
    }
}
