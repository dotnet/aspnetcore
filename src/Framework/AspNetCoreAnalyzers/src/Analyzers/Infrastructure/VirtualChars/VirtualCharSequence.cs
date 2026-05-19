// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;

/// <summary>
/// Represents the individual characters that raw string token represents (i.e. with escapes collapsed).  
/// The difference between this and the result from token.ValueText is that for each collapsed character
/// returned the original span of text in the original token can be found.  i.e. if you had the
/// following in C#:
/// <para/>
/// <c>"G\u006fo"</c>
/// <para/>
/// Then you'd get back:
/// <para/>
/// <c>'G' -> [0, 1) 'o' -> [1, 7) 'o' -> [7, 1)</c>
/// <para/>
/// This allows for embedded language processing that can refer back to the user's original code
/// instead of the escaped value we're processing.
/// </summary>
internal partial struct VirtualCharSequence
{
    public static readonly VirtualCharSequence Empty = Create(ImmutableList<VirtualChar>.Empty);

    public static VirtualCharSequence Create(ImmutableList<VirtualChar> virtualChars)
        => new(new ImmutableSegmentedListChunk(virtualChars));

    public static VirtualCharSequence Create(int firstVirtualCharPosition, string underlyingData)
        => new(new StringChunk(firstVirtualCharPosition, underlyingData));

    /// <summary>
    /// The actual characters that this <see cref="VirtualCharSequence"/> is a portion of.
    /// </summary>
    private readonly Chunk _leafCharacters;

    /// <summary>
    /// The portion of <see cref="_leafCharacters"/> that is being exposed.  This span 
    /// is `[inclusive, exclusive)`.
    /// </summary>
    private readonly TextSpan _span;

    private VirtualCharSequence(Chunk sequence)
        : this(sequence, new TextSpan(0, sequence.Length))
    {
    }

    private VirtualCharSequence(Chunk sequence, TextSpan span)
    {
        if (span.Start > sequence.Length)
        {
            throw new ArgumentException("Span start out of range.");
        }

        if (span.End > sequence.Length)
        {
            throw new ArgumentException("Span end out of range.");
        }

        _leafCharacters = sequence;
        _span = span;
    }

    /// <summary>
    /// Gets the number of elements contained in the <see cref="VirtualCharSequence"/>.
    /// </summary>
    public int Length => _span.Length;

    /// <summary>
    /// Gets the <see cref="VirtualChar"/> at the specified index.
    /// </summary>
    public VirtualChar this[int index] => _leafCharacters[_span.Start + index];

    /// <summary>
    /// Gets a value indicating whether the <see cref="VirtualCharSequence"/> was declared but not initialized.
    /// </summary>
    public bool IsDefault => _leafCharacters == null;
    public bool IsEmpty => Length == 0;
    public bool IsDefaultOrEmpty => IsDefault || IsEmpty;

    /// <summary>
    /// Retreives a sub-sequence from this <see cref="VirtualCharSequence"/>.
    /// </summary>
    public VirtualCharSequence GetSubSequence(TextSpan span)
       => new(_leafCharacters, new TextSpan(_span.Start + span.Start, span.Length));

    public Enumerator GetEnumerator()
        => new(this);

    public VirtualChar First() => this[0];
    public VirtualChar Last() => this[Length - 1];

    /// <summary>
    /// Finds the virtual char in this sequence that contains the position.  Will return null if this position is not
    /// in the span of this sequence.
    /// </summary>
    public VirtualChar? Find(int position)
        => _leafCharacters?.Find(position);

    public bool Contains(VirtualChar @char)
        => IndexOf(@char) >= 0;

    public int IndexOf(VirtualChar @char)
    {
        var index = 0;
        foreach (var ch in this)
        {
            if (ch == @char)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    public VirtualChar? FirstOrNull(Func<VirtualChar, bool> predicate)
    {
        foreach (var ch in this)
        {
            if (predicate(ch))
            {
                return ch;
            }
        }

        return null;
    }

    public VirtualChar? LastOrNull(Func<VirtualChar, bool> predicate)
    {
        for (var i = this.Length - 1; i >= 0; i--)
        {
            var ch = this[i];
            if (predicate(ch))
            {
                return ch;
            }
        }

        return null;
    }

    public bool Any(Func<VirtualChar, bool> predicate)
    {
        foreach (var ch in this)
        {
            if (predicate(ch))
            {
                return true;
            }
        }

        return false;
    }

    public bool All(Func<VirtualChar, bool> predicate)
    {
        foreach (var ch in this)
        {
            if (!predicate(ch))
            {
                return false;
            }
        }

        return true;
    }

    public VirtualCharSequence Skip(int count)
        => this.GetSubSequence(TextSpan.FromBounds(count, this.Length));

    public VirtualCharSequence SkipWhile(Func<VirtualChar, bool> predicate)
    {
        var start = 0;
        foreach (var ch in this)
        {
            if (!predicate(ch))
            {
                break;
            }

            start++;
        }

        return this.GetSubSequence(TextSpan.FromBounds(start, this.Length));
    }

    /// <summary>
    /// Create a <see cref="string"/> from the <see cref="VirtualCharSequence"/>.
    /// </summary>
    public string CreateString()
    {
        var sb = new StringBuilder();
        foreach (var ch in this)
        {
            ch.AppendTo(sb);
        }

        return sb.ToString();
    }

    [Conditional("DEBUG")]
    public void AssertAdjacentTo(VirtualCharSequence virtualChars)
    {
        Debug.Assert(_leafCharacters == virtualChars._leafCharacters);
        Debug.Assert(_span.End == virtualChars._span.Start);
    }

    /// <summary>
    /// Combines two <see cref="VirtualCharSequence"/>s, producing a final
    /// sequence that points at the same underlying data, but spans from the 
    /// start of <paramref name="chars1"/> to the end of <paramref name="chars2"/>.
    /// </summary>  
    public static VirtualCharSequence FromBounds(
        VirtualCharSequence chars1, VirtualCharSequence chars2)
    {
        Debug.Assert(chars1._leafCharacters == chars2._leafCharacters);
        return new VirtualCharSequence(
            chars1._leafCharacters,
            TextSpan.FromBounds(chars1._span.Start, chars2._span.End));
    }
}
