// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class MaxLengthAttributeAdapterTest
    {
        [Fact]
        [ReplaceCulture]
        public void ClientRulesWithMaxLengthAttribute()
        {
            // Arrange
            var provider = TestModelMetadataProvider.CreateDefaultProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), "Length");
            var attribute = new MaxLengthAttribute(10);
            var adapter = new MaxLengthAttributeAdapter(attribute, stringLocalizer: null);
            var serviceCollection = new ServiceCollection();
            var requestServices = serviceCollection.BuildServiceProvider();
            var context = new ClientModelValidationContext(metadata, provider, requestServices);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("maxlength", rule.ValidationType);
            Assert.Equal(1, rule.ValidationParameters.Count);
            Assert.Equal(10, rule.ValidationParameters["max"]);
            Assert.Equal(attribute.FormatErrorMessage("Length"), rule.ErrorMessage);
        }

        [Fact]
        [ReplaceCulture]
        public void ClientRulesWithMaxLengthAttributeAndCustomMessage()
        {
            // Arrange
            var propertyName = "Length";
            var message = "{0} must be at most {1}";
            var provider = TestModelMetadataProvider.CreateDefaultProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), propertyName);
            var attribute = new MaxLengthAttribute(5) { ErrorMessage = message };
            var adapter = new MaxLengthAttributeAdapter(attribute, stringLocalizer: null);
            var serviceCollection = new ServiceCollection();
            var requestServices = serviceCollection.BuildServiceProvider();
            var context = new ClientModelValidationContext(metadata, provider, requestServices);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("maxlength", rule.ValidationType);
            Assert.Equal(1, rule.ValidationParameters.Count);
            Assert.Equal(5, rule.ValidationParameters["max"]);
            Assert.Equal("Length must be at most 5", rule.ErrorMessage);
        }

        [Fact]
        [ReplaceCulture]
        public void ClientRulesWithMaxLengthAttribute_StringLocalizer_ReturnsLocalizedErrorString()
        {
            // Arrange
            var provider = TestModelMetadataProvider.CreateDefaultProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), "Length");
            var errorKey = metadata.GetDisplayName();
            var attribute = new MaxLengthAttribute(10);
            attribute.ErrorMessage = errorKey;

            var localizedString = new LocalizedString(errorKey, "Longueur est invalide");
            var stringLocalizer = new Mock<IStringLocalizer>();
            stringLocalizer.Setup(s => s[errorKey]).Returns(localizedString);

            var adapter = new MaxLengthAttributeAdapter(attribute, stringLocalizer.Object);
            var serviceCollection = new ServiceCollection();
            var requestServices = serviceCollection.BuildServiceProvider();
            var context = new ClientModelValidationContext(metadata, provider, requestServices);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("maxlength", rule.ValidationType);
            Assert.Equal(1, rule.ValidationParameters.Count);
            Assert.Equal(10, rule.ValidationParameters["max"]);
            Assert.Equal("Longueur est invalide", rule.ErrorMessage);
        }
    }
}