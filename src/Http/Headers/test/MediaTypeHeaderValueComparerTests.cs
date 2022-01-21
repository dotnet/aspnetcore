// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Net.Http.Headers;

public class MediaTypeHeaderValueComparerTests
{
    public static IEnumerable<object[]> SortValues
    {
        get
        {
            yield return new object[] {
                    new string[]
                        {
                            "application/*",
                            "text/plain",
                            "text/*+json;q=0.8",
                            "text/plain;q=1.0",
                            "text/plain",
                            "text/*+json;q=0.6",
                            "text/plain;q=0",
                            "*/*;q=0.8",
                            "*/*;q=1",
                            "text/*;q=1",
                            "text/plain;q=0.8",
                            "text/*;q=0.8",
                            "text/*;q=0.6",
                            "text/*+json;q=0.4",
                            "text/*;q=1.0",
                            "*/*;q=0.4",
                            "text/plain;q=0.6",
                            "text/xml",
                        },
                    new string[]
                        {
                            "text/plain",
                            "text/plain;q=1.0",
                            "text/plain",
                            "text/xml",
                            "application/*",
                            "text/*;q=1",
                            "text/*;q=1.0",
                            "*/*;q=1",
                            "text/plain;q=0.8",
                            "text/*+json;q=0.8",
                            "text/*;q=0.8",
                            "*/*;q=0.8",
                            "text/plain;q=0.6",
                            "text/*+json;q=0.6",
                            "text/*;q=0.6",
                            "text/*+json;q=0.4",
                            "*/*;q=0.4",
                            "text/plain;q=0",
                        }
                };
        }
    }

    [Theory]
    [MemberData(nameof(SortValues))]
    public void SortMediaTypeHeaderValuesByQFactor_SortsCorrectly(IEnumerable<string> unsorted, IEnumerable<string> expectedSorted)
    {
        var unsortedValues = MediaTypeHeaderValue.ParseList(unsorted.ToList());
        var expectedSortedValues = MediaTypeHeaderValue.ParseList(expectedSorted.ToList());

        var actualSorted = unsortedValues.OrderByDescending(m => m, MediaTypeHeaderValueComparer.QualityComparer).ToList();

        Assert.Equal(expectedSortedValues, actualSorted);
    }
}
