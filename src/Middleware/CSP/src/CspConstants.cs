namespace Microsoft.AspNetCore.Csp
{
    public static class CspConstants
    {
        public static readonly string CspEnforcedHeaderName = "Content-Security-Policy";
        public static readonly string CspReportingHeaderName = "Content-Security-Policy-Report-Only";
        public static readonly string CspReportContentType = "application/csp-report";
        public static readonly string ScriptSrcElem = "script-src-elem";
        public static readonly string BlockedUriInline = "inline";
        public static readonly string ScriptSrcAttr = "script-src-attr";
    }
}
