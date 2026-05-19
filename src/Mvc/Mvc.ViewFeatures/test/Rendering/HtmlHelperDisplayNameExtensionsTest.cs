// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Test the <see cref="HtmlHelperDisplayNameExtensions" /> class.
/// </summary>
public class HtmlHelperDisplayNameExtensionsTest
{
    [Fact]
    public void DisplayNameHelpers_ReturnEmptyForModel()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();
        var enumerableHelper =
            DefaultTemplatesUtilities.GetHtmlHelper<IEnumerable<DefaultTemplatesUtilities.ObjectTemplateModel>>(model: null);

        // Act
        var displayNameResult = helper.DisplayName(expression: string.Empty);
        var displayNameNullResult = helper.DisplayName(expression: null);   // null is another alias for current model
        var displayNameForResult = helper.DisplayNameFor(m => m);
        var displayNameForEnumerableModelResult = enumerableHelper.DisplayNameFor(m => m);
        var displayNameForModelResult = helper.DisplayNameForModel();

        // Assert
        Assert.Empty(displayNameResult);
        Assert.Empty(displayNameNullResult);
        Assert.Empty(displayNameForResult);
        Assert.Empty(displayNameForEnumerableModelResult);
        Assert.Empty(displayNameForModelResult);
    }

    [Fact]
    public void DisplayNameHelpers_ReturnPropertyName()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();
        var enumerableHelper = DefaultTemplatesUtilities.GetHtmlHelperForEnumerable();

        // Act
        var displayNameResult = helper.DisplayName("Property1");
        var displayNameForResult = helper.DisplayNameFor(m => m.Property1);
        var displayNameForEnumerableResult = enumerableHelper.DisplayNameFor(m => m.Property1);

        // Assert
        Assert.Equal("Property1", displayNameResult);
        Assert.Equal("Property1", displayNameForResult);
        Assert.Equal("Property1", displayNameForEnumerableResult);
    }

    // If the metadata is for a type (not property), then DisplayName(expression) will evaluate the expression
    [Fact]
    public void DisplayNameHelpers_DisplayName_Evaluates_Expression()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();
        helper.ViewData["value"] = "testvalue";

        // Act
        var displayNameResult = helper.DisplayName(expression: "value");

        // Assert
        Assert.Equal("value", displayNameResult);
    }

    [Fact]
    public void DisplayNameHelpers_ReturnPropertyName_ForNestedProperty()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper<OuterClass>(model: null);
        var enumerableHelper = DefaultTemplatesUtilities.GetHtmlHelperForEnumerable<OuterClass>(model: null);

        // Act
        var displayNameResult = helper.DisplayName("Inner.Id");
        var displayNameForResult = helper.DisplayNameFor(m => m.Inner.Id);
        var displayNameForEnumerableResult = enumerableHelper.DisplayNameFor(m => m.Inner.Id);

        // Assert
        Assert.Equal("Id", displayNameResult);
        Assert.Equal("Id", displayNameForResult);
        Assert.Equal("Id", displayNameForEnumerableResult);
    }

    [Theory]
    [InlineData("")]    // Empty display name wins over non-empty property name.
    [InlineData("Custom display name from metadata")]
    public void DisplayNameHelpers_ReturnDisplayName_IfNonNull(string displayName)
    {
        // Arrange
        var provider = new TestModelMetadataProvider();
        provider
            .ForType<DefaultTemplatesUtilities.ObjectTemplateModel>()
            .DisplayDetails(dd => dd.DisplayName = () => displayName);

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(provider: provider);
        var enumerableHelper = DefaultTemplatesUtilities.GetHtmlHelperForEnumerable(provider: provider);

        // Act
        var displayNameResult = helper.DisplayName(expression: string.Empty);
        var displayNameForResult = helper.DisplayNameFor(m => m);
        var displayNameForEnumerableResult = enumerableHelper.DisplayNameFor((DefaultTemplatesUtilities.ObjectTemplateModel m) => m);
        var displayNameForModelResult = helper.DisplayNameForModel();

        // Assert
        Assert.Equal(displayName, displayNameResult);
        Assert.Equal(displayName, displayNameForResult);
        Assert.Equal(displayName, displayNameForEnumerableResult);
        Assert.Equal(displayName, displayNameForModelResult);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Custom display name from metadata")]
    public void DisplayNameHelpers_ReturnDisplayNameForProperty_IfNonNull(string displayName)
    {
        // Arrange
        var provider = new TestModelMetadataProvider();
        provider
            .ForProperty<DefaultTemplatesUtilities.ObjectTemplateModel>("Property1")
            .DisplayDetails(dd => dd.DisplayName = () => displayName);

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(provider: provider);
        var enumerableHelper = DefaultTemplatesUtilities.GetHtmlHelperForEnumerable(provider: provider);

        // Act
        var displayNameResult = helper.DisplayName("Property1");
        var displayNameForResult = helper.DisplayNameFor(m => m.Property1);
        var displayNameForEnumerableResult = enumerableHelper.DisplayNameFor(m => m.Property1);

        // Assert
        Assert.Equal(displayName, displayNameResult);
        Assert.Equal(displayName, displayNameForResult);
        Assert.Equal(displayName, displayNameForEnumerableResult);
    }

    [Theory]
    [InlineData("A", "A")]
    [InlineData("A[23]", "A[23]")]
    [InlineData("A[0].B", "B")]
    [InlineData("A.B.C.D", "D")]
    public void DisplayName_ReturnsRightmostExpressionSegment_IfPropertiesNotFound(
        string expression,
        string expectedResult)
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var result = helper.DisplayName(expression);

        // Assert
        // DisplayName() falls back to expression name when DisplayName and PropertyName are null.
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void DisplayNameFor_ThrowsInvalidOperation_IfExpressionUnsupported()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => helper.DisplayNameFor(model => new { foo = "Bar" }));
        Assert.Equal(
            "Templates can be used only with field access, property access, single-dimension array index, or single-parameter custom indexer expressions.",
            exception.Message);
    }

    [Fact]
    public void EnumerableDisplayNameFor_ThrowsInvalidOperation_IfExpressionUnsupported()
    {
        // Arrange
        var helper =
            DefaultTemplatesUtilities.GetHtmlHelper<IEnumerable<DefaultTemplatesUtilities.ObjectTemplateModel>>(model: null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => helper.DisplayNameFor(model => new { foo = "Bar" }));
        Assert.Equal(
            "Templates can be used only with field access, property access, single-dimension array index, or single-parameter custom indexer expressions.",
            exception.Message);
    }

    [Fact]
    public void DisplayNameFor_ReturnsVariableName()
    {
        // Arrange
        var unknownKey = "this is a dummy parameter value";
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var result = helper.DisplayNameFor(model => unknownKey);

        // Assert
        Assert.Equal("unknownKey", result);
    }

    [Fact]
    public void EnumerableDisplayNameFor_ReturnsVariableName()
    {
        // Arrange
        var unknownKey = "this is a dummy parameter value";
        var helper =
            DefaultTemplatesUtilities.GetHtmlHelper<IEnumerable<DefaultTemplatesUtilities.ObjectTemplateModel>>(model: null);

        // Act
        var result = helper.DisplayNameFor(model => unknownKey);

        // Assert
        Assert.Equal("unknownKey", result);
    }

    private sealed class InnerClass
    {
        public int Id { get; set; }
    }

    private sealed class OuterClass
    {
        public InnerClass Inner { get; set; }
    }
}
