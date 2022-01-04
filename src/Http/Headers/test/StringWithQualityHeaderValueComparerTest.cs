// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Net.Http.Headers;

public class StringWithQualityHeaderValueComparerTest
{
    public static TheoryData<string[], string[]> StringWithQualityHeaderValueComparerTestsBeforeAfterSortedValues
    {
        get
        {
            return new TheoryData<string[], string[]>
                {
                    {
                        new string[]
                        {
                            "text",
                            "text;q=1.0",
                            "text",
                            "text;q=0",
                            "*;q=0.8",
                            "*;q=1",
                            "text;q=0.8",
                            "*;q=0.6",
                            "text;q=1.0",
                            "*;q=0.4",
                            "text;q=0.6",
                        },
                        new string[]
                        {
                            "text",
                            "text;q=1.0",
                            "text",
                            "text;q=1.0",
                            "*;q=1",
                            "text;q=0.8",
                            "*;q=0.8",
                            "text;q=0.6",
                            "*;q=0.6",
                            "*;q=0.4",
                            "text;q=0",
                        }
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(StringWithQualityHeaderValueComparerTestsBeforeAfterSortedValues))]
    public void SortStringWithQualityHeaderValuesByQFactor_SortsCorrectly(IEnumerable<string> unsorted, IEnumerable<string> expectedSorted)
    {
        var unsortedValues = StringWithQualityHeaderValue.ParseList(unsorted.ToList());
        var expectedSortedValues = StringWithQualityHeaderValue.ParseList(expectedSorted.ToList());

        var actualSorted = unsortedValues.OrderByDescending(k => k, StringWithQualityHeaderValueComparer.QualityComparer).ToList();

        Assert.True(expectedSortedValues.SequenceEqual(actualSorted));
    }
}
