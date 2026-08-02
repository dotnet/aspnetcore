// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Localization;
using Moq;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

public class RequiredAttributeAdapterTest
{
    [Fact]
    [ReplaceCulture]
    public void AddValidation_AddsValidation_Localize()
    {
        // Arrange
        var provider = TestModelMetadataProvider.CreateDefaultProvider();
        var metadata = provider.GetMetadataForProperty(typeof(string), "Length");

        var attribute = new RequiredAttribute();

        var expectedProperties = new object[] { "Length" };
        var message = "This parameter is required.";
        var expectedMessage = "FR This parameter is required.";
        attribute.ErrorMessage = message;

        var stringLocalizer = new Mock<IStringLocalizer>();
        stringLocalizer.Setup(s => s[attribute.ErrorMessage, expectedProperties])
            .Returns(new LocalizedString(attribute.ErrorMessage, expectedMessage));

        var adapter = new RequiredAttributeAdapter(attribute, stringLocalizer: stringLocalizer.Object);

        var actionContext = new ActionContext();
        var context = new ClientModelValidationContext(actionContext, metadata, provider, new Dictionary<string, string>());

        // Act
        adapter.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp => { Assert.Equal("data-val-required", kvp.Key); Assert.Equal(expectedMessage, kvp.Value); });
    }

    [Fact]
    [ReplaceCulture]
    public void AddValidation_AddsValidation()
    {
        // Arrange
        var expectedMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Length");
        var provider = TestModelMetadataProvider.CreateDefaultProvider();
        var metadata = provider.GetMetadataForProperty(typeof(string), "Length");

        var attribute = new RequiredAttribute();
        var adapter = new RequiredAttributeAdapter(attribute, stringLocalizer: null);

        var actionContext = new ActionContext();
        var context = new ClientModelValidationContext(actionContext, metadata, provider, new Dictionary<string, string>());

        // Act
        adapter.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp => { Assert.Equal("data-val-required", kvp.Key); Assert.Equal(expectedMessage, kvp.Value); });
    }

    [Fact]
    [ReplaceCulture]
    public void AddValidation_DoesNotTrounceExistingAttributes()
    {
        // Arrange
        var expectedMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Length");
        var provider = TestModelMetadataProvider.CreateDefaultProvider();
        var metadata = provider.GetMetadataForProperty(typeof(string), "Length");

        var attribute = new RequiredAttribute();
        var adapter = new RequiredAttributeAdapter(attribute, stringLocalizer: null);

        var actionContext = new ActionContext();
        var context = new ClientModelValidationContext(actionContext, metadata, provider, new Dictionary<string, string>());

        context.Attributes.Add("data-val", "original");
        context.Attributes.Add("data-val-required", "original");

        // Act
        adapter.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("original", kvp.Value); },
            kvp => { Assert.Equal("data-val-required", kvp.Key); Assert.Equal("original", kvp.Value); });
    }
}
