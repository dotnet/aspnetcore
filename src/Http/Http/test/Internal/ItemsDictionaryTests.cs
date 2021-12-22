// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Http;

public class ItemsDictionaryTests
{
    [Fact]
    public void GetEnumerator_ShouldResolveWithoutNullReferenceException()
    {
        // Arrange
        var dict = new ItemsDictionary();

        // Act and Assert
        IEnumerable en = (IEnumerable)dict;
        Assert.NotNull(en.GetEnumerator());
    }

    [Fact]
    public void CopyTo_ShouldCopyItemsWithoutNullReferenceException()
    {
        // Arrange
        var dict = new ItemsDictionary();
        var pairs = new KeyValuePair<object, object>[] { new KeyValuePair<object, object>("first", "value") };

        // Act and Assert
        ICollection<KeyValuePair<object, object>> cl = (ICollection<KeyValuePair<object, object>>)dict;
        cl.CopyTo(pairs, 0);
    }
}
