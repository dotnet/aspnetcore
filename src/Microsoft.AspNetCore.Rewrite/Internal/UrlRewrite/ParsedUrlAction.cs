// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite
{
    public class ParsedUrlAction
    {
        public ActionType Type { get; set; }
        public Pattern Url { get; set; }
        public bool AppendQueryString { get; set; } = true;
        public bool LogRewrittenUrl { get; set; } // Ignoring this flag.
        public RedirectType RedirectType { get; set; } = RedirectType.Permanent;
    }
}
