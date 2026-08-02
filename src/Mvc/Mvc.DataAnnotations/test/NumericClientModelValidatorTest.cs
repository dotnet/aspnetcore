// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

public class NumericClientModelValidatorTest
{
    [Fact]
    [ReplaceCulture]
    public void AddValidation_CorrectValidationTypeAndErrorMessage()
    {
        // Arrange
        var provider = TestModelMetadataProvider.CreateDefaultProvider();
        var metadata = provider.GetMetadataForProperty(typeof(TypeWithNumericProperty), "Id");

        var adapter = new NumericClientModelValidator();

        var actionContext = new ActionContext();
        var context = new ClientModelValidationContext(actionContext, metadata, provider, new Dictionary<string, string>());

        var expectedMessage = "The field DisplayId must be a number.";

        // Act
        adapter.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp => { Assert.Equal("data-val-number", kvp.Key); Assert.Equal(expectedMessage, kvp.Value); });
    }

    [Fact]
    public void AddValidation_CorrectValidationTypeAndOverriddenErrorMessage()
    {
        // Arrange
        var expectedMessage = "Error message about 'DisplayId' from override.";
        var provider = new TestModelMetadataProvider();
        provider
            .ForProperty(typeof(TypeWithNumericProperty), nameof(TypeWithNumericProperty.Id))
            .BindingDetails(d =>
            {
                d.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(
                    name => $"Error message about '{ name }' from override.");
            });
        var metadata = provider.GetMetadataForProperty(
            typeof(TypeWithNumericProperty),
            nameof(TypeWithNumericProperty.Id));

        var adapter = new NumericClientModelValidator();

        var actionContext = new ActionContext();
        var context = new ClientModelValidationContext(actionContext, metadata, provider, new Dictionary<string, string>());

        // Act
        adapter.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp => { Assert.Equal("data-val-number", kvp.Key); Assert.Equal(expectedMessage, kvp.Value); });
    }

    [Fact]
    public void AddValidation_CorrectValidationTypeAndOverriddenErrorMessage_WithParameter()
    {
        // Arrange
        var expectedMessage = "Error message about 'number' from override.";

        var method = typeof(TypeWithNumericProperty).GetMethod(nameof(TypeWithNumericProperty.IsLovely));
        var parameter = method.GetParameters()[0]; // IsLovely(double number)
        var provider = new TestModelMetadataProvider();
        provider
            .ForParameter(parameter)
            .BindingDetails(d =>
            {
                d.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(
                    name => $"Error message about '{ name }' from override.");
            });
        var metadata = provider.GetMetadataForParameter(parameter);

        var adapter = new NumericClientModelValidator();
        var actionContext = new ActionContext();
        var context = new ClientModelValidationContext(actionContext, metadata, provider, new Dictionary<string, string>());

        // Act
        adapter.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp => { Assert.Equal("data-val-number", kvp.Key); Assert.Equal(expectedMessage, kvp.Value); });
    }

    [Fact]
    public void AddValidation_CorrectValidationTypeAndOverriddenErrorMessage_WithType()
    {
        // Arrange
        var expectedMessage = "Error message from override.";
        var provider = new TestModelMetadataProvider();
        provider
            .ForType(typeof(int))
            .BindingDetails(d => d.ModelBindingMessageProvider.SetNonPropertyValueMustBeANumberAccessor(
                () => $"Error message from override."));
        var metadata = provider.GetMetadataForType(typeof(int));

        var adapter = new NumericClientModelValidator();
        var actionContext = new ActionContext();
        var context = new ClientModelValidationContext(actionContext, metadata, provider, new Dictionary<string, string>());

        // Act
        adapter.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp => { Assert.Equal("data-val-number", kvp.Key); Assert.Equal(expectedMessage, kvp.Value); });
    }

    [Fact]
    [ReplaceCulture]
    public void AddValidation_DoesNotTrounceExistingAttributes()
    {
        // Arrange
        var provider = TestModelMetadataProvider.CreateDefaultProvider();
        var metadata = provider.GetMetadataForProperty(typeof(TypeWithNumericProperty), "Id");

        var adapter = new NumericClientModelValidator();

        var actionContext = new ActionContext();
        var context = new ClientModelValidationContext(actionContext, metadata, provider, new Dictionary<string, string>());

        context.Attributes.Add("data-val", "original");
        context.Attributes.Add("data-val-number", "original");

        // Act
        adapter.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("original", kvp.Value); },
            kvp => { Assert.Equal("data-val-number", kvp.Key); Assert.Equal("original", kvp.Value); });
    }

    private class TypeWithNumericProperty
    {
        [Display(Name = "DisplayId")]
        public float Id { get; set; }

        public bool IsLovely(double number)
        {
            return true;
        }
    }
}
