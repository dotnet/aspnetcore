// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

// Copied from https://github.com/dotnet/roslyn/blob/9fee6f5461baae5152c956c3c3024ca15b85feb9/src/Features/Core/Portable/EmbeddedLanguages/EmbeddedLanguageCommentDetector.cs

/// <summary>
/// Helps match patterns of the form: language=name,option1,option2,option3
/// <para/>
/// All matching is case insensitive, with spaces allowed between the punctuation. Option values are
/// returned as strings.
/// <para/>
/// Option names are the values from the TOptions enum.
/// </summary>
internal readonly struct EmbeddedLanguageCommentDetector
{
    private readonly Regex _regex;

    public EmbeddedLanguageCommentDetector(ImmutableArray<string> identifiers)
    {
        var namePortion = string.Join("|", identifiers.Select(n => $"({Regex.Escape(n)})"));
        _regex = new Regex($@"^((//)|(')|(/\*))\s*lang(uage)?\s*=\s*(?<identifier>{namePortion})\b((\s*,\s*)(?<option>[a-zA-Z]+))*",
            RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    public bool TryMatch(
        string text,
        [NotNullWhen(true)] out string? identifier,
        [NotNullWhen(true)] out IEnumerable<string>? options)
    {
        var match = _regex.Match(text);
        identifier = null;
        options = null;
        if (!match.Success)
        {
            return false;
        }

        identifier = match.Groups["identifier"].Value;
        var optionGroup = match.Groups["option"];
        options = optionGroup.Captures.OfType<Capture>().Select(c => c.Value);
        return true;
    }
}
