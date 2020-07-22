
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Csp
{
    public class CspReport
    {
        [JsonPropertyName("csp-report")]
        public Report ReportData { get; set; }
        // TODO: if we find a way to get the csp-report field from the JSON we'll be able to remove one level of nestedness
        public class Report
        {
            [JsonPropertyName("blocked-uri")]
            public string BlockedUri { get; set; }
            [JsonPropertyName("document-uri")]
            public string DocumentUri { get; set; }
            [JsonPropertyName("referrer")]
            public string Referrer { get; set; }
            [JsonPropertyName("violated-directive")]
            public string ViolatedDirective { get; set; }
            [JsonPropertyName("source-file")]
            public string SourceFile { get; set; }
            [JsonPropertyName("line-number")]
            public string LineNumber { get; set; }

            // Old browsers don't set the next two fields (e.g. Firefox v25/v26)
            [JsonPropertyName("original-policy")]
            public string OriginalPolicy { get; set; }
            [JsonPropertyName("effective-directive")]
            public string EffectiveDirective { get; set; }

            // CSP3 only
            [JsonPropertyName("script-sample")]
            public string ScriptSample { get; set; }
            [JsonPropertyName("disposition")]
            public string Disposition { get; set; }
        }
    }
}
