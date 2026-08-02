// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

public class FormattedClassification
{
    public string ClassificationName { get; }
    public string Text { get; }

    private FormattedClassification() { }

    public FormattedClassification(string text, string classificationName)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        ClassificationName = classificationName ?? throw new ArgumentNullException(nameof(classificationName));
    }

    public override bool Equals(object obj)
    {
        if (obj is FormattedClassification other)
        {
            return ClassificationName == other.ClassificationName
                && Text == other.Text;
        }

        return false;
    }

    public override int GetHashCode()
        => ClassificationName.GetHashCode() ^ Text.GetHashCode();

    public override string ToString()
    {
        if (ClassificationName.StartsWith("regex", StringComparison.Ordinal))
        {
            var remainder = ClassificationName.Substring("regex - ".Length);
            var parts = remainder.Split(' ');
            var type = string.Join("", parts.Select(Capitalize));
            return "Regex." + $"{type}(\"{Text}\")";
        }

        if (ClassificationName.StartsWith("json", StringComparison.Ordinal))
        {
            var remainder = ClassificationName.Substring("json - ".Length);
            var parts = remainder.Split(' ');
            var type = string.Join("", parts.Select(Capitalize));
            return "Json." + $"{type}(\"{Text}\")";
        }

        switch (ClassificationName)
        {
            case "punctuation":
                switch (Text)
                {
                    case "(":
                        return "Punctuation.OpenParen";
                    case ")":
                        return "Punctuation.CloseParen";
                    case "[":
                        return "Punctuation.OpenBracket";
                    case "]":
                        return "Punctuation.CloseBracket";
                    case "{":
                        return "Punctuation.OpenCurly";
                    case "}":
                        return "Punctuation.CloseCurly";
                    case ";":
                        return "Punctuation.Semicolon";
                    case ":":
                        return "Punctuation.Colon";
                    case ",":
                        return "Punctuation.Comma";
                    case "..":
                        return "Punctuation.DotDot";
                }

                goto default;

            case "operator":
                switch (Text)
                {
                    case "=":
                        return "Operators.Equals";
                    case "++":
                        return "Operators.PlusPlus";
                    case "=>":
                        return "Operators.EqualsGreaterThan";
                }

                goto default;

            case "keyword - control":
                return $"ControlKeyword(\"{Text}\")";

            default:
                return $"{Capitalize(ClassificationName)}(\"{Text}\")";
        }
    }

    private static string Capitalize(string val)
        => char.ToUpperInvariant(val[0]) + val.Substring(1);
}
