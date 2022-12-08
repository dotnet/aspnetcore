// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;

/// <summary>
/// Helper service that takes the raw text of a string token and produces the individual
/// characters that raw string token represents (i.e. with escapes collapsed).  The difference
/// between this and the result from token.ValueText is that for each collapsed character
/// returned the original span of text in the original token can be found.  i.e. if you had the
/// following in C#:
///
/// "G\u006fo"
///
/// Then you'd get back:
///
/// 'G' -> [0, 1) 'o' -> [1, 7) 'o' -> [7, 1)
///
/// This allows for embedded language processing that can refer back to the users' original code
/// instead of the escaped value we're processing.
/// </summary>
internal interface IVirtualCharService
{
    /// <summary>
    /// <para>
    /// Takes in a string token and return the <see cref="VirtualChar"/>s corresponding to each
    /// char of the tokens <see cref="SyntaxToken.ValueText"/>.  In other words, for each char
    /// in ValueText there will be a VirtualChar in the resultant array.  Each VirtualChar will
    /// specify what char the language considers them to represent, as well as the span of text
    /// in the original <see cref="SourceText"/> that the language created that char from. 
    /// </para>
    /// <para>
    /// For most chars this will be a single character span.  i.e. 'c' -> 'c'.  However, for
    /// escapes this may be a multi character span.  i.e. 'c' -> '\u0063'
    /// </para>
    /// <para>
    /// If the token is not a string literal token, or the string literal has any diagnostics on
    /// it, then <see langword="default"/> will be returned.   Additionally, because a
    /// VirtualChar can only represent a single char, while some escape sequences represent
    /// multiple chars, <see langword="default"/> will also be returned in those cases. All
    /// these cases could be relaxed in the future.  But they greatly simplify the
    /// implementation.
    /// </para>
    /// <para>
    /// If this function succeeds, certain invariants will hold.  First, each character in the
    /// sequence of characters in <paramref name="token"/>.ValueText will become a single
    /// VirtualChar in the result array with a matching <see cref="VirtualChar.Rune"/> property.
    /// Similarly, each VirtualChar's <see cref="VirtualChar.Span"/> will abut each other, and
    /// the union of all of them will cover the span of the token's <see
    /// cref="SyntaxToken.Text"/>
    /// *not* including the start and quotes.
    /// </para>
    /// <para>
    /// In essence the VirtualChar array acts as the information explaining how the <see
    /// cref="SyntaxToken.Text"/> of the token between the quotes maps to each character in the
    /// token's <see cref="SyntaxToken.ValueText"/>.
    /// </para>
    /// </summary>
    VirtualCharSequence TryConvertToVirtualChars(SyntaxToken token);

    /// <summary>
    /// Produces the appropriate escape version of <paramref name="ch"/> to be placed in a
    /// normal string literal.  For example if <paramref name="ch"/> is the <c>tab</c>
    /// character, then this would produce <c>t</c> as <c>\t</c> is what would go into a string
    /// literal.
    /// </summary>
    bool TryGetEscapeCharacter(VirtualChar ch, out char escapeChar);
}
