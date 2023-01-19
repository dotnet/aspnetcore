// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;

internal class CSharpVirtualCharService : AbstractVirtualCharService
{
    public static readonly IVirtualCharService Instance = new CSharpVirtualCharService();

    protected CSharpVirtualCharService()
    {
    }

    protected override bool IsMultiLineRawStringToken(SyntaxToken token)
    {
        if (token.Kind() is SyntaxKind.MultiLineRawStringLiteralToken or SyntaxKind.Utf8MultiLineRawStringLiteralToken)
        {
            return true;
        }
        if (token.Parent?.Parent is InterpolatedStringExpressionSyntax { StringStartToken.RawKind: (int)SyntaxKind.InterpolatedMultiLineRawStringStartToken })
        {
            return true;
        }
        return false;
    }

    protected override VirtualCharSequence TryConvertToVirtualCharsWorker(SyntaxToken token)
    {
        // C# preprocessor directives can contain string literals.  However, these string literals do not behave
        // like normal literals.  Because they are used for paths (i.e. in a #line directive), the language does not
        // do any escaping within them.  i.e. if you have a \ it's just a \   Note that this is not a verbatim
        // string.  You can't put a double quote in it either, and you cannot have newlines and whatnot.
        //
        // We technically could convert this trivially to an array of virtual chars.  After all, there would just be
        // a 1:1 correspondence with the literal contents and the chars returned.  However, we don't even both
        // returning anything here.  That's because there's no useful features we can offer here.  Because there are
        // no escape characters we won't classify any escape characters.  And there is no way that these strings
        // would be Regex/Json snippets.  So it's easier to just bail out and return nothing.
        if (IsInDirective(token.Parent))
        {
            return default;
        }

        Debug.Assert(!token.ContainsDiagnostics);

        switch (token.Kind())
        {
            case SyntaxKind.CharacterLiteralToken:
                return TryConvertStringToVirtualChars(token, "'", "'", escapeBraces: false);

            case SyntaxKind.StringLiteralToken:
                return token.IsVerbatimStringLiteral()
                    ? TryConvertVerbatimStringToVirtualChars(token, "@\"", "\"", escapeBraces: false)
                    : TryConvertStringToVirtualChars(token, "\"", "\"", escapeBraces: false);

            case SyntaxKind.Utf8StringLiteralToken:
                return token.IsVerbatimStringLiteral()
                    ? TryConvertVerbatimStringToVirtualChars(token, "@\"", "\"u8", escapeBraces: false)
                    : TryConvertStringToVirtualChars(token, "\"", "\"u8", escapeBraces: false);

            case SyntaxKind.SingleLineRawStringLiteralToken:
            case SyntaxKind.Utf8SingleLineRawStringLiteralToken:
                return TryConvertSingleLineRawStringToVirtualChars(token);

            case SyntaxKind.MultiLineRawStringLiteralToken:
            case SyntaxKind.Utf8MultiLineRawStringLiteralToken:
                return token.GetRequiredParent() is LiteralExpressionSyntax literalExpression
                    ? TryConvertMultiLineRawStringToVirtualChars(token, literalExpression, tokenIncludeDelimiters: true)
                    : default;

            case SyntaxKind.InterpolatedStringTextToken:
                {
                    var parent = token.GetRequiredParent();
                    var isFormatClause = parent is InterpolationFormatClauseSyntax;
                    if (isFormatClause)
                    {
                        parent = parent.GetRequiredParent();
                    }

                    var interpolatedString = (InterpolatedStringExpressionSyntax)parent.GetRequiredParent();

                    return interpolatedString.StringStartToken.Kind() switch
                    {
                        SyntaxKind.InterpolatedStringStartToken => TryConvertStringToVirtualChars(token, "", "", escapeBraces: true),
                        SyntaxKind.InterpolatedVerbatimStringStartToken => TryConvertVerbatimStringToVirtualChars(token, "", "", escapeBraces: true),
                        SyntaxKind.InterpolatedSingleLineRawStringStartToken => TryConvertSingleLineRawStringToVirtualChars(token),
                        SyntaxKind.InterpolatedMultiLineRawStringStartToken
                            // Format clauses must be single line, even when in a multi-line interpolation.
                            => isFormatClause
                                ? TryConvertSingleLineRawStringToVirtualChars(token)
                                : TryConvertMultiLineRawStringToVirtualChars(token, interpolatedString, tokenIncludeDelimiters: false),
                        _ => default,
                    };
                }
        }

        return default;
    }

    private static bool IsInDirective(SyntaxNode? node)
    {
        while (node != null)
        {
            if (node is DirectiveTriviaSyntax)
            {
                return true;
            }

            node = node.GetParent(ascendOutOfTrivia: true);
        }

        return false;
    }

    private static VirtualCharSequence TryConvertVerbatimStringToVirtualChars(SyntaxToken token, string startDelimiter, string endDelimiter, bool escapeBraces)
        => TryConvertSimpleDoubleQuoteString(token, startDelimiter, endDelimiter, escapeBraces);

    private static VirtualCharSequence TryConvertSingleLineRawStringToVirtualChars(SyntaxToken token)
    {
        var tokenText = token.Text;
        var offset = token.SpanStart;

        var result = ImmutableList.CreateBuilder<VirtualChar>();

        var startIndexInclusive = 0;
        var endIndexExclusive = tokenText.Length;

        if (token.Kind() is SyntaxKind.Utf8SingleLineRawStringLiteralToken)
        {
            endIndexExclusive -= "u8".Length;
        }

        if (token.Kind() is SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.Utf8SingleLineRawStringLiteralToken)
        {
            if (!(tokenText[0] == '"'))
            {
                throw new InvalidOperationException("String should start with quote.");
            }

            while (tokenText[startIndexInclusive] == '"')
            {
                // All quotes should be paired at the end
                if (!(tokenText[endIndexExclusive - 1] == '"'))
                {
                    throw new InvalidOperationException("String should end with quote.");
                }
                startIndexInclusive++;
                endIndexExclusive--;
            }
        }

        for (var index = startIndexInclusive; index < endIndexExclusive;)
        {
            index += ConvertTextAtIndexToRune(tokenText, index, result, offset);
        }

        return CreateVirtualCharSequence(tokenText, offset, startIndexInclusive, endIndexExclusive, result);
    }

    /// <summary>
    /// Creates the sequence for the <b>content</b> characters in this <paramref name="token"/>.  This will not
    /// include indentation whitespace that the language specifies is not part of the content.
    /// </summary>
    /// <param name="parentExpression">The containing expression for this token.  This is needed so that we can
    /// determine the indentation whitespace based on the last line of the containing multiline literal.</param>
    /// <param name="tokenIncludeDelimiters">If this token includes the quote (<c>"</c>) characters for the
    /// delimiters inside of it or not.  If so, then those quotes will need to be skipped when determining the
    /// content</param>
    private static VirtualCharSequence TryConvertMultiLineRawStringToVirtualChars(
        SyntaxToken token, ExpressionSyntax parentExpression, bool tokenIncludeDelimiters)
    {
        // if this is the first text content chunk of the multi-line literal.  The first chunk contains the leading
        // indentation of the line it's on (which thus must be trimmed), while all subsequent chunks do not (because
        // they start right after some `{...}` interpolation
        var isFirstChunk =
            parentExpression is LiteralExpressionSyntax ||
            parentExpression is InterpolatedStringExpressionSyntax { Contents: var contents } && contents.First() == token.GetRequiredParent();

        if (parentExpression.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            return default;
        }

        // Use the parent multi-line expression to determine what whitespace to remove from the start of each line.
        var parentSourceText = parentExpression.SyntaxTree.GetText();
        var indentationLength = parentSourceText.Lines.GetLineFromPosition(parentExpression.Span.End).GetFirstNonWhitespaceOffset() ?? 0;

        // Create a source-text view over the token.  This makes it very easy to treat the token as a set of lines
        // that can be processed sensibly.
        var tokenSourceText = SourceText.From(token.Text);

        // If we're on the very first chunk of the multi-line raw string literal, then we want to start on line 1 so
        // we skip the space and newline that follow the initial `"""`.
        var startLineInclusive = tokenIncludeDelimiters ? 1 : 0;

        // Similarly, if we're on the very last chunk of hte multi-line raw string literal, then we don't want to
        // include the line contents for the line that has the final `    """` on it.
        var lastLineExclusive = tokenIncludeDelimiters ? tokenSourceText.Lines.Count - 1 : tokenSourceText.Lines.Count;

        var result = ImmutableList.CreateBuilder<VirtualChar>();
        for (var lineNumber = startLineInclusive; lineNumber < lastLineExclusive; lineNumber++)
        {
            var currentLine = tokenSourceText.Lines[lineNumber];
            var lineSpan = currentLine.Span;
            var lineStart = lineSpan.Start;

            // If we're on the second line onwards, we want to trim the indentation if we have it.  We also always
            // do this for the first line of the first chunk as that will contain the initial leading whitespace.
            if (isFirstChunk || lineNumber > startLineInclusive)
            {
                lineStart = lineSpan.Length > indentationLength
                    ? lineSpan.Start + indentationLength
                    : lineSpan.End;
            }

            // The last line of the last chunk does not include the final newline on the line.
            var lineEnd = lineNumber == lastLineExclusive - 1 ? currentLine.End : currentLine.EndIncludingLineBreak;

            // Now that we've found the start and end portions of that line, convert all the characters within to
            // virtual chars and return.
            for (var i = lineStart; i < lineEnd;)
            {
                i += ConvertTextAtIndexToRune(tokenSourceText, i, result, token.SpanStart);
            }
        }

        return VirtualCharSequence.Create(result.ToImmutable());
    }

    private static VirtualCharSequence TryConvertStringToVirtualChars(
        SyntaxToken token, string startDelimiter, string endDelimiter, bool escapeBraces)
    {
        var tokenText = token.Text;
        if (startDelimiter.Length > 0 && !tokenText.StartsWith(startDelimiter, StringComparison.Ordinal))
        {
            Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
            return default;
        }

        if (endDelimiter.Length > 0 && !tokenText.EndsWith(endDelimiter, StringComparison.OrdinalIgnoreCase))
        {
            Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
            return default;
        }

        var startIndexInclusive = startDelimiter.Length;
        var endIndexExclusive = tokenText.Length - endDelimiter.Length;

        // Do things in two passes.  First, convert everything in the string to a 16-bit-char+span.  Then walk
        // again, trying to create Runes from the 16-bit-chars. We do this to simplify complex cases where we may
        // have escapes and non-escapes mixed together.

        var charResults = new List<(char ch, TextSpan span)>();

        // First pass, just convert everything in the string (i.e. escapes) to plain 16-bit characters.
        var offset = token.SpanStart;
        for (var index = startIndexInclusive; index < endIndexExclusive;)
        {
            var ch = tokenText[index];
            if (ch == '\\')
            {
                if (!TryAddEscape(charResults, tokenText, offset, index))
                {
                    return default;
                }

                index += charResults.Last().span.Length;
            }
            else if (escapeBraces && IsOpenOrCloseBrace(ch))
            {
                if (!IsLegalBraceEscape(tokenText, index, offset, out var braceSpan))
                {
                    return default;
                }

                charResults.Add((ch, braceSpan));
                index += charResults.Last().span.Length;
            }
            else
            {
                charResults.Add((ch, new TextSpan(offset + index, 1)));
                index++;
            }
        }

        return CreateVirtualCharSequence(tokenText, offset, startIndexInclusive, endIndexExclusive, charResults);
    }

    private static VirtualCharSequence CreateVirtualCharSequence(
        string tokenText, int offset, int startIndexInclusive, int endIndexExclusive, List<(char ch, TextSpan span)> charResults)
    {
        // Second pass.  Convert those characters to Runes.
        var runeResults = ImmutableList.CreateBuilder<VirtualChar>();

        ConvertCharactersToRunes(charResults, runeResults);

        return CreateVirtualCharSequence(tokenText, offset, startIndexInclusive, endIndexExclusive, runeResults);
    }

    private static void ConvertCharactersToRunes(List<(char ch, TextSpan span)> charResults, ImmutableList<VirtualChar>.Builder runeResults)
    {
        for (var i = 0; i < charResults.Count;)
        {
            var (ch, span) = charResults[i];

            // First, see if this was a valid single char that can become a Rune.
            if (Rune.TryCreate(ch, out var rune))
            {
                runeResults.Add(VirtualChar.Create(rune, span));
                i++;
                continue;
            }

            // Next, see if we got at least a surrogate pair that can be converted into a Rune.
            if (i + 1 < charResults.Count)
            {
                var (nextCh, nextSpan) = charResults[i + 1];
                if (Rune.TryCreate(ch, nextCh, out rune))
                {
                    runeResults.Add(VirtualChar.Create(rune, TextSpan.FromBounds(span.Start, nextSpan.End)));
                    i += 2;
                    continue;
                }
            }

            // Had an unpaired surrogate.
            Debug.Assert(char.IsSurrogate(ch));
            runeResults.Add(VirtualChar.Create(ch, span));
            i++;
        }
    }

    private static bool TryAddEscape(
        List<(char ch, TextSpan span)> result, string tokenText, int offset, int index)
    {
        // Copied from Lexer.ScanEscapeSequence.
        Debug.Assert(tokenText[index] == '\\');

        return TryAddSingleCharacterEscape(result, tokenText, offset, index) ||
               TryAddMultiCharacterEscape(result, tokenText, offset, index);
    }

    public override bool TryGetEscapeCharacter(VirtualChar ch, out char escapedChar)
        => ch.TryGetEscapeCharacter(out escapedChar);

    private static bool TryAddSingleCharacterEscape(
        List<(char ch, TextSpan span)> result, string tokenText, int offset, int index)
    {
        // Copied from Lexer.ScanEscapeSequence.
        Debug.Assert(tokenText[index] == '\\');

        var ch = tokenText[index + 1];

        // Keep in sync with EscapeForRegularString
        switch (ch)
        {
            // escaped characters that translate to themselves
            case '\'':
            case '"':
            case '\\':
                break;
            // translate escapes as per C# spec 2.4.4.4
            case '0': ch = '\0'; break;
            case 'a': ch = '\a'; break;
            case 'b': ch = '\b'; break;
            case 'f': ch = '\f'; break;
            case 'n': ch = '\n'; break;
            case 'r': ch = '\r'; break;
            case 't': ch = '\t'; break;
            case 'v': ch = '\v'; break;
            default:
                return false;
        }

        result.Add((ch, new TextSpan(offset + index, 2)));
        return true;
    }

    private static bool TryAddMultiCharacterEscape(
        List<(char ch, TextSpan span)> result, string tokenText, int offset, int index)
    {
        // Copied from Lexer.ScanEscapeSequence.
        Debug.Assert(tokenText[index] == '\\');

        var ch = tokenText[index + 1];
        switch (ch)
        {
            case 'x':
            case 'u':
            case 'U':
                return TryAddMultiCharacterEscape(result, tokenText, offset, index, ch);
            default:
                Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                return false;
        }
    }

    private static bool TryAddMultiCharacterEscape(
        List<(char ch, TextSpan span)> result, string tokenText, int offset, int index, char character)
    {
        var startIndex = index;
        Debug.Assert(tokenText[index] == '\\');

        // skip past the / and the escape type.
        index += 2;
        if (character == 'U')
        {
            // 8 character escape.  May represent 1 or 2 actual chars.
            uint uintChar = 0;

            if (!IsHexDigit(tokenText[index]))
            {
                Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                return false;
            }

            for (var i = 0; i < 8; i++)
            {
                character = tokenText[index + i];
                if (!IsHexDigit(character))
                {
                    Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                    return false;
                }

                uintChar = (uint)((uintChar << 4) + HexValue(character));
            }

            // Copied from Lexer.cs and SlidingTextWindow.cs

            if (uintChar > 0x0010FFFF)
            {
                Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                return false;
            }

            if (uintChar < 0x00010000)
            {
                // something like \U0000000A
                //
                // Represents a single char value.
                result.Add(((char)uintChar, new TextSpan(startIndex + offset, 2 + 8)));
                return true;
            }
            else
            {
                Debug.Assert(uintChar is > 0x0000FFFF and <= 0x0010FFFF);
                var lowSurrogate = (uintChar - 0x00010000) % 0x0400 + 0xDC00;
                var highSurrogate = (uintChar - 0x00010000) / 0x0400 + 0xD800;

                // Encode this as a surrogate pair.
                var pos = startIndex + offset;
                result.Add(((char)highSurrogate, new TextSpan(pos, 0)));
                result.Add(((char)lowSurrogate, new TextSpan(pos, 2 + 8)));
                return true;
            }
        }
        else if (character == 'u')
        {
            // 4 character escape representing one char.

            var intChar = 0;
            if (!IsHexDigit(tokenText[index]))
            {
                Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                return false;
            }

            for (var i = 0; i < 4; i++)
            {
                var ch2 = tokenText[index + i];
                if (!IsHexDigit(ch2))
                {
                    Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                    return false;
                }

                intChar = (intChar << 4) + HexValue(ch2);
            }

            character = (char)intChar;
            result.Add((character, new TextSpan(startIndex + offset, 2 + 4)));
            return true;
        }
        else
        {
            Debug.Assert(character == 'x');
            // Variable length (up to 4 chars) hexadecimal escape.

            var intChar = 0;
            if (!IsHexDigit(tokenText[index]))
            {
                Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                return false;
            }

            var endIndex = index;
            for (var i = 0; i < 4 && endIndex < tokenText.Length; i++)
            {
                var ch2 = tokenText[index + i];
                if (!IsHexDigit(ch2))
                {
                    // This is possible.  These escape sequences are variable length.
                    break;
                }

                intChar = (intChar << 4) + HexValue(ch2);
                endIndex++;
            }

            character = (char)intChar;
            result.Add((character, TextSpan.FromBounds(startIndex + offset, endIndex + offset)));
            return true;
        }
    }

    private static int HexValue(char c)
    {
        Debug.Assert(IsHexDigit(c));
        return c is >= '0' and <= '9' ? c - '0' : (c & 0xdf) - 'A' + 10;
    }

    private static bool IsHexDigit(char c)
    {
        return c is >= '0' and <= '9' or
               >= 'A' and <= 'F' or
               >= 'a' and <= 'f';
    }
}
