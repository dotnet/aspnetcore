// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Rewrite.Internal.UrlMatches;

namespace Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite
{
    public class UriMatchCondition : Condition
    {
        public UriMatchCondition(InputParser inputParser, string input, string pattern, UriMatchPart uriMatchPart, bool ignoreCase, bool negate)
        {
            var regex = new Regex(
                pattern,
                ignoreCase ? RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase : RegexOptions.CultureInvariant | RegexOptions.Compiled,
                TimeSpan.FromMilliseconds(1));
            Input = inputParser.ParseInputString(input, uriMatchPart);
            Match = new RegexMatch(regex, negate);
        }
    }
}