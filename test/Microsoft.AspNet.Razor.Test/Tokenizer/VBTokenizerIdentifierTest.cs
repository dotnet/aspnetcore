// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Tokenizer
{
    public class VBTokenizerIdentifierTest : VBTokenizerTestBase
    {
        [Fact]
        public void Simple_Identifier_Is_Recognized()
        {
            TestTokenizer("foo", new VBSymbol(0, 0, 0, "foo", VBSymbolType.Identifier));
        }

        [Fact]
        public void Escaped_Identifier_Terminates_At_EOF()
        {
            TestTokenizer("[foo", new VBSymbol(0, 0, 0, "[foo", VBSymbolType.Identifier));
        }

        [Fact]
        public void Escaped_Identifier_Terminates_At_Whitespace()
        {
            TestTokenizer("[foo ", new VBSymbol(0, 0, 0, "[foo", VBSymbolType.Identifier), IgnoreRemaining);
        }

        [Fact]
        public void Escaped_Identifier_Terminates_At_RightBracket_And_Does_Not_Read_TypeCharacter()
        {
            TestTokenizer("[foo]&", new VBSymbol(0, 0, 0, "[foo]", VBSymbolType.Identifier), IgnoreRemaining);
        }

        [Fact]
        public void Identifier_Starting_With_Underscore_Is_Recognized()
        {
            TestTokenizer("_foo", new VBSymbol(0, 0, 0, "_foo", VBSymbolType.Identifier));
        }

        [Fact]
        public void Identifier_Can_Contain_Digits()
        {
            TestTokenizer("foo4", new VBSymbol(0, 0, 0, "foo4", VBSymbolType.Identifier));
        }

        [Fact]
        public void Identifier_Can_Start_With_Titlecase_Letter()
        {
            TestTokenizer("ῼfoo", new VBSymbol(0, 0, 0, "ῼfoo", VBSymbolType.Identifier));
        }

        [Fact]
        public void Identifier_Can_Start_With_Letter_Modifier()
        {
            TestTokenizer("ᵊfoo", new VBSymbol(0, 0, 0, "ᵊfoo", VBSymbolType.Identifier));
        }

        [Fact]
        public void Identifier_Can_Start_With_Other_Letter()
        {
            TestTokenizer("ƻfoo", new VBSymbol(0, 0, 0, "ƻfoo", VBSymbolType.Identifier));
        }

        [Fact]
        public void Identifier_Can_Start_With_Number_Letter()
        {
            TestTokenizer("Ⅽool", new VBSymbol(0, 0, 0, "Ⅽool", VBSymbolType.Identifier));
        }

        [Fact]
        public void Identifier_Can_Contain_Non_Spacing_Mark()
        {
            TestTokenizer("foo\u0300", new VBSymbol(0, 0, 0, "foo\u0300", VBSymbolType.Identifier));
        }

        [Fact]
        public void Identifier_Can_Contain_Spacing_Combining_Mark()
        {
            TestTokenizer("fooः", new VBSymbol(0, 0, 0, "fooः", VBSymbolType.Identifier));
        }

        [Fact]
        public void Identifier_Can_Contain_Non_English_Digit()
        {
            TestTokenizer("foo١", new VBSymbol(0, 0, 0, "foo١", VBSymbolType.Identifier));
        }

        [Fact]
        public void Identifier_Can_Contain_Connector_Punctuation()
        {
            TestTokenizer("foo‿bar", new VBSymbol(0, 0, 0, "foo‿bar", VBSymbolType.Identifier));
        }

        [Fact]
        public void Identifier_Can_Contain_Format_Character()
        {
            TestTokenizer("foo؃bar", new VBSymbol(0, 0, 0, "foo؃bar", VBSymbolType.Identifier));
        }

        [Fact]
        public void Escaped_Keyword_Is_Recognized_As_Identifier()
        {
            TestSingleToken("[AddHandler]", VBSymbolType.Identifier);
        }

        [Fact]
        public void Keywords_Are_Recognized_As_Keyword_Tokens()
        {
            TestKeyword("AddHandler", VBKeyword.AddHandler);
            TestKeyword("AndAlso", VBKeyword.AndAlso);
            TestKeyword("Byte", VBKeyword.Byte);
            TestKeyword("Catch", VBKeyword.Catch);
            TestKeyword("CDate", VBKeyword.CDate);
            TestKeyword("CInt", VBKeyword.CInt);
            TestKeyword("Const", VBKeyword.Const);
            TestKeyword("CSng", VBKeyword.CSng);
            TestKeyword("CULng", VBKeyword.CULng);
            TestKeyword("Declare", VBKeyword.Declare);
            TestKeyword("DirectCast", VBKeyword.DirectCast);
            TestKeyword("Else", VBKeyword.Else);
            TestKeyword("Enum", VBKeyword.Enum);
            TestKeyword("Exit", VBKeyword.Exit);
            TestKeyword("Friend", VBKeyword.Friend);
            TestKeyword("GetXmlNamespace", VBKeyword.GetXmlNamespace);
            TestKeyword("Handles", VBKeyword.Handles);
            TestKeyword("In", VBKeyword.In);
            TestKeyword("Is", VBKeyword.Is);
            TestKeyword("Like", VBKeyword.Like);
            TestKeyword("Mod", VBKeyword.Mod);
            TestKeyword("MyBase", VBKeyword.MyBase);
            TestKeyword("New", VBKeyword.New);
            TestKeyword("AddressOf", VBKeyword.AddressOf);
            TestKeyword("As", VBKeyword.As);
            TestKeyword("ByVal", VBKeyword.ByVal);
            TestKeyword("CBool", VBKeyword.CBool);
            TestKeyword("CDbl", VBKeyword.CDbl);
            TestKeyword("Class", VBKeyword.Class);
            TestKeyword("Continue", VBKeyword.Continue);
            TestKeyword("CStr", VBKeyword.CStr);
            TestKeyword("CUShort", VBKeyword.CUShort);
            TestKeyword("Default", VBKeyword.Default);
            TestKeyword("Do", VBKeyword.Do);
            TestKeyword("ElseIf", VBKeyword.ElseIf);
            TestKeyword("Erase", VBKeyword.Erase);
            TestKeyword("False", VBKeyword.False);
            TestKeyword("Function", VBKeyword.Function);
            TestKeyword("Global", VBKeyword.Global);
            TestKeyword("If", VBKeyword.If);
            TestKeyword("Inherits", VBKeyword.Inherits);
            TestKeyword("IsNot", VBKeyword.IsNot);
            TestKeyword("Long", VBKeyword.Long);
            TestKeyword("Module", VBKeyword.Module);
            TestKeyword("MyClass", VBKeyword.MyClass);
            TestKeyword("Next", VBKeyword.Next);
            TestKeyword("Alias", VBKeyword.Alias);
            TestKeyword("Boolean", VBKeyword.Boolean);
            TestKeyword("Call", VBKeyword.Call);
            TestKeyword("CByte", VBKeyword.CByte);
            TestKeyword("CDec", VBKeyword.CDec);
            TestKeyword("CLng", VBKeyword.CLng);
            TestKeyword("CSByte", VBKeyword.CSByte);
            TestKeyword("CType", VBKeyword.CType);
            TestKeyword("Date", VBKeyword.Date);
            TestKeyword("Delegate", VBKeyword.Delegate);
            TestKeyword("Double", VBKeyword.Double);
            TestKeyword("End", VBKeyword.End);
            TestKeyword("Error", VBKeyword.Error);
            TestKeyword("Finally", VBKeyword.Finally);
            TestKeyword("Get", VBKeyword.Get);
            TestKeyword("GoSub", VBKeyword.GoSub);
            TestKeyword("Implements", VBKeyword.Implements);
            TestKeyword("Integer", VBKeyword.Integer);
            TestKeyword("Let", VBKeyword.Let);
            TestKeyword("Loop", VBKeyword.Loop);
            TestKeyword("MustInherit", VBKeyword.MustInherit);
            TestKeyword("Namespace", VBKeyword.Namespace);
            TestKeyword("Not", VBKeyword.Not);
            TestKeyword("And", VBKeyword.And);
            TestKeyword("ByRef", VBKeyword.ByRef);
            TestKeyword("Case", VBKeyword.Case);
            TestKeyword("CChar", VBKeyword.CChar);
            TestKeyword("Char", VBKeyword.Char);
            TestKeyword("CObj", VBKeyword.CObj);
            TestKeyword("CShort", VBKeyword.CShort);
            TestKeyword("CUInt", VBKeyword.CUInt);
            TestKeyword("Decimal", VBKeyword.Decimal);
            TestKeyword("Dim", VBKeyword.Dim);
            TestKeyword("Each", VBKeyword.Each);
            TestKeyword("EndIf", VBKeyword.EndIf);
            TestKeyword("Event", VBKeyword.Event);
            TestKeyword("For", VBKeyword.For);
            TestKeyword("GetType", VBKeyword.GetType);
            TestKeyword("GoTo", VBKeyword.GoTo);
            TestKeyword("Imports", VBKeyword.Imports);
            TestKeyword("Interface", VBKeyword.Interface);
            TestKeyword("Lib", VBKeyword.Lib);
            TestKeyword("Me", VBKeyword.Me);
            TestKeyword("MustOverride", VBKeyword.MustOverride);
            TestKeyword("Narrowing", VBKeyword.Narrowing);
            TestKeyword("Nothing", VBKeyword.Nothing);
            TestKeyword("NotInheritable", VBKeyword.NotInheritable);
            TestKeyword("On", VBKeyword.On);
            TestKeyword("Or", VBKeyword.Or);
            TestKeyword("Overrides", VBKeyword.Overrides);
            TestKeyword("Property", VBKeyword.Property);
            TestKeyword("ReadOnly", VBKeyword.ReadOnly);
            TestKeyword("Resume", VBKeyword.Resume);
            TestKeyword("Set", VBKeyword.Set);
            TestKeyword("Single", VBKeyword.Single);
            TestKeyword("String", VBKeyword.String);
            TestKeyword("Then", VBKeyword.Then);
            TestKeyword("Try", VBKeyword.Try);
            TestKeyword("ULong", VBKeyword.ULong);
            TestKeyword("Wend", VBKeyword.Wend);
            TestKeyword("With", VBKeyword.With);
            TestKeyword("NotOverridable", VBKeyword.NotOverridable);
            TestKeyword("Operator", VBKeyword.Operator);
            TestKeyword("OrElse", VBKeyword.OrElse);
            TestKeyword("ParamArray", VBKeyword.ParamArray);
            TestKeyword("Protected", VBKeyword.Protected);
            TestKeyword("ReDim", VBKeyword.ReDim);
            TestKeyword("Return", VBKeyword.Return);
            TestKeyword("Shadows", VBKeyword.Shadows);
            TestKeyword("Static", VBKeyword.Static);
            TestKeyword("Structure", VBKeyword.Structure);
            TestKeyword("Throw", VBKeyword.Throw);
            TestKeyword("TryCast", VBKeyword.TryCast);
            TestKeyword("UShort", VBKeyword.UShort);
            TestKeyword("When", VBKeyword.When);
            TestKeyword("WithEvents", VBKeyword.WithEvents);
            TestKeyword("Object", VBKeyword.Object);
            TestKeyword("Option", VBKeyword.Option);
            TestKeyword("Overloads", VBKeyword.Overloads);
            TestKeyword("Partial", VBKeyword.Partial);
            TestKeyword("Public", VBKeyword.Public);
            TestKeyword("SByte", VBKeyword.SByte);
            TestKeyword("Shared", VBKeyword.Shared);
            TestKeyword("Step", VBKeyword.Step);
            TestKeyword("Sub", VBKeyword.Sub);
            TestKeyword("To", VBKeyword.To);
            TestKeyword("TypeOf", VBKeyword.TypeOf);
            TestKeyword("Using", VBKeyword.Using);
            TestKeyword("While", VBKeyword.While);
            TestKeyword("WriteOnly", VBKeyword.WriteOnly);
            TestKeyword("Of", VBKeyword.Of);
            TestKeyword("Optional", VBKeyword.Optional);
            TestKeyword("Overridable", VBKeyword.Overridable);
            TestKeyword("Private", VBKeyword.Private);
            TestKeyword("RaiseEvent", VBKeyword.RaiseEvent);
            TestKeyword("RemoveHandler", VBKeyword.RemoveHandler);
            TestKeyword("Select", VBKeyword.Select);
            TestKeyword("Short", VBKeyword.Short);
            TestKeyword("Stop", VBKeyword.Stop);
            TestKeyword("SyncLock", VBKeyword.SyncLock);
            TestKeyword("True", VBKeyword.True);
            TestKeyword("UInteger", VBKeyword.UInteger);
            TestKeyword("Variant", VBKeyword.Variant);
            TestKeyword("Widening", VBKeyword.Widening);
            TestKeyword("Xor", VBKeyword.Xor);
        }

        private void TestKeyword(string keyword, VBKeyword keywordType)
        {
            TestTokenizer(keyword, new VBSymbol(0, 0, 0, keyword, VBSymbolType.Keyword) { Keyword = keywordType });
        }
    }
}
