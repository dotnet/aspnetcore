// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Localization;
using Moq;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

public class MaxLengthAttributeAdapterTest
{
    [Fact]
    [ReplaceCulture]
    public void MaxLengthAttribute_AddValidation_Localize()
    {
        // Arrange
        var provider = TestModelMetadataProvider.CreateDefaultProvider();
        var metadata = provider.GetMetadataForProperty(typeof(string), "Length");

        var attribute = new MaxLengthAttribute(10);
        attribute.ErrorMessage = "Property must be max '{1}' characters long.";

        var expectedProperties = new object[] { "Length", 10 };
        var expectedMessage = "Property must be max '10' characters long.";

        var stringLocalizer = new Mock<IStringLocalizer>();
        stringLocalizer.Setup(s => s[attribute.ErrorMessage, expectedProperties])
            .Returns(new LocalizedString(attribute.ErrorMessage, expectedMessage));

        var adapter = new MaxLengthAttributeAdapter(attribute, stringLocalizer: stringLocalizer.Object);

        var actionContext = new ActionContext();
        var context = new ClientModelValidationContext(actionContext, metadata, provider, new Dictionary<string, string>());

        // Act
        adapter.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp => { Assert.Equal("data-val-maxlength", kvp.Key); Assert.Equal(expectedMessage, kvp.Value); },
            kvp => { Assert.Equal("data-val-maxlength-max", kvp.Key); Assert.Equal("10", kvp.Value); });
    }

    [Fact]
    [ReplaceCulture]
    public void MaxLengthAttribute_AddValidation()
    {
        // Arrange
        var provider = TestModelMetadataProvider.CreateDefaultProvider();
        var metadata = provider.GetMetadataForProperty(typeof(string), "Length");

        var attribute = new MaxLengthAttribute(10);
        var adapter = new MaxLengthAttributeAdapter(attribute, stringLocalizer: null);

        var expectedMessage = attribute.FormatErrorMessage("Length");

        var actionContext = new ActionContext();
        var context = new ClientModelValidationContext(actionContext, metadata, provider, new Dictionary<string, string>());

        // Act
        adapter.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp => { Assert.Equal("data-val-maxlength", kvp.Key); Assert.Equal(expectedMessage, kvp.Value); },
            kvp => { Assert.Equal("data-val-maxlength-max", kvp.Key); Assert.Equal("10", kvp.Value); });
    }

    [Fact]
    [ReplaceCulture]
    public void MaxLengthAttribute_AddValidation_CustomMessage()
    {
        // Arrange
        var propertyName = "Length";
        var message = "{0} must be at most {1}";
        var provider = TestModelMetadataProvider.CreateDefaultProvider();
        var metadata = provider.GetMetadataForProperty(typeof(string), propertyName);

        var expectedMessage = "Length must be at most 5";

        var attribute = new MaxLengthAttribute(5) { ErrorMessage = message };
        var adapter = new MaxLengthAttributeAdapter(attribute, stringLocalizer: null);

        var actionContext = new ActionContext();
        var context = new ClientModelValidationContext(actionContext, metadata, provider, new Dictionary<string, string>());

        // Act
        adapter.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp => { Assert.Equal("data-val-maxlength", kvp.Key); Assert.Equal(expectedMessage, kvp.Value); },
            kvp => { Assert.Equal("data-val-maxlength-max", kvp.Key); Assert.Equal("5", kvp.Value); });
    }

    [Fact]
    [ReplaceCulture]
    public void MaxLengthAttribute_AddValidation_StringLocalizer_ReturnsLocalizedErrorString()
    {
        // Arrange
        var provider = TestModelMetadataProvider.CreateDefaultProvider();
        var metadata = provider.GetMetadataForProperty(typeof(string), "Length");

        var errorKey = metadata.GetDisplayName();
        var attribute = new MaxLengthAttribute(10);
        attribute.ErrorMessage = errorKey;
        var localizedString = new LocalizedString(errorKey, "Longueur est invalide");
        var stringLocalizer = new Mock<IStringLocalizer>();
        stringLocalizer.Setup(s => s[errorKey, metadata.GetDisplayName(), attribute.Length]).Returns(localizedString);

        var expectedMessage = "Longueur est invalide";

        var adapter = new MaxLengthAttributeAdapter(attribute, stringLocalizer.Object);

        var actionContext = new ActionContext();
        var context = new ClientModelValidationContext(actionContext, metadata, provider, new Dictionary<string, string>());

        // Act
        adapter.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp => { Assert.Equal("data-val-maxlength", kvp.Key); Assert.Equal(expectedMessage, kvp.Value); },
            kvp => { Assert.Equal("data-val-maxlength-max", kvp.Key); Assert.Equal("10", kvp.Value); });
    }

    [Fact]
    [ReplaceCulture]
    public void AddValidation_DoesNotTrounceExistingAttributes()
    {
        // Arrange
        var provider = TestModelMetadataProvider.CreateDefaultProvider();
        var metadata = provider.GetMetadataForProperty(typeof(string), "Length");

        var attribute = new MaxLengthAttribute(10);
        var adapter = new MaxLengthAttributeAdapter(attribute, stringLocalizer: null);

        var expectedMessage = attribute.FormatErrorMessage("Length");

        var actionContext = new ActionContext();
        var context = new ClientModelValidationContext(actionContext, metadata, provider, new Dictionary<string, string>());

        context.Attributes.Add("data-val", "original");
        context.Attributes.Add("data-val-maxlength", "original");
        context.Attributes.Add("data-val-maxlength-max", "original");

        // Act
        adapter.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("original", kvp.Value); },
            kvp => { Assert.Equal("data-val-maxlength", kvp.Key); Assert.Equal("original", kvp.Value); },
            kvp => { Assert.Equal("data-val-maxlength-max", kvp.Key); Assert.Equal("original", kvp.Value); });
    }
}
