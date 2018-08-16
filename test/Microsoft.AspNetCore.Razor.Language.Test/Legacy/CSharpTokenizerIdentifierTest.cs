// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpTokenizerIdentifierTest : CSharpTokenizerTestBase
    {
        [Fact]
        public void Simple_Identifier_Is_Recognized()
        {
            TestTokenizer("foo", SyntaxFactory.Token(SyntaxKind.Identifier, "foo"));
        }

        [Fact]
        public void Identifier_Starting_With_Underscore_Is_Recognized()
        {
            TestTokenizer("_foo", SyntaxFactory.Token(SyntaxKind.Identifier, "_foo"));
        }

        [Fact]
        public void Identifier_Can_Contain_Digits()
        {
            TestTokenizer("foo4", SyntaxFactory.Token(SyntaxKind.Identifier, "foo4"));
        }

        [Fact]
        public void Identifier_Can_Start_With_Titlecase_Letter()
        {
            TestTokenizer("ῼfoo", SyntaxFactory.Token(SyntaxKind.Identifier, "ῼfoo"));
        }

        [Fact]
        public void Identifier_Can_Start_With_Letter_Modifier()
        {
            TestTokenizer("ᵊfoo", SyntaxFactory.Token(SyntaxKind.Identifier, "ᵊfoo"));
        }

        [Fact]
        public void Identifier_Can_Start_With_Other_Letter()
        {
            TestTokenizer("ƻfoo", SyntaxFactory.Token(SyntaxKind.Identifier, "ƻfoo"));
        }

        [Fact]
        public void Identifier_Can_Start_With_Number_Letter()
        {
            TestTokenizer("Ⅽool", SyntaxFactory.Token(SyntaxKind.Identifier, "Ⅽool"));
        }

        [Fact]
        public void Identifier_Can_Contain_Non_Spacing_Mark()
        {
            TestTokenizer("foo\u0300", SyntaxFactory.Token(SyntaxKind.Identifier, "foo\u0300"));
        }

        [Fact]
        public void Identifier_Can_Contain_Spacing_Combining_Mark()
        {
            TestTokenizer("fooः", SyntaxFactory.Token(SyntaxKind.Identifier, "fooः"));
        }

        [Fact]
        public void Identifier_Can_Contain_Non_English_Digit()
        {
            TestTokenizer("foo١", SyntaxFactory.Token(SyntaxKind.Identifier, "foo١"));
        }

        [Fact]
        public void Identifier_Can_Contain_Connector_Punctuation()
        {
            TestTokenizer("foo‿bar", SyntaxFactory.Token(SyntaxKind.Identifier, "foo‿bar"));
        }

        [Fact]
        public void Identifier_Can_Contain_Format_Character()
        {
            TestTokenizer("foo؃bar", SyntaxFactory.Token(SyntaxKind.Identifier, "foo؃bar"));
        }

        [Fact]
        public void Keywords_Are_Recognized_As_Keyword_Tokens()
        {
            TestKeyword("abstract", CSharpKeyword.Abstract);
            TestKeyword("byte", CSharpKeyword.Byte);
            TestKeyword("class", CSharpKeyword.Class);
            TestKeyword("delegate", CSharpKeyword.Delegate);
            TestKeyword("event", CSharpKeyword.Event);
            TestKeyword("fixed", CSharpKeyword.Fixed);
            TestKeyword("if", CSharpKeyword.If);
            TestKeyword("internal", CSharpKeyword.Internal);
            TestKeyword("new", CSharpKeyword.New);
            TestKeyword("override", CSharpKeyword.Override);
            TestKeyword("readonly", CSharpKeyword.Readonly);
            TestKeyword("short", CSharpKeyword.Short);
            TestKeyword("struct", CSharpKeyword.Struct);
            TestKeyword("try", CSharpKeyword.Try);
            TestKeyword("unsafe", CSharpKeyword.Unsafe);
            TestKeyword("volatile", CSharpKeyword.Volatile);
            TestKeyword("as", CSharpKeyword.As);
            TestKeyword("do", CSharpKeyword.Do);
            TestKeyword("is", CSharpKeyword.Is);
            TestKeyword("params", CSharpKeyword.Params);
            TestKeyword("ref", CSharpKeyword.Ref);
            TestKeyword("switch", CSharpKeyword.Switch);
            TestKeyword("ushort", CSharpKeyword.Ushort);
            TestKeyword("while", CSharpKeyword.While);
            TestKeyword("case", CSharpKeyword.Case);
            TestKeyword("const", CSharpKeyword.Const);
            TestKeyword("explicit", CSharpKeyword.Explicit);
            TestKeyword("float", CSharpKeyword.Float);
            TestKeyword("null", CSharpKeyword.Null);
            TestKeyword("sizeof", CSharpKeyword.Sizeof);
            TestKeyword("typeof", CSharpKeyword.Typeof);
            TestKeyword("implicit", CSharpKeyword.Implicit);
            TestKeyword("private", CSharpKeyword.Private);
            TestKeyword("this", CSharpKeyword.This);
            TestKeyword("using", CSharpKeyword.Using);
            TestKeyword("extern", CSharpKeyword.Extern);
            TestKeyword("return", CSharpKeyword.Return);
            TestKeyword("stackalloc", CSharpKeyword.Stackalloc);
            TestKeyword("uint", CSharpKeyword.Uint);
            TestKeyword("base", CSharpKeyword.Base);
            TestKeyword("catch", CSharpKeyword.Catch);
            TestKeyword("continue", CSharpKeyword.Continue);
            TestKeyword("double", CSharpKeyword.Double);
            TestKeyword("for", CSharpKeyword.For);
            TestKeyword("in", CSharpKeyword.In);
            TestKeyword("lock", CSharpKeyword.Lock);
            TestKeyword("object", CSharpKeyword.Object);
            TestKeyword("protected", CSharpKeyword.Protected);
            TestKeyword("static", CSharpKeyword.Static);
            TestKeyword("false", CSharpKeyword.False);
            TestKeyword("public", CSharpKeyword.Public);
            TestKeyword("sbyte", CSharpKeyword.Sbyte);
            TestKeyword("throw", CSharpKeyword.Throw);
            TestKeyword("virtual", CSharpKeyword.Virtual);
            TestKeyword("decimal", CSharpKeyword.Decimal);
            TestKeyword("else", CSharpKeyword.Else);
            TestKeyword("operator", CSharpKeyword.Operator);
            TestKeyword("string", CSharpKeyword.String);
            TestKeyword("ulong", CSharpKeyword.Ulong);
            TestKeyword("bool", CSharpKeyword.Bool);
            TestKeyword("char", CSharpKeyword.Char);
            TestKeyword("default", CSharpKeyword.Default);
            TestKeyword("foreach", CSharpKeyword.Foreach);
            TestKeyword("long", CSharpKeyword.Long);
            TestKeyword("void", CSharpKeyword.Void);
            TestKeyword("enum", CSharpKeyword.Enum);
            TestKeyword("finally", CSharpKeyword.Finally);
            TestKeyword("int", CSharpKeyword.Int);
            TestKeyword("out", CSharpKeyword.Out);
            TestKeyword("sealed", CSharpKeyword.Sealed);
            TestKeyword("true", CSharpKeyword.True);
            TestKeyword("goto", CSharpKeyword.Goto);
            TestKeyword("unchecked", CSharpKeyword.Unchecked);
            TestKeyword("interface", CSharpKeyword.Interface);
            TestKeyword("break", CSharpKeyword.Break);
            TestKeyword("checked", CSharpKeyword.Checked);
            TestKeyword("namespace", CSharpKeyword.Namespace);
            TestKeyword("when", CSharpKeyword.When);
        }

        private void TestKeyword(string keyword, CSharpKeyword keywordType)
        {
            TestTokenizer(keyword, SyntaxFactory.Token(SyntaxKind.Keyword, keyword));
        }
    }
}
