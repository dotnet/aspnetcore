// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations.Internal
{
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
            var context = new ClientModelValidationContext(actionContext, metadata, provider, new AttributeDictionary());

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
            var context = new ClientModelValidationContext(actionContext, metadata, provider, new AttributeDictionary());

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
            var context = new ClientModelValidationContext(actionContext, metadata, provider, new AttributeDictionary());

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
        }
    }
}
