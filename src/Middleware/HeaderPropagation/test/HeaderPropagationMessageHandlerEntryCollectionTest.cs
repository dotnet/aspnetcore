// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HeaderPropagation.Tests;

public class HeaderPropagationMessageHandlerEntryCollectionTest
{
    [Fact]
    public void Add_SingleValue_UseValueForBothProperties()
    {
        var collection = new HeaderPropagationMessageHandlerEntryCollection();
        collection.Add("foo");

        Assert.Single(collection);
        var entry = collection[0];
        Assert.Equal("foo", entry.CapturedHeaderName);
        Assert.Equal("foo", entry.OutboundHeaderName);
    }

    [Fact]
    public void Add_BothValues_UseCorrectValues()
    {
        var collection = new HeaderPropagationMessageHandlerEntryCollection();
        collection.Add("foo", "bar");

        Assert.Single(collection);
        var entry = collection[0];
        Assert.Equal("foo", entry.CapturedHeaderName);
        Assert.Equal("bar", entry.OutboundHeaderName);
    }
}
