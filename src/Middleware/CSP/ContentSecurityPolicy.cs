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
        public CspMode CspMode { get; internal set; }
        public bool StrictDynamic { get; internal set; }
        public bool UnsafeEval { get; internal set; }
        public string ReportingUri { get; internal set; }
        public LoggingConfiguration LoggingConfiguration { get; internal set; }
        public bool ReportOnly { get; internal set; }
    }
}
