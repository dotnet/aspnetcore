using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Csp
{
    public static class CspConstants
    {
        public static readonly string CspEnforcedHeaderName = "Content-Security-Policy";
        public static readonly string CspReportingHeaderName = "Content-Security-Policy-Report-Only";
        public static readonly string CspReportContentType = "application/csp-report";
    }
}
