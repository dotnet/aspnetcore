// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.UrlRewrite
{
    public class UrlAction
    {
        public ActionType Type { get; set; }
        public Pattern Url { get; set; }
        public bool AppendQueryString { get; set; }
        public bool LogRewrittenUrl { get; set; }
        public RedirectType RedirectType { get; set; } = RedirectType.Permanent;
    }
}
