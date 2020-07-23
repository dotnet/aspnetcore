// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Csp
{
    /// <summary>
    /// CSP-related constants.
    /// </summary>
    public static class CspConstants
    {
        /// <summary>
        /// CSP header name in enforcement mode.
        /// </summary>
        public static readonly string CspEnforcedHeaderName = "Content-Security-Policy";
        /// <summary>
        /// CSP header name in reporting mode.
        /// </summary>
        public static readonly string CspReportingHeaderName = "Content-Security-Policy-Report-Only";
        /// <summary>
        /// Expected content type for requests containing CSP violation reports.
        /// </summary>
        public static readonly string CspReportContentType = "application/csp-report";
        /// <summary>
        /// Possible violated directive value used to create textual representations of violation reports.
        /// </summary>
        public static readonly string ScriptSrcElem = "script-src-elem";
        /// <summary>
        /// Possible blocked URI value used to create textual representations of violation reports.
        /// </summary>
        public static readonly string BlockedUriInline = "inline";
        /// <summary>
        /// Possible violated directive value used to create textual representations of violation reports.
        /// </summary>
        public static readonly string ScriptSrcAttr = "script-src-attr";
    }
}
