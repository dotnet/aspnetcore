// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Http.Features;

public class FeatureCollectionTests
{
    [Fact]
    public void AddedInterfaceIsReturned()
    {
        var interfaces = new FeatureCollection();
        var thing = new Thing();

        interfaces[typeof(IThing)] = thing;

        var thing2 = interfaces[typeof(IThing)];
        Assert.Equal(thing2, thing);
    }

    [Fact]
    public void IndexerAlsoAddsItems()
    {
        var interfaces = new FeatureCollection();
        var thing = new Thing();

        interfaces[typeof(IThing)] = thing;

        Assert.Equal(interfaces[typeof(IThing)], thing);
    }

    [Fact]
    public void SetNullValueRemoves()
    {
        var interfaces = new FeatureCollection();
        var thing = new Thing();

        interfaces[typeof(IThing)] = thing;
        Assert.Equal(interfaces[typeof(IThing)], thing);

        interfaces[typeof(IThing)] = null;

        var thing2 = interfaces[typeof(IThing)];
        Assert.Null(thing2);
    }

    [Fact]
    public void GetMissingStructFeatureThrows()
    {
        var interfaces = new FeatureCollection();

        // Regression test: Used to throw NullReferenceException because it tried to unbox a null object to a struct
        var ex = Assert.Throws<InvalidOperationException>(() => interfaces.Get<int>());
        Assert.Equal("System.Int32 does not exist in the feature collection and because it is a struct the method can't return null. Use 'featureCollection[typeof(System.Int32)] is not null' to check if the feature exists.", ex.Message);
    }

    [Fact]
    public void GetMissingFeatureReturnsNull()
    {
        var interfaces = new FeatureCollection();

        Assert.Null(interfaces.Get<Thing>());
    }

    [Fact]
    public void GetStructFeature()
    {
        var interfaces = new FeatureCollection();
        var value = 20;
        interfaces.Set(value);

        Assert.Equal(value, interfaces.Get<int>());
    }

    [Fact]
    public void GetNullableStructFeatureWhenSetWithNonNullableStruct()
    {
        var interfaces = new FeatureCollection();
        var value = 20;
        interfaces.Set(value);

        Assert.Null(interfaces.Get<int?>());
    }

    [Fact]
    public void GetNullableStructFeatureWhenSetWithNullableStruct()
    {
        var interfaces = new FeatureCollection();
        var value = 20;
        interfaces.Set<int?>(value);

        Assert.Equal(value, interfaces.Get<int?>());
    }

    [Fact]
    public void GetFeature()
    {
        var interfaces = new FeatureCollection();
        var thing = new Thing();
        interfaces.Set(thing);

        Assert.Equal(thing, interfaces.Get<Thing>());
    }
}
