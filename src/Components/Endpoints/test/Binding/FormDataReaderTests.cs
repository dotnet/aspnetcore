// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

public class FormDataReaderTests
{
    [Fact]
    public void FormDataReader_ReturnsValue_WhenAvailable()
    {
        // Arrange
        var buffer = new char[256];
        var data = new Dictionary<FormKey, StringValues>()
        {
            [new FormKey("value".AsMemory())] = "success",
        };

        var reader = new FormDataReader(data, CultureInfo.InvariantCulture, buffer);
        reader.PushPrefix("value");
        Assert.True(reader.TryGetValue(out var value));
        Assert.Equal("success", value);
    }

    [Fact]
    public void FormDataReader_DoesNotReturnValue_WhenNotAvailable()
    {
        // Arrange
        var buffer = new char[256];
        var data = new Dictionary<FormKey, StringValues>()
        {
            [new FormKey("other".AsMemory())] = "success",
        };

        var reader = new FormDataReader(data, CultureInfo.InvariantCulture, buffer);
        reader.PushPrefix("value");
        Assert.False(reader.TryGetValue(out var value));
        Assert.Equal(StringValues.Empty, value);
    }

    [Fact]
    public void FormDataReader_PushPrefix_AppendsDotsForNestedProperties()
    {
        // Arrange
        var buffer = new char[256];
        var data = new Dictionary<FormKey, StringValues>()
        {
            [new FormKey("Address.Street".AsMemory())] = "One Microsoft Way",
        };

        var reader = new FormDataReader(data, CultureInfo.InvariantCulture, buffer);
        reader.PushPrefix("Address");
        reader.PushPrefix("Street");
        Assert.True(reader.TryGetValue(out var value));
        Assert.Equal("One Microsoft Way", value);
    }

    [Fact]
    public void FormDataReader_PushPrefix_DoesNotAppendDotForIndexers()
    {
        // Arrange
        var buffer = new char[256];
        var data = new Dictionary<FormKey, StringValues>()
        {
            [new FormKey("Address[Street]".AsMemory())] = "One Microsoft Way",
        };

        var reader = new FormDataReader(data, CultureInfo.InvariantCulture, buffer);
        reader.PushPrefix("Address");
        reader.PushPrefix("[Street]");
        Assert.True(reader.TryGetValue(out var value));
        Assert.Equal("One Microsoft Way", value);
    }

    [Fact]
    public void FormDataReader_PushPrefix_AppendsDotsForProperties_AfterIndexers()
    {
        // Arrange
        var buffer = new char[256];
        var data = new Dictionary<FormKey, StringValues>()
        {
            [new FormKey("Customers[Id].Address".AsMemory())] = "One Microsoft Way",
        };

        var reader = new FormDataReader(data, CultureInfo.InvariantCulture, buffer);
        reader.PushPrefix("Customers");
        reader.PushPrefix("[Id]");
        reader.PushPrefix("Address");
        Assert.True(reader.TryGetValue(out var value));
        Assert.Equal("One Microsoft Way", value);
    }

    [Fact]
    public void FormDataReader_PopPrefix_RemovesDotsForProperties_AfterIndexers()
    {
        // Arrange
        var buffer = new char[256];
        var data = new Dictionary<FormKey, StringValues>()
        {
            [new FormKey("Customers[Id].Address".AsMemory())] = "One Microsoft Way",
        };

        var reader = new FormDataReader(data, CultureInfo.InvariantCulture, buffer);
        reader.PushPrefix("Customers");
        reader.PushPrefix("[Id]");
        reader.PushPrefix("Address");
        reader.PopPrefix("Address");

        Assert.Equal("Customers[Id]", reader.CurrentPrefix.ToString());
    }

    [Fact]
    public void FormDataReader_PopPrefix_RemovesIndexer()
    {
        // Arrange
        var buffer = new char[256];
        var data = new Dictionary<FormKey, StringValues>()
        {
            [new FormKey("Address[Street]".AsMemory())] = "One Microsoft Way",
        };

        var reader = new FormDataReader(data, CultureInfo.InvariantCulture, buffer);
        reader.PushPrefix("Address");
        reader.PushPrefix("[Street]");
        Assert.True(reader.TryGetValue(out var value));
        reader.PopPrefix("[Street]");
        Assert.Equal("Address", reader.CurrentPrefix.ToString());
    }

    [Fact]
    public void FormDataReader_PopPrefix_RemovesDotsForNestedProperties()
    {
        // Arrange
        var buffer = new char[256];
        var data = new Dictionary<FormKey, StringValues>()
        {
            [new FormKey("Address.Street".AsMemory())] = "One Microsoft Way",
        };

        var reader = new FormDataReader(data, CultureInfo.InvariantCulture, buffer);
        reader.PushPrefix("Address");
        reader.PushPrefix("Street");
        Assert.True(reader.TryGetValue(out var value));
        Assert.Equal("One Microsoft Way", value);
        reader.PopPrefix("Street");
        Assert.Equal("Address", reader.CurrentPrefix.ToString());
    }

    [Fact]
    public void FormDataReader_PopPrefix_ResetsToEmptyOnTopLevelProperties()
    {
        // Arrange
        var buffer = new char[256];
        var data = new Dictionary<FormKey, StringValues>()
        {
            [new FormKey("value".AsMemory())] = "success",
        };

        var reader = new FormDataReader(data, CultureInfo.InvariantCulture, buffer);
        reader.PushPrefix("value");
        Assert.True(reader.TryGetValue(out var value));
        Assert.Equal("success", value);
        reader.PopPrefix("value");
        Assert.Equal("", reader.CurrentPrefix.ToString());
    }

    [Fact]
    public void FormDataReader_PopPrefix_ResetsToEmptyOnTopLevelIndexers()
    {
        // Arrange
        var buffer = new char[256];
        var data = new Dictionary<FormKey, StringValues>()
        {
            [new FormKey("[value]".AsMemory())] = "success",
        };

        var reader = new FormDataReader(data, CultureInfo.InvariantCulture, buffer);
        reader.PushPrefix("[value]");
        Assert.True(reader.TryGetValue(out var value));
        Assert.Equal("success", value);
        reader.PopPrefix("[value]");
        Assert.Equal("", reader.CurrentPrefix.ToString());
    }

    [Fact]
    public void FormDataReader_ProcessKeys_ConstructsKeysDictionary()
    {
        // Arrange
        var buffer = new char[256];
        var data = new Dictionary<FormKey, StringValues>()
        {
            [new FormKey("Name".AsMemory())] = "Microsoft",
            [new FormKey("WareHousesByLocation[Redmond].Name".AsMemory())] = "Redmond",
            [new FormKey("WareHousesByLocation[Redmond].Address.City".AsMemory())] = "Redmond",
            [new FormKey("WareHousesByLocation[Redmond].Address.Country".AsMemory())] = "United States",
            [new FormKey("WareHousesByLocation[Redmond].Address.Street".AsMemory())] = "1 Microsoft Way",
            [new FormKey("WareHousesByLocation[Redmond].Address.ZipCode".AsMemory())] = "98052",
            [new FormKey("WareHousesByLocation[Seattle].Name".AsMemory())] = "Seattle",
            [new FormKey("WareHousesByLocation[Seattle].Address.City".AsMemory())] = "Seattle",
            [new FormKey("WareHousesByLocation[Seattle].Address.Country".AsMemory())] = "United States",
            [new FormKey("WareHousesByLocation[Seattle].Address.Street".AsMemory())] = "1 Microsoft Way",
            [new FormKey("WareHousesByLocation[Seattle].Address.ZipCode".AsMemory())] = "98052",
            [new FormKey("WareHousesByLocation[New York].Name".AsMemory())] = "New York",
            [new FormKey("WareHousesByLocation[New York].Address.City".AsMemory())] = "New York",
            [new FormKey("WareHousesByLocation[New York].Address.Country".AsMemory())] = "United States",
            [new FormKey("WareHousesByLocation[New York].Address.Street".AsMemory())] = "1 Microsoft Way",
            [new FormKey("WareHousesByLocation[New York].Address.ZipCode".AsMemory())] = "98052",
        };

        var reader = new FormDataReader(data, CultureInfo.InvariantCulture, buffer);
        var keys = reader.ProcessFormKeys();
        var prefix = Assert.Single(keys);
        Assert.Equal("WareHousesByLocation", prefix.Key.Value.ToString());
        Assert.Collection(prefix.Value,
            e => Assert.Equal("[Redmond]", e.Value.ToString()),
            e => Assert.Equal("[Seattle]", e.Value.ToString()),
            e => Assert.Equal("[New York]", e.Value.ToString()));
    }

    [Fact]
    public void FormDataReader_ProcessKeys_ConstructsKeys_NestedDictionaries()
    {
        // Arrange
        var buffer = new char[256];
        IReadOnlyDictionary<FormKey, HashSet<FormKey>> expectedKeysByPrefix = new Dictionary<FormKey, HashSet<FormKey>>()
        {
            [new FormKey("WareHousesByLocation".AsMemory())] =
                new HashSet<FormKey> { new FormKey("Redmond".AsMemory()), new FormKey("Seattle".AsMemory()), new FormKey("New York".AsMemory()) }
        };
        var data = new Dictionary<FormKey, StringValues>()
        {
            [new FormKey("Name".AsMemory())] = "Microsoft",
            [new FormKey("WareHousesByLocation[Redmond].Name".AsMemory())] = "Redmond",
            [new FormKey("WareHousesByLocation[Redmond].Address.City".AsMemory())] = "Redmond",
            [new FormKey("WareHousesByLocation[Redmond].Address[Country]".AsMemory())] = "United States",
            [new FormKey("WareHousesByLocation[Redmond].Address.Street".AsMemory())] = "1 Microsoft Way",
            [new FormKey("WareHousesByLocation[Redmond].Address.ZipCode".AsMemory())] = "98052",
            [new FormKey("WareHousesByLocation[Seattle].Name".AsMemory())] = "Seattle",
            [new FormKey("WareHousesByLocation[Seattle].Address.City".AsMemory())] = "Seattle",
            [new FormKey("WareHousesByLocation[Seattle].Address[Country]".AsMemory())] = "United States",
            [new FormKey("WareHousesByLocation[Seattle].Address.Street".AsMemory())] = "1 Microsoft Way",
            [new FormKey("WareHousesByLocation[Seattle].Address.ZipCode".AsMemory())] = "98052",
            [new FormKey("WareHousesByLocation[New York].Name".AsMemory())] = "New York",
            [new FormKey("WareHousesByLocation[New York].Address.City".AsMemory())] = "New York",
            [new FormKey("WareHousesByLocation[New York].Address[Country]".AsMemory())] = "United States",
            [new FormKey("WareHousesByLocation[New York].Address.Street".AsMemory())] = "1 Microsoft Way",
            [new FormKey("WareHousesByLocation[New York].Address.ZipCode".AsMemory())] = "98052",
        };

        var reader = new FormDataReader(data, CultureInfo.InvariantCulture, buffer);
        var keys = reader.ProcessFormKeys();
        Assert.Equal(4, keys.Count);
        Assert.Collection(keys,
            kvp =>
            {
                Assert.Equal("WareHousesByLocation", kvp.Key.Value.ToString());
                Assert.Collection(kvp.Value,
                    e => Assert.Equal("[Redmond]", e.Value.ToString()),
                    e => Assert.Equal("[Seattle]", e.Value.ToString()),
                    e => Assert.Equal("[New York]", e.Value.ToString()));
            },
            kvp =>
            {
                Assert.Equal("WareHousesByLocation[Redmond].Address", kvp.Key.Value.ToString());
                var value = Assert.Single(kvp.Value);
                Assert.Equal("[Country]", value.Value.ToString());
            },
            kvp =>
            {
                Assert.Equal("WareHousesByLocation[Seattle].Address", kvp.Key.Value.ToString());
                var value = Assert.Single(kvp.Value);
                Assert.Equal("[Country]", value.Value.ToString());
            },
            kvp =>
            {
                Assert.Equal("WareHousesByLocation[New York].Address", kvp.Key.Value.ToString());
                var value = Assert.Single(kvp.Value);
                Assert.Equal("[Country]", value.Value.ToString());
            });
    }
}
