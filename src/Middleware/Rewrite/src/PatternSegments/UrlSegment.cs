// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Rewrite.IISUrlRewrite;

namespace Microsoft.AspNetCore.Rewrite.PatternSegments
{
    internal class UrlSegment : PatternSegment
    {
        private readonly UriMatchPart _uriMatchPart;

        public UrlSegment()
            : this(UriMatchPart.Path)
        {
        }

        public UrlSegment(UriMatchPart uriMatchPart)
        {
            _uriMatchPart = uriMatchPart;
        }

        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences)
        {
            return _uriMatchPart == UriMatchPart.Full ? context.HttpContext.Request.GetEncodedUrl() : (string)context.HttpContext.Request.Path;
        }
    }
}