// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite
{
    public static class RewriteTags
    {
        public const string Rewrite = "rewrite";
        public const string GlobalRules = "globalRules";
        public const string Rules = "rules";
        public const string Rule = "rule";
        public const string Action = "action";
        public const string Name = "name";
        public const string Enabled = "enabled";
        public const string PatternSyntax = "patternSyntax";
        public const string StopProcessing = "stopProcessing";
        public const string Match = "match";
        public const string Conditions = "conditions";
        public const string IgnoreCase = "ignoreCase";
        public const string Negate = "negate";
        public const string Url = "url";
        public const string MatchType = "matchType";
        public const string Add = "add";
        public const string TrackingAllCaptures = "trackingAllCaptures";
        public const string MatchPattern = "matchPattern";
        public const string Input = "input";
        public const string Pattern = "pattern";
        public const string Type = "type";
        public const string AppendQuery = "appendQueryString";
        public const string LogRewrittenUrl = "logRewrittenUrl";
        public const string RedirectType = "redirectType";
    }
}
