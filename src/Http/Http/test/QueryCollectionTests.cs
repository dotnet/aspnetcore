// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
}
