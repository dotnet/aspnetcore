// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Tests;

public class QueryCollectionTests
{
    [Fact]
    public void ReturnStringValuesEmptyForMissingQueryKeys()
    {
        IQueryCollection query = new QueryCollection(new Dictionary<string, StringValues>());

        // StringValues.Empty.Equals(default(StringValues)), so we check if the implicit conversion
        // to string[] returns null or Array.Empty<string>() to tell the difference.
        Assert.Same(Array.Empty<string>(), (string[])query["query1"]);

        // Test the null-dictionary code path too.
        Assert.Same(Array.Empty<string>(), (string[])QueryCollection.Empty["query1"]);
    }

    [Fact]
    public void EnumeratorResetsCorrectly()
    {
        var query = new QueryCollection(
            new Dictionary<string, StringValues>
            {
                { "Query1", "Value1" },
                { "Query2", "Value2" },
                { "Query3", "Value3" }
            });

        var enumerator = query.GetEnumerator();
        var initial = enumerator.Current;

        Assert.True(enumerator.MoveNext());

        var first = enumerator.Current;
        var last = enumerator.Current;

        while (enumerator.MoveNext())
        {
            last = enumerator.Current;
        }

        Assert.NotEqual(first, initial);
        Assert.NotEqual(first, last);

        ((IEnumerator)enumerator).Reset();

        Assert.Equal(enumerator.Current, initial);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(enumerator.Current, first);
    }
}
