// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

// Centralized all the supressions for the CSharpSymbolType and VBSymbolType enum members here for clarity. They are
// not in the CodeAnalysisDictionary because they are special case exclusions

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Foreach", Scope = "member", Target = "System.Web.Razor.Tokenizer.Symbols.CSharpKeyword.#Foreach", Justification = Justifications.SymbolTypeNames)]
[assembly: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Readonly", Scope = "member", Target = "System.Web.Razor.Tokenizer.Symbols.CSharpKeyword.#Readonly", Justification = Justifications.SymbolTypeNames)]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sbyte", Scope = "member", Target = "System.Web.Razor.Tokenizer.Symbols.CSharpKeyword.#Sbyte", Justification = Justifications.SymbolTypeNames)]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sizeof", Scope = "member", Target = "System.Web.Razor.Tokenizer.Symbols.CSharpKeyword.#Sizeof", Justification = Justifications.SymbolTypeNames)]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Stackalloc", Scope = "member", Target = "System.Web.Razor.Tokenizer.Symbols.CSharpKeyword.#Stackalloc", Justification = Justifications.SymbolTypeNames)]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Typeof", Scope = "member", Target = "System.Web.Razor.Tokenizer.Symbols.CSharpKeyword.#Typeof", Justification = Justifications.SymbolTypeNames)]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Uint", Scope = "member", Target = "System.Web.Razor.Tokenizer.Symbols.CSharpKeyword.#Uint", Justification = Justifications.SymbolTypeNames)]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ulong", Scope = "member", Target = "System.Web.Razor.Tokenizer.Symbols.CSharpKeyword.#Ulong", Justification = Justifications.SymbolTypeNames)]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ushort", Scope = "member", Target = "System.Web.Razor.Tokenizer.Symbols.CSharpKeyword.#Ushort", Justification = Justifications.SymbolTypeNames)]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Val", Scope = "member", Target = "System.Web.Razor.Tokenizer.Symbols.VBKeyword.#ByVal", Justification = Justifications.SymbolTypeNames)]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sng", Scope = "member", Target = "System.Web.Razor.Tokenizer.Symbols.VBKeyword.#CSng", Justification = Justifications.SymbolTypeNames)]
[assembly: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "ReDim", Scope = "member", Target = "System.Web.Razor.Tokenizer.Symbols.VBKeyword.#ReDim", Justification = Justifications.SymbolTypeNames)]
[assembly: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Re", Scope = "member", Target = "System.Web.Razor.Tokenizer.Symbols.VBKeyword.#ReDim", Justification = Justifications.SymbolTypeNames)]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Str", Scope = "member", Target = "System.Web.Razor.Tokenizer.Symbols.VBKeyword.#CStr", Justification = Justifications.SymbolTypeNames)]

internal static partial class Justifications
{
    internal const string SymbolTypeNames = "Symbol Type Names are spelled according to the language keyword or token they represent";
}
