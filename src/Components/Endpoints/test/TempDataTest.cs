// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

public class TempDataTest
{
    [Fact]
    public void Indexer_CanSetAndGetValues()
    {
        var tempData = new TempData();
        tempData["Key1"] = "Value1";
        var value = tempData["Key1"];
        Assert.Equal("Value1", value);
    }

    [Fact]
    public void Get_ReturnsValueAndRemovesFromRetainedKeys()
    {
        var tempData = new TempData();
        tempData["Key1"] = "Value1";

        var value = tempData.Get("Key1");

        Assert.Equal("Value1", value);
        var saved = tempData.Save();
        Assert.Empty(saved);
    }

    [Fact]
    public void Get_ReturnsNullForNonExistentKey()
    {
        var tempData = new TempData();
        var value = tempData.Get("NonExistent");
        Assert.Null(value);
    }

    [Fact]
    public void Peek_ReturnsValueWithoutRemovingFromRetainedKeys()
    {
        var tempData = new TempData();
        tempData["Key1"] = "Value1";
        var value = tempData.Peek("Key1");
        Assert.Equal("Value1", value);
        value = tempData.Get("Key1");
        Assert.Equal("Value1", value);
    }

    [Fact]
    public void Peek_ReturnsNullForNonExistentKey()
    {
        var tempData = new TempData();
        var value = tempData.Peek("NonExistent");
        Assert.Null(value);
    }

    [Fact]
    public void Keep_RetainsAllKeys()
    {
        var tempData = new TempData();
        tempData["Key1"] = "Value1";
        tempData["Key2"] = "Value2";
        _ = tempData.Get("Key1");
        _ = tempData.Get("Key2");

        tempData.Keep();

        var value1 = tempData.Get("Key1");
        var value2 = tempData.Get("Key2");
        Assert.Equal("Value1", value1);
        Assert.Equal("Value2", value2);
    }

    [Fact]
    public void KeepWithKey_RetainsSpecificKey()
    {
        var tempData = new TempData();
        tempData["Key1"] = "Value1";
        tempData["Key2"] = "Value2";
        _ = tempData.Get("Key1");
        _ = tempData.Get("Key2");

        tempData.Keep("Key1");

        var saved = tempData.Save();
        Assert.Single(saved);
        Assert.Equal("Value1", saved["Key1"]);
    }

    [Fact]
    public void KeepWithKey_DoesNothingForNonExistentKey()
    {
        var tempData = new TempData();
        tempData["Key1"] = "Value1";
        _ = tempData.Get("Key1");

        tempData.Keep("NonExistent");

        var value = tempData.Get("NonExistent");
        Assert.Null(value);
    }

    [Fact]
    public void ContainsKey_ReturnsTrueForExistingKey()
    {
        var tempData = new TempData();
        tempData["Key1"] = "Value1";
        var result = tempData.ContainsKey("Key1");
        Assert.True(result);
    }

    [Fact]
    public void ContainsKey_ReturnsFalseForNonExistentKey()
    {
        var tempData = new TempData();
        var result = tempData.ContainsKey("NonExistent");
        Assert.False(result);
    }

    [Fact]
    public void Remove_RemovesKeyAndReturnsTrue()
    {
        var tempData = new TempData();
        tempData["Key1"] = "Value1";

        var result = tempData.Remove("Key1");

        Assert.True(result);
        var value = tempData.Get("Key1");
        Assert.Null(value);
    }

    [Fact]
    public void Remove_ReturnsFalseForNonExistentKey()
    {
        var tempData = new TempData();
        var result = tempData.Remove("NonExistent");
        Assert.False(result);
    }

    [Fact]
    public void Save_ReturnsOnlyRetainedKeys()
    {
        var tempData = new TempData();
        tempData["Key1"] = "Value1";
        tempData["Key2"] = "Value2";
        tempData["Key3"] = "Value3";
        _ = tempData.Get("Key1");
        _ = tempData.Get("Key2");

        var saved = tempData.Save();

        Assert.Single(saved);
        Assert.Equal("Value3", saved["Key3"]);
    }

    [Fact]
    public void Load_PopulatesDataFromDictionary()
    {
        var tempData = new TempData();
        var dataToLoad = new Dictionary<string, object>
        {
            ["Key1"] = "Value1",
            ["Key2"] = "Value2"
        };

        tempData.Load(dataToLoad);

        Assert.Equal("Value1", tempData.Get("Key1"));
        Assert.Equal("Value2", tempData.Get("Key2"));
    }

    [Fact]
    public void Load_ClearsExistingDataBeforeLoading()
    {
        var tempData = new TempData();
        tempData["ExistingKey"] = "ExistingValue";
        var dataToLoad = new Dictionary<string, object>
        {
            ["NewKey"] = "NewValue"
        };

        tempData.Load(dataToLoad);

        Assert.False(tempData.ContainsKey("ExistingKey"));
        Assert.True(tempData.ContainsKey("NewKey"));
    }

    [Fact]
    public void Clear_RemovesAllData()
    {
        var tempData = new TempData();
        tempData["Key1"] = "Value1";
        tempData["Key2"] = "Value2";

        tempData.Clear();

        Assert.Null(tempData.Get("Key1"));
        Assert.Null(tempData.Get("Key2"));
    }

    [Fact]
    public void Indexer_IsCaseInsensitive()
    {
        var tempData = new TempData();
        tempData["Key1"] = "Value1";
        var value = tempData["KEY1"];
        Assert.Equal("Value1", value);
    }
}
