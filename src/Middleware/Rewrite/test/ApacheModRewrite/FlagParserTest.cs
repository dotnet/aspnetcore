// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Rewrite.ApacheModRewrite;

namespace Microsoft.AspNetCore.Rewrite.Tests.ModRewrite;

public class FlagParserTest
{
    [Fact]
    public void FlagParser_CheckSingleTerm()
    {
        var results = FlagParser.Parse("[NC]");
        var dict = new Dictionary<FlagType, string>();
        dict.Add(FlagType.NoCase, string.Empty);
        var expected = new Flags(dict);

        Assert.True(DictionaryContentsEqual(expected.FlagDictionary, results.FlagDictionary));
    }

    [Fact]
    public void FlagParser_CheckManyTerms()
    {
        var results = FlagParser.Parse("[NC,F,L]");
        var dict = new Dictionary<FlagType, string>();
        dict.Add(FlagType.NoCase, string.Empty);
        dict.Add(FlagType.Forbidden, string.Empty);
        dict.Add(FlagType.Last, string.Empty);
        var expected = new Flags(dict);

        Assert.True(DictionaryContentsEqual(expected.FlagDictionary, results.FlagDictionary));
    }

    [Fact]
    public void FlagParser_CheckManyTermsWithEquals()
    {
        var results = FlagParser.Parse("[NC,F,R=301]");
        var dict = new Dictionary<FlagType, string>();
        dict.Add(FlagType.NoCase, string.Empty);
        dict.Add(FlagType.Forbidden, string.Empty);
        dict.Add(FlagType.Redirect, "301");
        var expected = new Flags(dict);

        Assert.True(DictionaryContentsEqual(expected.FlagDictionary, results.FlagDictionary));
    }

    [Theory]
    [InlineData("]", "Flags should start and end with square brackets: [flags]")]
    [InlineData("[", "Flags should start and end with square brackets: [flags]")]
    [InlineData("[R, L]", "Unrecognized flag: ' L'")] // cannot have spaces after ,
    [InlineData("[RL]", "Unrecognized flag: 'RL'")]
    public void FlagParser_AssertFormatErrorWhenFlagsArePoorlyConstructed(string input, string expected)
    {
        var ex = Assert.Throws<FormatException>(() => FlagParser.Parse(input));
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public void FlagParser_AssertArgumentExceptionWhenFlagsAreNullOrEmpty()
    {
        Assert.Throws<ArgumentNullException>(() => FlagParser.Parse(null));
        Assert.Throws<ArgumentException>(() => FlagParser.Parse(string.Empty));
    }

    [Theory]
    [InlineData("[CO=VAR:VAL]", "VAR:VAL")]
    [InlineData("[CO=!VAR]", "!VAR")]
    [InlineData("[CO=;NAME:VALUE;ABC:123]", ";NAME:VALUE;ABC:123")]
    public void Flag_ParserHandlesComplexFlags(string flagString, string expected)
    {
        var results = FlagParser.Parse(flagString);
        string value;
        Assert.True(results.GetValue(FlagType.Cookie, out value));
        Assert.Equal(expected, value);
    }

    public bool DictionaryContentsEqual<TKey, TValue>(IDictionary<TKey, TValue> dictionary, IDictionary<TKey, TValue> other)
    {
        return (other ?? new Dictionary<TKey, TValue>())
            .OrderBy(kvp => kvp.Key)
            .SequenceEqual((dictionary ?? new Dictionary<TKey, TValue>())
            .OrderBy(kvp => kvp.Key));
    }
}
