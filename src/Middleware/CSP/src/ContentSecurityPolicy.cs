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
        //TODO: Remove getters
        public CspMode CspMode { get; internal set; }
        public bool StrictDynamic { get; internal set; }
        public bool UnsafeEval { get; internal set; }
        public string ReportingUri { get; internal set; }
        public LoggingConfiguration LoggingConfiguration { get; internal set; }
        public bool ReportOnly { get; internal set; }

        public string GetPolicy()
        {
            return "object-src 'none'; script-src 'nonce-{random}' 'strict-dynamic' https: http:; base-uri 'none'; ";
        }
    }
}
