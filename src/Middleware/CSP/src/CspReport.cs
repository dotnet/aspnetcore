using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Csp
{
    public class CspReport
    {
        public Report ReportData { get; set; }
        // TODO: if we find a way to get the csp-report field from the JSON we'll be able to remove one level of nestedness
        public class Report
        {
            public string BlockedUri { get; set; }
            public string DocumentUri { get; set; }
            public string Referrer { get; set; }
            public string ViolatedDirective { get; set; }
            public string SourceFile { get; set; }
            public int LineNumber { get; set; }

            // Old browsers don't set the next two fields (e.g. Firefox v25/v26)
            public string OriginalPolicy { get; set; }
            public string EffectiveDirective { get; set; }

            // CSP3 only
            public string ScriptSample { get; set; }
            public string Disposition { get; set; }
        }
    }
}
