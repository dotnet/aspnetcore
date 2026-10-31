// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

public class ValidationProblemDetailsTest
{
    [Fact]
    public void Constructor_SetsTitle()
    {
        // Arrange & Act
        var problemDescription = new ValidationProblemDetails();

        // Assert
        Assert.Equal("One or more validation errors occurred.", problemDescription.Title);
        Assert.Empty(problemDescription.Errors);
    }

    [Fact]
    public void Constructor_SerializesErrorsFromModelStateDictionary()
    {
        // Arrange
        var modelStateDictionary = new ModelStateDictionary();
        modelStateDictionary.AddModelError("key1", "error1");
        modelStateDictionary.SetModelValue("key2", new object(), "value");
        modelStateDictionary.AddModelError("key3", "error2");
        modelStateDictionary.AddModelError("key3", "error3");

        // Act
        var problemDescription = new ValidationProblemDetails(modelStateDictionary);

        // Assert
        Assert.Equal("One or more validation errors occurred.", problemDescription.Title);
        Assert.Collection(
            problemDescription.Errors,
            item =>
            {
                Assert.Equal("key1", item.Key);
                Assert.Equal(new[] { "error1" }, item.Value);
            },
            item =>
            {
                Assert.Equal("key3", item.Key);
                Assert.Equal(new[] { "error2", "error3" }, item.Value);
            });
    }

    [Fact]
    public void Constructor_SerializesErrorsFromModelStateDictionary_AddsDefaultMessage()
    {
        // Arrange
        var modelStateDictionary = new ModelStateDictionary();
        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForProperty(typeof(string), nameof(string.Length));
        modelStateDictionary.AddModelError("unsafeError",
            new Exception("This message should not be returned to clients"),
            metadata);

        // Act
        var problemDescription = new ValidationProblemDetails(modelStateDictionary);

        // Assert
        Assert.Equal("One or more validation errors occurred.", problemDescription.Title);
        Assert.Collection(
            problemDescription.Errors,
            item =>
            {
                Assert.Equal("unsafeError", item.Key);
                Assert.Equal(new[] { "The input was not valid." }, item.Value);
            });
    }

    [Fact]
    public void Constructor_CopiesPassedInDictionary()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            ["key1"] = new[] { "error1", "error2" },
            ["key2"] = new[] { "error3", },
        };

        // Act
        var problemDescription = new ValidationProblemDetails(errors);

        // Assert
        Assert.Equal("One or more validation errors occurred.", problemDescription.Title);
        Assert.Collection(
            problemDescription.Errors,
            item =>
            {
                Assert.Equal("key1", item.Key);
                Assert.Equal(new[] { "error1", "error2" }, item.Value);
            },
            item =>
            {
                Assert.Equal("key2", item.Key);
                Assert.Equal(new[] { "error3" }, item.Value);
            });
    }
}
