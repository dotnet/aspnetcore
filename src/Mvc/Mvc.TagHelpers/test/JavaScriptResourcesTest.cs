// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

public class JavaScriptResourcesTest
{
    [Fact]
    public void GetEmbeddedJavaScript_LoadsEmbeddedResourceFromManifestStream()
    {
        // Arrange
        var resource = "window.alert('An alert');";
        var expected = resource.Substring(0, resource.Length - 2);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(resource));
        var getManifestResourceStream = new Func<string, Stream>(name => stream);
        var cache = new ConcurrentDictionary<string, string>();

        // Act
        var result = JavaScriptResources.GetEmbeddedJavaScript("test.js", getManifestResourceStream, cache);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetEmbeddedJavaScript_AddsResourceToCacheWhenRead()
    {
        // Arrange
        var resource = "window.alert('An alert');";
        var expected = resource.Substring(0, resource.Length - 2);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(resource));
        var getManifestResourceStream = new Func<string, Stream>(name => stream);
        var cache = new ConcurrentDictionary<string, string>();

        // Act
        var result = JavaScriptResources.GetEmbeddedJavaScript("test.js", getManifestResourceStream, cache);

        // Assert
        Assert.Collection(cache, kvp =>
        {
            Assert.Equal("test.js", kvp.Key);
            Assert.Equal(expected, kvp.Value);
        });
    }

    [Fact]
    public void GetEmbeddedJavaScript_LoadsResourceFromCacheAfterInitialCall()
    {
        // Arrange
        var resource = "window.alert('An alert');";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(resource));
        var callCount = 0;
        var getManifestResourceStream = new Func<string, Stream>(name =>
        {
            callCount++;
            return stream;
        });
        var cache = new ConcurrentDictionary<string, string>();

        // Act
        var result = JavaScriptResources.GetEmbeddedJavaScript("test.js", getManifestResourceStream, cache);
        result = JavaScriptResources.GetEmbeddedJavaScript("test.js", getManifestResourceStream, cache);

        // Assert
        Assert.Equal(1, callCount);
    }

    [Theory]
    [InlineData("window.alert(\"[[[0]]]\")")]
    [InlineData("var test = { a: 1 };")]
    [InlineData("var test = { a: 1, b: \"[[[0]]]\" };")]
    public void GetEmbeddedJavaScript_PreparesJavaScriptCorrectly(string resource)
    {
        // Arrange
        var expected = resource.Substring(0, resource.Length - 2);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(resource));
        var getManifestResourceStream = new Func<string, Stream>(name => stream);
        var cache = new ConcurrentDictionary<string, string>();

        // Act
        var result = JavaScriptResources.GetEmbeddedJavaScript("test.js", getManifestResourceStream, cache);

        // Assert
        Assert.Equal(expected, result);
    }
}
