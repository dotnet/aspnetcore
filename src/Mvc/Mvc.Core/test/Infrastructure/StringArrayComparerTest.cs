// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

// StringArrayComparer backs the ActionSelectionTable lookups. It implements both the classic
// IEqualityComparer<string[]> and the IAlternateEqualityComparer<ReadOnlySpan<string>, string[]>
// used by the pooled-buffer fast path in ActionSelectionTable.Select. These tests assert the span
// overloads are semantically identical to the array overloads so the allocation-free lookup cannot
// change matching or hashing behavior.
public class StringArrayComparerTest
{
    public static IEnumerable<object[]> Comparers =>
        new[]
        {
            new object[] { false },
            new object[] { true },
        };

    private static StringArrayComparer GetComparer(bool ignoreCase)
        => ignoreCase ? StringArrayComparer.OrdinalIgnoreCase : StringArrayComparer.Ordinal;

    [Theory]
    [MemberData(nameof(Comparers))]
    public void GetHashCode_SpanMatchesArray_ForEqualContents(bool ignoreCase)
    {
        var comparer = GetComparer(ignoreCase);
        var array = new[] { "Home", "Index", "" };
        ReadOnlySpan<string> span = array;

        Assert.Equal(comparer.GetHashCode(array), comparer.GetHashCode(span));
    }

    [Theory]
    [MemberData(nameof(Comparers))]
    public void GetHashCode_NullAndEmpty_ProduceSameHash(bool ignoreCase)
    {
        var comparer = GetComparer(ignoreCase);
        ReadOnlySpan<string> withNull = new[] { "Home", null! };
        ReadOnlySpan<string> withEmpty = new[] { "Home", "" };

        Assert.Equal(comparer.GetHashCode(withEmpty), comparer.GetHashCode(withNull));
    }

    [Theory]
    [MemberData(nameof(Comparers))]
    public void Equals_SpanAndArray_TrueForEqualContents(bool ignoreCase)
    {
        var comparer = GetComparer(ignoreCase);
        ReadOnlySpan<string> span = new[] { "Home", "Index" };
        var array = new[] { "Home", "Index" };

        Assert.True(comparer.Equals(span, array));
    }

    [Theory]
    [MemberData(nameof(Comparers))]
    public void Equals_NullAndEmpty_AreEquivalent(bool ignoreCase)
    {
        var comparer = GetComparer(ignoreCase);
        ReadOnlySpan<string> span = new[] { "Home", null! };
        var array = new[] { "Home", "" };

        Assert.True(comparer.Equals(span, array));
    }

    [Theory]
    [MemberData(nameof(Comparers))]
    public void Equals_DifferentLength_ReturnsFalse(bool ignoreCase)
    {
        var comparer = GetComparer(ignoreCase);
        ReadOnlySpan<string> span = new[] { "Home", "Index" };
        var array = new[] { "Home" };

        Assert.False(comparer.Equals(span, array));
    }

    [Theory]
    [MemberData(nameof(Comparers))]
    public void Equals_OtherNull_ReturnsFalse(bool ignoreCase)
    {
        var comparer = GetComparer(ignoreCase);
        ReadOnlySpan<string> span = new[] { "Home", "Index" };

        Assert.False(comparer.Equals(span, null!));
    }

    [Fact]
    public void Equals_Ordinal_IsCaseSensitive()
    {
        var comparer = StringArrayComparer.Ordinal;
        ReadOnlySpan<string> span = new[] { "Home", "index" };
        var array = new[] { "Home", "Index" };

        Assert.False(comparer.Equals(span, array));
    }

    [Fact]
    public void Equals_OrdinalIgnoreCase_IsCaseInsensitive()
    {
        var comparer = StringArrayComparer.OrdinalIgnoreCase;
        ReadOnlySpan<string> span = new[] { "HOME", "iNDex" };
        var array = new[] { "Home", "Index" };

        Assert.True(comparer.Equals(span, array));
    }

    [Theory]
    [MemberData(nameof(Comparers))]
    public void GetHashCode_OverLengthBackingBuffer_UsesSpanLength(bool ignoreCase)
    {
        var comparer = GetComparer(ignoreCase);

        // Mimics ArrayPool.Rent returning a buffer longer than needed - the extra trailing slots
        // must not participate in hashing.
        var backing = new[] { "Home", "Index", "GARBAGE", "MORE_GARBAGE" };
        ReadOnlySpan<string> sliced = backing.AsSpan(0, 2);

        Assert.Equal(comparer.GetHashCode(new[] { "Home", "Index" }), comparer.GetHashCode(sliced));
    }

    [Theory]
    [MemberData(nameof(Comparers))]
    public void Equals_OverLengthBackingBuffer_UsesSpanLength(bool ignoreCase)
    {
        var comparer = GetComparer(ignoreCase);
        var backing = new[] { "Home", "Index", "GARBAGE", "MORE_GARBAGE" };
        ReadOnlySpan<string> sliced = backing.AsSpan(0, 2);

        Assert.True(comparer.Equals(sliced, new[] { "Home", "Index" }));
    }

    [Fact]
    public void Create_ReturnsIndependentArrayCopy()
    {
        var comparer = StringArrayComparer.Ordinal;
        var backing = new[] { "Home", "Index", "GARBAGE" };
        ReadOnlySpan<string> sliced = backing.AsSpan(0, 2);

        var created = ((IAlternateEqualityComparer<ReadOnlySpan<string>, string[]>)comparer).Create(sliced);

        Assert.Equal(new[] { "Home", "Index" }, created);
        backing[0] = "Mutated";
        Assert.Equal("Home", created[0]);
    }

    [Theory]
    [MemberData(nameof(Comparers))]
    public void AlternateLookup_ProbesStoredKey_ViaOverLengthBuffer(bool ignoreCase)
    {
        var comparer = GetComparer(ignoreCase);
        var dictionary = new Dictionary<string[], int>(comparer)
        {
            [new[] { "Home", "Index" }] = 1,
        };
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<string>>();

        var backing = new[] { "Home", "Index", "GARBAGE", "MORE_GARBAGE" };
        Assert.True(lookup.TryGetValue(backing.AsSpan(0, 2), out var value));
        Assert.Equal(1, value);
    }

    [Fact]
    public void AlternateLookup_Ordinal_DoesNotMatchDifferentCasing()
    {
        var dictionary = new Dictionary<string[], int>(StringArrayComparer.Ordinal)
        {
            [new[] { "Home", "Index" }] = 1,
        };
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<string>>();

        ReadOnlySpan<string> differentCase = new[] { "home", "index" };
        Assert.False(lookup.TryGetValue(differentCase, out _));
    }

    [Fact]
    public void AlternateLookup_OrdinalIgnoreCase_MatchesDifferentCasing()
    {
        var dictionary = new Dictionary<string[], int>(StringArrayComparer.OrdinalIgnoreCase)
        {
            [new[] { "Home", "Index" }] = 1,
        };
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<string>>();

        ReadOnlySpan<string> differentCase = new[] { "home", "index" };
        Assert.True(lookup.TryGetValue(differentCase, out var value));
        Assert.Equal(1, value);
    }
}
