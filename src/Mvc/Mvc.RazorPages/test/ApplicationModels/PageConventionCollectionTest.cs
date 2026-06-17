// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class PageConventionCollectionTest
{
    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string.")]
    public void EnsureValidPageName_ThrowsIfPageNameIsNullOrEmpty(string pageName, string expectedMessage)
    {
        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => PageConventionCollection.EnsureValidPageName(pageName),
            "pageName",
            expectedMessage);
    }

    [Theory]
    [InlineData("path-without-slash")]
    [InlineData(@"c:\myapp\path-without-slash")]
    public void EnsureValidPageName_ThrowsIfPageNameDoesNotStartWithLeadingSlash(string pageName)
    {
        // Arrange
        var expected = $"'{pageName}' is not a valid page name. A page name is path relative to the Razor Pages root directory that starts with a leading forward slash ('/') and does not contain the file extension e.g \"/Users/Edit\".";
        // Act & Assert
        var ex = ExceptionAssert.ThrowsArgument(
            () => PageConventionCollection.EnsureValidPageName(pageName),
            "pageName",
            expected);
    }

    [Fact]
    public void EnsureValidPageName_ThrowsIfPageNameHasExtension()
    {
        // Arrange
        var pageName = "/Page.cshtml";
        var expected = $"'{pageName}' is not a valid page name. A page name is path relative to the Razor Pages root directory that starts with a leading forward slash ('/') and does not contain the file extension e.g \"/Users/Edit\".";
        // Act & Assert
        var ex = ExceptionAssert.ThrowsArgument(
            () => PageConventionCollection.EnsureValidPageName(pageName),
            "pageName",
            expected);
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string.")]
    public void EnsureValidFolderPath_ThrowsIfPathIsNullOrEmpty(string folderPath, string expectedMessage)
    {
        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => PageConventionCollection.EnsureValidFolderPath(folderPath),
            "folderPath",
            expectedMessage);
    }

    [Theory]
    [InlineData("path-without-slash")]
    [InlineData(@"c:\myapp\path-without-slash")]
    public void EnsureValidFolderPath_ThrowsIfPageNameDoesNotStartWithLeadingSlash(string folderPath)
    {
        // Arrange
        // Act & Assert
        var ex = ExceptionAssert.ThrowsArgument(
            () => PageConventionCollection.EnsureValidFolderPath(folderPath),
            "folderPath",
            "Path must be a root relative path that starts with a forward slash '/'.");
    }

    [Fact]
    public void RemoveType_RemovesAllOfType()
    {
        // Arrange
        var collection = new PageConventionCollection(Mock.Of<IServiceProvider>())
            {
                new FooPageConvention(),
                new BarPageConvention(),
                new FooPageConvention()
            };

        // Act
        collection.RemoveType(typeof(FooPageConvention));

        // Assert
        Assert.Collection(
            collection,
            convention => Assert.IsType<BarPageConvention>(convention));
    }

    [Fact]
    public void GenericRemoveType_RemovesAllOfType()
    {
        // Arrange
        var collection = new PageConventionCollection(Mock.Of<IServiceProvider>())
            {
                new FooPageConvention(),
                new BarPageConvention(),
                new FooPageConvention()
            };

        // Act
        collection.RemoveType<FooPageConvention>();

        // Assert
        Assert.Collection(
           collection,
           convention => Assert.IsType<BarPageConvention>(convention));
    }

    private class FooPageConvention : IPageConvention { }

    private class BarPageConvention : IPageConvention { }
}
