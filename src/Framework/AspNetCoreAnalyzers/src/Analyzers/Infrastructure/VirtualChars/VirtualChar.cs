// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;

/// <summary>
/// <see cref="VirtualChar"/> provides a uniform view of a language's string token characters regardless if they
/// were written raw in source, or are the production of a language escape sequence.  For example, in C#, in a
/// normal <c>""</c> string a <c>Tab</c> character can be written either as the raw tab character (value <c>9</c> in
/// ASCII),  or as <c>\t</c>.  The format is a single character in the source, while the latter is two characters
/// (<c>\</c> and <c>t</c>).  <see cref="VirtualChar"/> will represent both, providing the raw <see cref="char"/>
/// value of <c>9</c> as well as what <see cref="TextSpan"/> in the original <see cref="SourceText"/> they occupied.
/// </summary>
/// <remarks>
/// A core consumer of this system is the Regex parser.  That parser wants to work over an array of characters,
/// however this array of characters is not the same as the array of characters a user types into a string in C# or
/// VB. For example In C# someone may write: @"\z".  This should appear to the user the same as if they wrote "\\z"
/// and the same as "\\\u007a".  However, as these all have wildly different presentations for the user, there needs
/// to be a way to map back the characters it sees ( '\' and 'z' ) back to the  ranges of characters the user wrote.
/// </remarks>
internal readonly struct VirtualChar : IEquatable<VirtualChar>, IComparable<VirtualChar>, IComparable<char>
{
    /// <summary>
    /// The value of this <see cref="VirtualChar"/> as a <see cref="Rune"/> if such a representation is possible.
    /// <see cref="Rune"/>s can represent Unicode codepoints that can appear in a <see cref="string"/> except for
    /// unpaired surrogates.  If an unpaired high or low surrogate character is present, this value will be <see
    /// cref="Rune.ReplacementChar"/>.  The value of this character can be retrieved from
    /// <see cref="SurrogateChar"/>.
    /// </summary>
    public readonly Rune Rune;

    /// <summary>
    /// The unpaired high or low surrogate character that was encountered that could not be represented in <see
    /// cref="Rune"/>.  If <see cref="Rune"/> is not <see cref="Rune.ReplacementChar"/>, this will be <c>0</c>.
    /// </summary>
    public readonly char SurrogateChar;

    /// <summary>
    /// The span of characters in the original <see cref="SourceText"/> that represent this <see
    /// cref="VirtualChar"/>.
    /// </summary>
    public readonly TextSpan Span;

    /// <summary>
    /// Creates a new <see cref="VirtualChar"/> from the provided <paramref name="rune"/>.  This operation cannot
    /// fail.
    /// </summary>
    public static VirtualChar Create(Rune rune, TextSpan span)
        => new(rune, surrogateChar: default, span);

    /// <summary>
    /// Creates a new <see cref="VirtualChar"/> from an unpaired high or low surrogate character.  This will throw
    /// if <paramref name="surrogateChar"/> is not actually a surrogate character. The resultant <see cref="Rune"/>
    /// value will be <see cref="Rune.ReplacementChar"/>.
    /// </summary>
    public static VirtualChar Create(char surrogateChar, TextSpan span)
    {
        if (!char.IsSurrogate(surrogateChar))
        {
            throw new ArgumentException("Must be a surrogate char.", nameof(surrogateChar));
        }

        return new VirtualChar(rune: Rune.ReplacementChar, surrogateChar, span);
    }

    private VirtualChar(Rune rune, char surrogateChar, TextSpan span)
    {
        if (!(surrogateChar == 0 || rune == Rune.ReplacementChar))
        {
            throw new InvalidOperationException("If surrogateChar is provided then rune must be Rune.ReplacementChar");
        }

        if (span.IsEmpty)
        {
            throw new ArgumentException("Span should not be empty.", nameof(span));
        }

        Rune = rune;
        SurrogateChar = surrogateChar;
        Span = span;
    }

    /// <summary>
    /// Retrieves the scaler value of this character as an <see cref="int"/>.  If this is an unpaired surrogate
    /// character, this will be the value of that surrogate.  Otherwise, this will be the value of our <see
    /// cref="Rune"/>.
    /// </summary>
    public int Value => SurrogateChar != 0 ? SurrogateChar : Rune.Value;

    public bool IsDigit
        => SurrogateChar != 0 ? char.IsDigit(SurrogateChar) : Rune.IsDigit(Rune);

    public bool IsLetterOrDigit
        => SurrogateChar != 0 ? char.IsLetterOrDigit(SurrogateChar) : Rune.IsLetterOrDigit(Rune);

    public bool IsWhiteSpace
        => SurrogateChar != 0 ? char.IsWhiteSpace(SurrogateChar) : Rune.IsWhiteSpace(Rune);

    #region equality

    public static bool operator ==(VirtualChar char1, VirtualChar char2)
        => char1.Equals(char2);

    public static bool operator !=(VirtualChar char1, VirtualChar char2)
        => !(char1 == char2);

    public static bool operator ==(VirtualChar ch1, char ch2)
        => ch1.Value == ch2;

    public static bool operator !=(VirtualChar ch1, char ch2)
        => !(ch1 == ch2);

    public override bool Equals(object? obj)
        => obj is VirtualChar vc && Equals(vc);

    public bool Equals(VirtualChar other)
        => Rune == other.Rune &&
           SurrogateChar == other.SurrogateChar &&
           Span == other.Span;

    public override int GetHashCode()
    {
        var hashCode = 1985253839;
        hashCode = hashCode * -1521134295 + Rune.GetHashCode();
        hashCode = hashCode * -1521134295 + SurrogateChar.GetHashCode();
        hashCode = hashCode * -1521134295 + Span.GetHashCode();
        return hashCode;
    }

    #endregion

    #region string operations

    /// <inheritdoc/>
    public override string ToString()
        => SurrogateChar != 0 ? SurrogateChar.ToString() : Rune.ToString();

    public void AppendTo(StringBuilder builder)
    {
        if (SurrogateChar != 0)
        {
            builder.Append(SurrogateChar);
            return;
        }

        Span<char> chars = stackalloc char[2];

        var length = Rune.EncodeToUtf16(chars);
        builder.Append(chars[0]);
        if (length == 2)
        {
            builder.Append(chars[1]);
        }
    }

    #endregion

    #region comparable

    public int CompareTo(VirtualChar other)
        => Value - other.Value;

    public static bool operator <(VirtualChar ch1, VirtualChar ch2)
        => ch1.Value < ch2.Value;

    public static bool operator <=(VirtualChar ch1, VirtualChar ch2)
        => ch1.Value <= ch2.Value;

    public static bool operator >(VirtualChar ch1, VirtualChar ch2)
        => ch1.Value > ch2.Value;

    public static bool operator >=(VirtualChar ch1, VirtualChar ch2)
        => ch1.Value >= ch2.Value;

    public int CompareTo(char other)
        => Value - other;

    public static bool operator <(VirtualChar ch1, char ch2)
        => ch1.Value < ch2;

    public static bool operator <=(VirtualChar ch1, char ch2)
        => ch1.Value <= ch2;

    public static bool operator >(VirtualChar ch1, char ch2)
        => ch1.Value > ch2;

    public static bool operator >=(VirtualChar ch1, char ch2)
        => ch1.Value >= ch2;

    #endregion
}
