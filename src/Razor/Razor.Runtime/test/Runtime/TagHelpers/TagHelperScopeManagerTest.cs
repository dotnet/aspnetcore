// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Razor.Runtime.TagHelpers;

public class TagHelperScopeManagerTest
{
    [Fact]
    public void Begin_DoesNotRequireParentExecutionContext()
    {
        // Arrange & Act
        var scopeManager = CreateDefaultScopeManager();

        // Act
        var executionContext = BeginDefaultScope(scopeManager, tagName: "p");
        executionContext.Items["test-entry"] = 1234;

        // Assert
        var executionContextItem = Assert.Single(executionContext.Items);
        Assert.Equal("test-entry", (string)executionContextItem.Key, StringComparer.Ordinal);
        Assert.Equal(1234, executionContextItem.Value);
    }

    [Fact]
    public void Begin_ReturnedExecutionContext_ItemsAreRetrievedFromParentExecutionContext()
    {
        // Arrange
        var scopeManager = CreateDefaultScopeManager();
        var parentExecutionContext = BeginDefaultScope(scopeManager, tagName: "p");
        parentExecutionContext.Items["test-entry"] = 1234;

        // Act
        var executionContext = BeginDefaultScope(scopeManager, tagName: "p");

        // Assert
        var executionContextItem = Assert.Single(executionContext.Items);
        Assert.Equal("test-entry", (string)executionContextItem.Key, StringComparer.Ordinal);
        Assert.Equal(1234, executionContextItem.Value);
    }

    [Fact]
    public void Begin_DoesShallowCopyOfParentItems()
    {
        // Arrange
        var scopeManager = CreateDefaultScopeManager();
        var parentComplexObject = new Dictionary<string, int>(StringComparer.Ordinal);
        var parentExecutionContext = BeginDefaultScope(scopeManager, tagName: "p");
        parentExecutionContext.Items["test-entry"] = parentComplexObject;
        var executionContext = BeginDefaultScope(scopeManager, tagName: "p");

        // Act
        ((Dictionary<string, int>)executionContext.Items["test-entry"]).Add("from-child", 1234);

        // Assert
        var executionContextItem = Assert.Single(executionContext.Items);
        Assert.Equal("test-entry", (string)executionContextItem.Key, StringComparer.Ordinal);
        Assert.Same(parentComplexObject, executionContextItem.Value);
        var parentEntry = Assert.Single(parentComplexObject);
        Assert.Equal("from-child", parentEntry.Key, StringComparer.Ordinal);
        Assert.Equal(1234, parentEntry.Value);
    }

    [Fact]
    public void Begin_ReturnedExecutionContext_ItemsModificationDoesNotAffectParent()
    {
        // Arrange
        var scopeManager = CreateDefaultScopeManager();
        var parentExecutionContext = BeginDefaultScope(scopeManager, tagName: "p");
        parentExecutionContext.Items["test-entry"] = 1234;
        var executionContext = BeginDefaultScope(scopeManager, tagName: "p");

        // Act
        executionContext.Items["test-entry"] = 2222;

        // Assert
        var executionContextItem = Assert.Single(executionContext.Items);
        Assert.Equal("test-entry", (string)executionContextItem.Key, StringComparer.Ordinal);
        Assert.Equal(2222, executionContextItem.Value);
        var parentExecutionContextItem = Assert.Single(parentExecutionContext.Items);
        Assert.Equal("test-entry", (string)parentExecutionContextItem.Key, StringComparer.Ordinal);
        Assert.Equal(1234, parentExecutionContextItem.Value);
    }

    [Fact]
    public void Begin_ReturnedExecutionContext_ItemsInsertionDoesNotAffectParent()
    {
        // Arrange
        var scopeManager = CreateDefaultScopeManager();
        var parentExecutionContext = BeginDefaultScope(scopeManager, tagName: "p");
        var executionContext = BeginDefaultScope(scopeManager, tagName: "p");

        // Act
        executionContext.Items["new-entry"] = 2222;

        // Assert
        var executionContextItem = Assert.Single(executionContext.Items);
        Assert.Equal("new-entry", (string)executionContextItem.Key, StringComparer.Ordinal);
        Assert.Equal(2222, executionContextItem.Value);
        Assert.Empty(parentExecutionContext.Items);
    }

    [Fact]
    public void Begin_ReturnedExecutionContext_ItemsRemovalDoesNotAffectParent()
    {
        // Arrange
        var scopeManager = CreateDefaultScopeManager();
        var parentExecutionContext = BeginDefaultScope(scopeManager, tagName: "p");
        parentExecutionContext.Items["test-entry"] = 1234;
        var executionContext = BeginDefaultScope(scopeManager, tagName: "p");

        // Act
        executionContext.Items.Remove("test-entry");

        // Assert
        Assert.Empty(executionContext.Items);
        var parentExecutionContextItem = Assert.Single(parentExecutionContext.Items);
        Assert.Equal("test-entry", (string)parentExecutionContextItem.Key, StringComparer.Ordinal);
        Assert.Equal(1234, parentExecutionContextItem.Value);
    }

    [Fact]
    public void Begin_CreatesContexts_TagHelperOutput_WithAppropriateTagName()
    {
        // Arrange
        var scopeManager = CreateDefaultScopeManager();

        // Act
        var executionContext = BeginDefaultScope(scopeManager, tagName: "p");
        var output = executionContext.Output;

        // Assert
        Assert.Equal("p", output.TagName);
    }

    [Fact]
    public void Begin_CanNest()
    {
        // Arrange
        var scopeManager = CreateDefaultScopeManager();

        // Act
        var executionContext = BeginDefaultScope(scopeManager, tagName: "p");
        executionContext = BeginDefaultScope(scopeManager, tagName: "div");
        var output = executionContext.Output;

        // Assert
        Assert.Equal("div", output.TagName);
    }

    [Theory]
    [InlineData(TagMode.SelfClosing)]
    [InlineData(TagMode.StartTagAndEndTag)]
    [InlineData(TagMode.StartTagOnly)]
    public void Begin_SetsExecutionContexts_TagHelperOutputTagMode(TagMode tagMode)
    {
        // Arrange
        var scopeManager = CreateDefaultScopeManager();

        // Act
        var executionContext = BeginDefaultScope(scopeManager, "p", tagMode);
        var output = executionContext.Output;

        // Assert
        Assert.Equal(tagMode, output.TagMode);
    }

    [Fact]
    public void End_ReturnsParentExecutionContext()
    {
        // Arrange
        var scopeManager = CreateDefaultScopeManager();

        // Act
        var executionContext = BeginDefaultScope(scopeManager, tagName: "p");
        executionContext = BeginDefaultScope(scopeManager, tagName: "div");
        executionContext = scopeManager.End();
        var output = executionContext.Output;

        // Assert
        Assert.Equal("p", output.TagName);
    }

    [Fact]
    public void End_ReturnsNullIfNoNestedContext()
    {
        // Arrange
        var scopeManager = CreateDefaultScopeManager();

        // Act
        var executionContext = BeginDefaultScope(scopeManager, tagName: "p");
        executionContext = BeginDefaultScope(scopeManager, tagName: "div");
        executionContext = scopeManager.End();
        executionContext = scopeManager.End();

        // Assert
        Assert.Null(executionContext);
    }

    [Fact]
    public void End_ThrowsIfNoScope()
    {
        // Arrange
        var scopeManager = CreateDefaultScopeManager();
        var expectedError = string.Format(
            CultureInfo.CurrentCulture,
            "Must call '{2}.{1}' before calling '{2}.{0}'.",
            nameof(TagHelperScopeManager.End),
            nameof(TagHelperScopeManager.Begin),
            nameof(TagHelperScopeManager));

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            scopeManager.End();
        });

        Assert.Equal(expectedError, ex.Message);
    }

    private static TagHelperScopeManager CreateDefaultScopeManager()
    {
        return new TagHelperScopeManager(
            startTagHelperWritingScope: _ => { },
            endTagHelperWritingScope: () => new DefaultTagHelperContent());
    }

    private static TagHelperExecutionContext BeginDefaultScope(
        TagHelperScopeManager scopeManager,
        string tagName,
        TagMode tagMode = TagMode.StartTagAndEndTag)
    {
        return scopeManager.Begin(
            tagName,
            tagMode,
            uniqueId: string.Empty,
            executeChildContentAsync: async () => await Task.FromResult(result: true));
    }
}
