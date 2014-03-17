// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Tokenizer
{
    internal static class CSharpKeywordDetector
    {
        private static readonly Dictionary<string, CSharpKeyword> _keywords = new Dictionary<string, CSharpKeyword>(StringComparer.Ordinal)
        {
            { "await", CSharpKeyword.Await },
            { "abstract", CSharpKeyword.Abstract },
            { "byte", CSharpKeyword.Byte },
            { "class", CSharpKeyword.Class },
            { "delegate", CSharpKeyword.Delegate },
            { "event", CSharpKeyword.Event },
            { "fixed", CSharpKeyword.Fixed },
            { "if", CSharpKeyword.If },
            { "internal", CSharpKeyword.Internal },
            { "new", CSharpKeyword.New },
            { "override", CSharpKeyword.Override },
            { "readonly", CSharpKeyword.Readonly },
            { "short", CSharpKeyword.Short },
            { "struct", CSharpKeyword.Struct },
            { "try", CSharpKeyword.Try },
            { "unsafe", CSharpKeyword.Unsafe },
            { "volatile", CSharpKeyword.Volatile },
            { "as", CSharpKeyword.As },
            { "do", CSharpKeyword.Do },
            { "is", CSharpKeyword.Is },
            { "params", CSharpKeyword.Params },
            { "ref", CSharpKeyword.Ref },
            { "switch", CSharpKeyword.Switch },
            { "ushort", CSharpKeyword.Ushort },
            { "while", CSharpKeyword.While },
            { "case", CSharpKeyword.Case },
            { "const", CSharpKeyword.Const },
            { "explicit", CSharpKeyword.Explicit },
            { "float", CSharpKeyword.Float },
            { "null", CSharpKeyword.Null },
            { "sizeof", CSharpKeyword.Sizeof },
            { "typeof", CSharpKeyword.Typeof },
            { "implicit", CSharpKeyword.Implicit },
            { "private", CSharpKeyword.Private },
            { "this", CSharpKeyword.This },
            { "using", CSharpKeyword.Using },
            { "extern", CSharpKeyword.Extern },
            { "return", CSharpKeyword.Return },
            { "stackalloc", CSharpKeyword.Stackalloc },
            { "uint", CSharpKeyword.Uint },
            { "base", CSharpKeyword.Base },
            { "catch", CSharpKeyword.Catch },
            { "continue", CSharpKeyword.Continue },
            { "double", CSharpKeyword.Double },
            { "for", CSharpKeyword.For },
            { "in", CSharpKeyword.In },
            { "lock", CSharpKeyword.Lock },
            { "object", CSharpKeyword.Object },
            { "protected", CSharpKeyword.Protected },
            { "static", CSharpKeyword.Static },
            { "false", CSharpKeyword.False },
            { "public", CSharpKeyword.Public },
            { "sbyte", CSharpKeyword.Sbyte },
            { "throw", CSharpKeyword.Throw },
            { "virtual", CSharpKeyword.Virtual },
            { "decimal", CSharpKeyword.Decimal },
            { "else", CSharpKeyword.Else },
            { "operator", CSharpKeyword.Operator },
            { "string", CSharpKeyword.String },
            { "ulong", CSharpKeyword.Ulong },
            { "bool", CSharpKeyword.Bool },
            { "char", CSharpKeyword.Char },
            { "default", CSharpKeyword.Default },
            { "foreach", CSharpKeyword.Foreach },
            { "long", CSharpKeyword.Long },
            { "void", CSharpKeyword.Void },
            { "enum", CSharpKeyword.Enum },
            { "finally", CSharpKeyword.Finally },
            { "int", CSharpKeyword.Int },
            { "out", CSharpKeyword.Out },
            { "sealed", CSharpKeyword.Sealed },
            { "true", CSharpKeyword.True },
            { "goto", CSharpKeyword.Goto },
            { "unchecked", CSharpKeyword.Unchecked },
            { "interface", CSharpKeyword.Interface },
            { "break", CSharpKeyword.Break },
            { "checked", CSharpKeyword.Checked },
            { "namespace", CSharpKeyword.Namespace }
        };

        public static CSharpKeyword? SymbolTypeForIdentifier(string id)
        {
            CSharpKeyword type;
            if (!_keywords.TryGetValue(id, out type))
            {
                return null;
            }
            return type;
        }
    }
}
