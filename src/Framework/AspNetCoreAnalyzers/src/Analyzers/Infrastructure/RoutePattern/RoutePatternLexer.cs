// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzers.Infrastructure.EmbeddedSyntax;
using Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;

using static RoutePatternHelpers;

using RoutePatternToken = EmbeddedSyntaxToken<RoutePatternKind>;

internal struct RoutePatternLexer
{
    public readonly VirtualCharSequence Text;
    public readonly RoutePatternOptions RoutePatternOptions;
    public int Position;

    public RoutePatternLexer(VirtualCharSequence text, RoutePatternOptions routePatternOptions) : this()
    {
        Text = text;
        RoutePatternOptions = routePatternOptions;
    }

    public VirtualChar CurrentChar => Position < Text.Length ? Text[Position] : default;

    public VirtualCharSequence GetSubPatternToCurrentPos(int start)
        => GetSubPattern(start, Position);

    public VirtualCharSequence GetSubPattern(int start, int end)
        => Text.GetSubSequence(TextSpan.FromBounds(start, end));

    public RoutePatternToken ScanNextToken()
    {
        if (Position == Text.Length)
        {
            return CreateToken(RoutePatternKind.EndOfFile, VirtualCharSequence.Empty);
        }

        var ch = CurrentChar;
        Position++;

        return CreateToken(GetKind(ch), Text.GetSubSequence(new TextSpan(Position - 1, 1)));
    }

    private static RoutePatternKind GetKind(VirtualChar ch)
        => ch.Value switch
        {
            '/' => RoutePatternKind.SlashToken,
            '~' => RoutePatternKind.TildeToken,
            '{' => RoutePatternKind.OpenBraceToken,
            '}' => RoutePatternKind.CloseBraceToken,
            '[' => RoutePatternKind.OpenBracketToken,
            ']' => RoutePatternKind.CloseBracketToken,
            '.' => RoutePatternKind.DotToken,
            '=' => RoutePatternKind.EqualsToken,
            ':' => RoutePatternKind.ColonToken,
            '*' => RoutePatternKind.AsteriskToken,
            '(' => RoutePatternKind.OpenParenToken,
            ')' => RoutePatternKind.CloseParenToken,
            '?' => RoutePatternKind.QuestionMarkToken,
            ',' => RoutePatternKind.CommaToken,
            _ => RoutePatternKind.TextToken,
        };

    public TextSpan GetTextSpan(int startInclusive, int endExclusive)
        => TextSpan.FromBounds(Text[startInclusive].Span.Start, Text[endExclusive - 1].Span.End);

    public bool IsAt(string val)
        => TextAt(Position, val);

    private bool TextAt(int position, string val)
    {
        for (var i = 0; i < val.Length; i++)
        {
            if (position + i >= Text.Length ||
                Text[position + i].Value != val[i])
            {
                return false;
            }
        }

        return true;
    }

    internal RoutePatternToken? TryScanLiteral()
    {
        if (Position == Text.Length)
        {
            return null;
        }

        var start = Position;

        int? mismatchBracePosition = null;
        int? mismatchBracketPosition = null;
        int? questionMarkPosition = null;
        while (Position < Text.Length)
        {
            var ch = CurrentChar;

            if (ch.Value == '/')
            {
                // Literal ends at a separator or start of a parameter.
                break;
            }
            else if (ch.Value == '{' && IsUnescapedChar(ref Position, '{'))
            {
                // Literal ends at brace start.
                break;
            }
            else if (ch.Value == '}' && IsUnescapedChar(ref Position, '}'))
            {
                // An unescaped brace is invalid.
                mismatchBracePosition = Position;
            }
            else if (ch.Value == '?')
            {
                questionMarkPosition = Position;
            }
            else if (ch.Value == '[' && IsUnescapedChar(ref Position, '[') && RoutePatternOptions.SupportTokenReplacement)
            {
                // Literal ends at bracket start if token replacement is supported.
                break;
            }
            else if (IsUnescapedChar(ref Position, ']') && RoutePatternOptions.SupportTokenReplacement)
            {
                mismatchBracketPosition = Position;
            }

            Position++;
        }

        if (Position == start)
        {
            return null;
        }

        var token = CreateToken(RoutePatternKind.Literal, GetSubPatternToCurrentPos(start));
        token = token.With(value: token.VirtualChars.CreateString());

        // It's fine that this only warns about the first invalid close brace.
        if (mismatchBracePosition != null)
        {
            token = token.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                Resources.TemplateRoute_MismatchedParameter,
                token.GetSpan()));
        }
        if (mismatchBracketPosition != null)
        {
            token = token.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                Resources.AttributeRoute_TokenReplacement_ImbalancedSquareBrackets,
                token.GetSpan()));
        }
        if (questionMarkPosition != null)
        {
            token = token.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                Resources.FormatTemplateRoute_InvalidLiteral(token.Value),
                token.GetSpan()));
        }

        return token;
    }

    private const char Separator = '/';
    private const char OpenBrace = '{';
    private const char CloseBrace = '}';
    private const char QuestionMark = '?';
    private const char Asterisk = '*';

    internal RoutePatternToken? TryScanParameterName()
    {
        if (Position == Text.Length)
        {
            return null;
        }

        var start = Position;
        var hasInvalidChar = false;
        var hasUnescapedOpenBrace = false;
        while (Position < Text.Length)
        {
            var ch = CurrentChar;
            if (ch.Value is ':' or '=' && start != Position)
            {
                // Colon and equals ends a parameter name unless they're the first character.
                // I think this is a bug in RoutePatternParser but follow it for compatibility.
                break;
            }
            else if (IsTrailingQuestionMark(ch))
            {
                // Parameter name ends before question mark (optional) if at the end of the parameter name.
                // e.g., {id?}
                break;
            }
            else if (ch.Value == '}' && IsUnescapedChar(ref Position, '}'))
            {
                break;
            }
            else if (ch.Value == '{' && IsUnescapedChar(ref Position, '{'))
            {
                hasUnescapedOpenBrace = true;
            }
            else if (IsInvalidNameChar(ch))
            {
                hasInvalidChar = true;
            }

            Position++;
        }

        if (Position == start)
        {
            return null;
        }

        var token = CreateToken(RoutePatternKind.ParameterNameToken, GetSubPatternToCurrentPos(start));
        token = token.With(value: token.VirtualChars.CreateString());
        if (hasUnescapedOpenBrace)
        {
            token = token.AddDiagnosticIfNone(
                new EmbeddedDiagnostic(Resources.TemplateRoute_UnescapedBrace, token.GetSpan()));
        }
        if (hasInvalidChar)
        {
            token = token.AddDiagnosticIfNone(
                new EmbeddedDiagnostic(Resources.FormatTemplateRoute_InvalidParameterName(token.Value!.ToString().Replace("{{", "{").Replace("}}", "}")), token.GetSpan()));
        }

        return token;

        static bool IsInvalidNameChar(VirtualChar ch) =>
            ch.Value switch
            {
                Separator => true,
                OpenBrace => true,
                CloseBrace => true,
                QuestionMark => true,
                Asterisk => true,
                _ => false
            };
    }

    private bool IsTrailingQuestionMark(VirtualChar ch)
    {
        return ch.Value == '?' && IsAt("?}") && !IsAt("?}}");
    }

    internal RoutePatternToken? TryScanUnescapedPolicyFragment()
    {
        if (Position == Text.Length)
        {
            return null;
        }

        var start = Position;
        var hasUnescapedOpenBrace = false;
        while (Position < Text.Length)
        {
            var ch = Text[Position];
            if (ch.Value is ':' or '=' or '?')
            {
                break;
            }
            else if (ch.Value == '{' && IsUnescapedChar(ref Position, '{'))
            {
                hasUnescapedOpenBrace = true;
            }
            else if (IsUnescapedChar(ref Position, '}'))
            {
                break;
            }

            // Only start escaped fragment if there is an open and close.
            if (ch.Value == '(')
            {
                if (HasPolicyParenClose())
                {
                    break;
                }
            }
            Position++;
        }

        if (Position == start)
        {
            return null;
        }

        var token = CreateToken(RoutePatternKind.PolicyFragmentToken, GetSubPatternToCurrentPos(start));
        token = token.With(value: token.VirtualChars.CreateString());
        if (hasUnescapedOpenBrace)
        {
            token = token.AddDiagnosticIfNone(
                new EmbeddedDiagnostic(Resources.TemplateRoute_UnescapedBrace, token.GetSpan()));
        }
        return token;
    }

    internal bool IsUnescapedChar(ref int position, char c)
    {
        if (Text[position].Value != c)
        {
            return false;
        }

        if (position + 1 >= Text.Length || Text[position + 1].Value != c)
        {
            return true;
        }

        position++;
        return false;
    }

    internal bool IsUnescapedCharLookahead(ref int position, char c)
    {
        var currentPosition = position;
        while (currentPosition < Text.Length && Text[currentPosition].Value == c)
        {
            currentPosition++;
        }

        // The char is unescaped if there is an odd number, e.g.
        // [ == unescaped
        // [[ == escaped
        // [[[ = unescaped, etc
        if ((currentPosition - position) % 2 == 1)
        {
            return true;
        }
        // If escaped chars encountered then skip to the end.
        if (currentPosition > position)
        {
            position = currentPosition - 1;
        }
        return false;
    }

    internal RoutePatternToken? TryScanEscapedPolicyFragment()
    {
        if (Position == Text.Length)
        {
            return null;
        }

        var start = Position;
        var parameterEndedWithoutCloseParen = false;
        var hasUnescapedOpenBrace = false;
        while (Position < Text.Length)
        {
            var ch = Text[Position];

            if (IsUnescapedChar(ref Position, '}'))
            {
                parameterEndedWithoutCloseParen = true;
                break;
            }
            else if (ch.Value == '{' && IsUnescapedChar(ref Position, '{'))
            {
                hasUnescapedOpenBrace = true;
            }
            else if (ch.Value == ')')
            {
                break;
            }

            Position++;
        }

        if (parameterEndedWithoutCloseParen)
        {
            // Couldn't find close paren before end of parameter.
            // Reset position to start so content can be parsed as unescaped.
            Position = start;
            return null;
        }

        // This token could end with an unclosed parameter.
        var token = CreateToken(RoutePatternKind.PolicyFragmentToken, GetSubPatternToCurrentPos(start));
        token = token.With(value: token.VirtualChars.CreateString());
        if (hasUnescapedOpenBrace)
        {
            token = token.AddDiagnosticIfNone(
                new EmbeddedDiagnostic(Resources.TemplateRoute_UnescapedBrace, token.GetSpan()));
        }
        return token;
    }

    internal RoutePatternToken? TryScanReplacementToken()
    {
        if (Position == Text.Length)
        {
            return null;
        }

        var start = Position;
        var hasUnescapedOpenBracket = false;
        while (Position < Text.Length)
        {
            var ch = Text[Position];

            if (ch.Value == '[' && IsUnescapedChar(ref Position, '['))
            {
                hasUnescapedOpenBracket = true;
            }
            else if (IsUnescapedCharLookahead(ref Position, ']'))
            {
                // Note that a replacement token ends at the start of a sequence of escapes.
                // ends here -> ]]]
                break;
            }

            Position++;
        }

        if (Position == start)
        {
            return null;
        }

        // This token could end with an unclosed parameter.
        var token = CreateToken(RoutePatternKind.ReplacementToken, GetSubPatternToCurrentPos(start));
        token = token.With(value: token.VirtualChars.CreateString());
        if (hasUnescapedOpenBracket)
        {
            token = token.AddDiagnosticIfNone(
                new EmbeddedDiagnostic(Resources.AttributeRoute_TokenReplacement_UnescapedBraceInToken, token.GetSpan()));
        }
        return token;
    }

    internal RoutePatternToken? TryScanDefaultValue()
    {
        if (Position == Text.Length)
        {
            return null;
        }

        var start = Position;
        while (Position < Text.Length)
        {
            var ch = Text[Position];

            if (ch.Value is '}')
            {
                break;
            }
            else if (IsTrailingQuestionMark(ch))
            {
                // Parameter name ends before question mark (optional) if at the end of the parameter name.
                // e.g., {id?}
                break;
            }

            Position++;
        }

        if (Position == start)
        {
            return null;
        }

        var token = CreateToken(RoutePatternKind.DefaultValueToken, GetSubPatternToCurrentPos(start));
        token = token.With(value: token.VirtualChars.CreateString());
        return token;
    }

    internal bool HasPolicyParenClose()
    {
        if (Position == Text.Length)
        {
            return false;
        }

        var current = Position;
        while (current < Text.Length)
        {
            var ch = Text[current];

            if (ch.Value == ')')
            {
                return true;
            }
            if (IsUnescapedChar(ref current, '}'))
            {
                return false;
            }
            current++;
        }

        return false;
    }
}
