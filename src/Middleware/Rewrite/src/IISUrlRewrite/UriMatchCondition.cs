// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Rewrite.UrlMatches;

namespace Microsoft.AspNetCore.Rewrite.IISUrlRewrite;

internal sealed class UriMatchCondition : Condition
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
