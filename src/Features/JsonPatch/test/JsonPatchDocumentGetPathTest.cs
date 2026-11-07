// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch;

public class JsonPatchDocumentGetPathTest
{
    [Fact]
    public void ExpressionType_MemberAccess()
    {
        // Arrange
        var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();

        // Act
        var path = patchDocument.GetPath(p => p.SimpleObject.IntegerList, "-");

        // Assert
        Assert.Equal("/SimpleObject/IntegerList/-", path);
    }

    [Fact]
    public void ExpressionType_ArrayIndex()
    {
        // Arrange
        var patchDocument = new JsonPatchDocument<int[]>();

        // Act
        var path = patchDocument.GetPath(p => p[3], null);

        // Assert
        Assert.Equal("/3", path);
    }

    [Fact]
    public void ExpressionType_Call()
    {
        // Arrange
        var patchDocument = new JsonPatchDocument<Dictionary<string, int>>();

        // Act
        var path = patchDocument.GetPath(p => p["key"], "3");

        // Assert
        Assert.Equal("/key/3", path);
    }

    [Theory]
    [InlineData("key", "/key/3")]
    [InlineData("", "//3")]
    [InlineData("/", "/~1/3")]
    [InlineData("~", "/~0/3")]
    [InlineData("~0", "/~00/3")]
    [InlineData("~1", "/~01/3")]
    [InlineData("a/~b", "/a~1~0b/3")]
    public void ExpressionType_Call_EscapesDictionaryKey(string key, string expectedPath)
    {
        var patchDocument = new JsonPatchDocument<Dictionary<string, List<int>>>();

        var path = patchDocument.GetPath(p => p[key], "3");
        var evaluatedPath = patchDocument.GetPath(p => p[string.Concat(key, string.Empty)], "3");

        Assert.Equal(expectedPath, path);
        Assert.Equal(expectedPath, evaluatedPath);
    }

    [Fact]
    public void ExpressionType_Call_EscapesNestedDictionaryKeys()
    {
        var patchDocument = new JsonPatchDocument<Dictionary<string, Dictionary<string, SimpleObject>>>();

        var path = patchDocument.GetPath(p => p["outer/~"]["~1"].IntegerList[2], null);

        Assert.Equal("/outer~1~0/~01/IntegerList/2", path);
    }

    [Fact]
    public void ExpressionType_MemberAccess_EscapesSerializedPropertyNames()
    {
        var patchDocument = new JsonPatchDocument<ObjectWithEscapedPropertyNames>();

        var path = patchDocument.GetPath(p => p.NestedObject.IntegerList[2], null);

        Assert.Equal("/outer~1~01/inner~1~00/2", path);
    }

    [Fact]
    public void Replace_EscapedDictionaryKey_UpdatesOnlyExactKey()
    {
        var nested = new Dictionary<string, int> { ["/"] = 20 };
        var model = new Dictionary<string, object>
        {
            ["a/~1"] = 10,
            ["a"] = nested,
            ["sibling"] = 30
        };
        var patchDocument = new JsonPatchDocument<Dictionary<string, object>>();

        patchDocument.Replace(p => p["a/~1"], 42);

        Assert.Equal("/a~1~01", Assert.Single(patchDocument.Operations).path);

        patchDocument.ApplyTo(model);

        Assert.Equal(42, model["a/~1"]);
        Assert.Equal(20, Assert.IsType<Dictionary<string, int>>(model["a"])["/"]);
        Assert.Equal(30, model["sibling"]);
        Assert.Equal(3, model.Count);
    }

    [Fact]
    public void ExpressionType_Parameter_NullPosition()
    {
        // Arrange
        var patchDocument = new JsonPatchDocument<SimpleObject>();

        // Act
        var path = patchDocument.GetPath(p => p, null);

        // Assert
        Assert.Equal("/", path);
    }

    [Fact]
    public void ExpressionType_Parameter_WithPosition()
    {
        // Arrange
        var patchDocument = new JsonPatchDocument<SimpleObject>();

        // Act
        var path = patchDocument.GetPath(p => p, "-");

        // Assert
        Assert.Equal("/-", path);
    }

    [Fact]
    public void ExpressionType_Convert()
    {
        // Arrange
        var patchDocument = new JsonPatchDocument<NestedObjectWithDerivedClass>();

        // Act
        var path = patchDocument.GetPath(p => (BaseClass)p.DerivedObject, null);

        // Assert
        Assert.Equal("/DerivedObject", path);
    }

    [Fact]
    public void ExpressionType_NotSupported()
    {
        // Arrange
        var patchDocument = new JsonPatchDocument<SimpleObject>();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            patchDocument.GetPath(p => p.IntegerValue >= 4, null);
        });

        // Assert
        Assert.Equal("The expression '(p.IntegerValue >= 4)' is not supported. Supported expressions include member access and indexer expressions.", exception.Message);
    }

    private class ObjectWithEscapedPropertyNames
    {
        [JsonProperty("outer/~1")]
        public ObjectWithEscapedPropertyNames NestedObject { get; set; }

        [JsonProperty("inner/~0")]
        public List<int> IntegerList { get; set; }
    }
}

internal class DerivedClass : BaseClass
{
    public DerivedClass()
    {
    }
}

internal class NestedObjectWithDerivedClass
{
    public DerivedClass DerivedObject { get; set; }
}

internal class BaseClass
{
}
