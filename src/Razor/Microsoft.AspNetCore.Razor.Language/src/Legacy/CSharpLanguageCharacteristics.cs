// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class CSharpLanguageCharacteristics : LanguageCharacteristics<CSharpTokenizer>
{
    private static readonly Dictionary<SyntaxKind, string> _tokenSamples = new Dictionary<SyntaxKind, string>()
        {
            { SyntaxKind.Arrow, "->" },
            { SyntaxKind.Minus, "-" },
            { SyntaxKind.Decrement, "--" },
            { SyntaxKind.MinusAssign, "-=" },
            { SyntaxKind.NotEqual, "!=" },
            { SyntaxKind.Not, "!" },
            { SyntaxKind.Modulo, "%" },
            { SyntaxKind.ModuloAssign, "%=" },
            { SyntaxKind.AndAssign, "&=" },
            { SyntaxKind.And, "&" },
            { SyntaxKind.DoubleAnd, "&&" },
            { SyntaxKind.LeftParenthesis, "(" },
            { SyntaxKind.RightParenthesis, ")" },
            { SyntaxKind.Star, "*" },
            { SyntaxKind.MultiplyAssign, "*=" },
            { SyntaxKind.Comma, "," },
            { SyntaxKind.Dot, "." },
            { SyntaxKind.Slash, "/" },
            { SyntaxKind.DivideAssign, "/=" },
            { SyntaxKind.DoubleColon, "::" },
            { SyntaxKind.Colon, ":" },
            { SyntaxKind.Semicolon, ";" },
            { SyntaxKind.QuestionMark, "?" },
            { SyntaxKind.NullCoalesce, "??" },
            { SyntaxKind.RightBracket, "]" },
            { SyntaxKind.LeftBracket, "[" },
            { SyntaxKind.XorAssign, "^=" },
            { SyntaxKind.Xor, "^" },
            { SyntaxKind.LeftBrace, "{" },
            { SyntaxKind.OrAssign, "|=" },
            { SyntaxKind.DoubleOr, "||" },
            { SyntaxKind.Or, "|" },
            { SyntaxKind.RightBrace, "}" },
            { SyntaxKind.Tilde, "~" },
            { SyntaxKind.Plus, "+" },
            { SyntaxKind.PlusAssign, "+=" },
            { SyntaxKind.Increment, "++" },
            { SyntaxKind.LessThan, "<" },
            { SyntaxKind.LessThanEqual, "<=" },
            { SyntaxKind.LeftShift, "<<" },
            { SyntaxKind.LeftShiftAssign, "<<=" },
            { SyntaxKind.Assign, "=" },
            { SyntaxKind.Equals, "==" },
            { SyntaxKind.GreaterThan, ">" },
            { SyntaxKind.GreaterThanEqual, ">=" },
            { SyntaxKind.RightShift, ">>" },
            { SyntaxKind.RightShiftAssign, ">>=" },
            { SyntaxKind.Hash, "#" },
            { SyntaxKind.Transition, "@" },
        };

    // Allows performance optimization of GetKeyword such that it need not do Enum.ToString
    private static readonly IReadOnlyDictionary<CSharpKeyword, string> _keywordNames = new Dictionary<CSharpKeyword, string>()
        {
            { CSharpKeyword.Await, "await" },
            { CSharpKeyword.Abstract, "abstract" },
            { CSharpKeyword.Byte, "byte" },
            { CSharpKeyword.Class, "class" },
            { CSharpKeyword.Delegate, "delegate" },
            { CSharpKeyword.Event, "event" },
            { CSharpKeyword.Fixed, "fixed" },
            { CSharpKeyword.If, "if" },
            { CSharpKeyword.Internal, "internal" },
            { CSharpKeyword.New, "new" },
            { CSharpKeyword.Override, "override" },
            { CSharpKeyword.Readonly, "readonly" },
            { CSharpKeyword.Short, "short" },
            { CSharpKeyword.Struct, "struct" },
            { CSharpKeyword.Try, "try" },
            { CSharpKeyword.Unsafe, "unsafe" },
            { CSharpKeyword.Volatile, "volatile" },
            { CSharpKeyword.As, "as" },
            { CSharpKeyword.Do, "do" },
            { CSharpKeyword.Is, "is" },
            { CSharpKeyword.Params, "params" },
            { CSharpKeyword.Ref, "ref" },
            { CSharpKeyword.Switch, "switch" },
            { CSharpKeyword.Ushort, "ushort" },
            { CSharpKeyword.While, "while" },
            { CSharpKeyword.Case, "case" },
            { CSharpKeyword.Const, "const" },
            { CSharpKeyword.Explicit, "explicit" },
            { CSharpKeyword.Float, "float" },
            { CSharpKeyword.Null, "null" },
            { CSharpKeyword.Sizeof, "sizeof" },
            { CSharpKeyword.Typeof, "typeof" },
            { CSharpKeyword.Implicit, "implicit" },
            { CSharpKeyword.Private, "private" },
            { CSharpKeyword.This, "this" },
            { CSharpKeyword.Using, "using" },
            { CSharpKeyword.Extern, "extern" },
            { CSharpKeyword.Return, "return" },
            { CSharpKeyword.Stackalloc, "stackalloc" },
            { CSharpKeyword.Uint, "uint" },
            { CSharpKeyword.Base, "base" },
            { CSharpKeyword.Catch, "catch" },
            { CSharpKeyword.Continue, "continue" },
            { CSharpKeyword.Double, "double" },
            { CSharpKeyword.For, "for" },
            { CSharpKeyword.In, "in" },
            { CSharpKeyword.Lock, "lock" },
            { CSharpKeyword.Object, "object" },
            { CSharpKeyword.Protected, "protected" },
            { CSharpKeyword.Static, "static" },
            { CSharpKeyword.False, "false" },
            { CSharpKeyword.Public, "public" },
            { CSharpKeyword.Sbyte, "sbyte" },
            { CSharpKeyword.Throw, "throw" },
            { CSharpKeyword.Virtual, "virtual" },
            { CSharpKeyword.Decimal, "decimal" },
            { CSharpKeyword.Else, "else" },
            { CSharpKeyword.Operator, "operator" },
            { CSharpKeyword.String, "string" },
            { CSharpKeyword.Ulong, "ulong" },
            { CSharpKeyword.Bool, "bool" },
            { CSharpKeyword.Char, "char" },
            { CSharpKeyword.Default, "default" },
            { CSharpKeyword.Foreach, "foreach" },
            { CSharpKeyword.Long, "long" },
            { CSharpKeyword.Void, "void" },
            { CSharpKeyword.Enum, "enum" },
            { CSharpKeyword.Finally, "finally" },
            { CSharpKeyword.Int, "int" },
            { CSharpKeyword.Out, "out" },
            { CSharpKeyword.Sealed, "sealed" },
            { CSharpKeyword.True, "true" },
            { CSharpKeyword.Goto, "goto" },
            { CSharpKeyword.Unchecked, "unchecked" },
            { CSharpKeyword.Interface, "interface" },
            { CSharpKeyword.Break, "break" },
            { CSharpKeyword.Checked, "checked" },
            { CSharpKeyword.Namespace, "namespace" },
            { CSharpKeyword.When,  "when" },
            { CSharpKeyword.Where,  "where" },
        };

    private static readonly CSharpLanguageCharacteristics _instance = new CSharpLanguageCharacteristics();

    protected CSharpLanguageCharacteristics()
    {
#if DEBUG
        var values = Enum.GetValues(typeof(CSharpKeyword));

        Debug.Assert(values.Length == _keywordNames.Count, "_keywordNames and CSharpKeyword are out of sync");
        for (var i = 0; i < values.Length; i++)
        {
            var keyword = (CSharpKeyword)values.GetValue(i);

            var expectedValue = keyword.ToString().ToLowerInvariant();
            var actualValue = _keywordNames[keyword];

            Debug.Assert(expectedValue == actualValue, "_keywordNames and CSharpKeyword are out of sync for " + expectedValue);
        }
#endif
    }

    public static CSharpLanguageCharacteristics Instance => _instance;

    public override CSharpTokenizer CreateTokenizer(ITextDocument source)
    {
        return new CSharpTokenizer(source);
    }

    protected override SyntaxToken CreateToken(string content, SyntaxKind kind, RazorDiagnostic[] errors)
    {
        return SyntaxFactory.Token(kind, content, errors);
    }

    public override string GetSample(SyntaxKind kind)
    {
        string sample;
        if (!_tokenSamples.TryGetValue(kind, out sample))
        {
            switch (kind)
            {
                case SyntaxKind.Identifier:
                    return Resources.CSharpToken_Identifier;
                case SyntaxKind.Keyword:
                    return Resources.CSharpToken_Keyword;
                case SyntaxKind.IntegerLiteral:
                    return Resources.CSharpToken_IntegerLiteral;
                case SyntaxKind.NewLine:
                    return Resources.CSharpToken_Newline;
                case SyntaxKind.Whitespace:
                    return Resources.CSharpToken_Whitespace;
                case SyntaxKind.CSharpComment:
                    return Resources.CSharpToken_Comment;
                case SyntaxKind.RealLiteral:
                    return Resources.CSharpToken_RealLiteral;
                case SyntaxKind.CharacterLiteral:
                    return Resources.CSharpToken_CharacterLiteral;
                case SyntaxKind.StringLiteral:
                    return Resources.CSharpToken_StringLiteral;
                default:
                    return Resources.Token_Unknown;
            }
        }
        return sample;
    }

    public override SyntaxToken CreateMarkerToken()
    {
        return SyntaxFactory.Token(SyntaxKind.Marker, string.Empty);
    }

    public override SyntaxKind GetKnownTokenType(KnownTokenType type)
    {
        switch (type)
        {
            case KnownTokenType.Identifier:
                return SyntaxKind.Identifier;
            case KnownTokenType.Keyword:
                return SyntaxKind.Keyword;
            case KnownTokenType.NewLine:
                return SyntaxKind.NewLine;
            case KnownTokenType.Whitespace:
                return SyntaxKind.Whitespace;
            case KnownTokenType.Transition:
                return SyntaxKind.Transition;
            case KnownTokenType.CommentStart:
                return SyntaxKind.RazorCommentTransition;
            case KnownTokenType.CommentStar:
                return SyntaxKind.RazorCommentStar;
            case KnownTokenType.CommentBody:
                return SyntaxKind.RazorCommentLiteral;
            default:
                return SyntaxKind.Marker;
        }
    }

    public override SyntaxKind FlipBracket(SyntaxKind bracket)
    {
        switch (bracket)
        {
            case SyntaxKind.LeftBrace:
                return SyntaxKind.RightBrace;
            case SyntaxKind.LeftBracket:
                return SyntaxKind.RightBracket;
            case SyntaxKind.LeftParenthesis:
                return SyntaxKind.RightParenthesis;
            case SyntaxKind.LessThan:
                return SyntaxKind.GreaterThan;
            case SyntaxKind.RightBrace:
                return SyntaxKind.LeftBrace;
            case SyntaxKind.RightBracket:
                return SyntaxKind.LeftBracket;
            case SyntaxKind.RightParenthesis:
                return SyntaxKind.LeftParenthesis;
            case SyntaxKind.GreaterThan:
                return SyntaxKind.LessThan;
            default:
                Debug.Fail("FlipBracket must be called with a bracket character");
                return SyntaxKind.Marker;
        }
    }

    public static string GetKeyword(CSharpKeyword keyword)
    {
        if (!_keywordNames.TryGetValue(keyword, out var value))
        {
            value = keyword.ToString().ToLowerInvariant();
        }

        return value;
    }
}
