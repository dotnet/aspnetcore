// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Rewrite.UrlMatches;

namespace Microsoft.AspNetCore.Rewrite.IISUrlRewrite
{
    internal class UriMatchCondition : Condition
    {
        private static readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(1);

        public UriMatchCondition(InputParser inputParser, string input, string pattern, UriMatchPart uriMatchPart, bool ignoreCase, bool negate)
            : base(CreatePattern(inputParser, input, uriMatchPart), CreateRegexMatch(pattern, ignoreCase, negate))
        {
        }

        private static Pattern CreatePattern(InputParser inputParser, string input, UriMatchPart uriMatchPart)
        {
            return inputParser.ParseInputString(input, uriMatchPart);
        }

        private static RegexMatch CreateRegexMatch(string pattern, bool ignoreCase, bool negate)
        {
            var regexOptions = RegexOptions.CultureInvariant | RegexOptions.Compiled;
            regexOptions = ignoreCase ? regexOptions | RegexOptions.IgnoreCase : regexOptions;
            var regex = new Regex(
                pattern,
                regexOptions,
                _regexTimeout
            );
            return new RegexMatch(regex, negate);
        }
    }
}
