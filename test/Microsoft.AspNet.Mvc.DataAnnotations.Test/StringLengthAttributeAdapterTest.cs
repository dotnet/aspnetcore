// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Testing;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class StringLengthAttributeAdapterTest
    {
        [Fact]
        [ReplaceCulture]
        public void GetClientValidationRules_WithMaxLength_ReturnsValidationParameters_Localize()
        {
            // Arrange
            var provider = TestModelMetadataProvider.CreateDefaultProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), "Length");

            var attribute = new StringLengthAttribute(8);
            attribute.ErrorMessage = "Property must not be longer than '{1}' characters.";

            var expectedMessage = "Property must not be longer than '8' characters.";

            var stringLocalizer = new Mock<IStringLocalizer>();
            var expectedProperties = new object[] { "Length", 0, 8 };

            stringLocalizer.Setup(s => s[attribute.ErrorMessage, expectedProperties])
                .Returns(new LocalizedString(attribute.ErrorMessage, expectedMessage));

            var adapter = new StringLengthAttributeAdapter(attribute, stringLocalizer: stringLocalizer.Object);

            var actionContext = new ActionContext();
            var context = new ClientModelValidationContext(actionContext, metadata, provider);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("length", rule.ValidationType);
            Assert.Equal(1, rule.ValidationParameters.Count);
            Assert.Equal(8, rule.ValidationParameters["max"]);
            Assert.Equal(expectedMessage, rule.ErrorMessage);
        }

        [Fact]
        [ReplaceCulture]
        public void GetClientValidationRules_WithMaxLength_ReturnsValidationParameters()
        {
            // Arrange
            var provider = TestModelMetadataProvider.CreateDefaultProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), "Length");

            var attribute = new StringLengthAttribute(8);
            var adapter = new StringLengthAttributeAdapter(attribute, stringLocalizer: null);

            var actionContext = new ActionContext();
            var context = new ClientModelValidationContext(actionContext, metadata, provider);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("length", rule.ValidationType);
            Assert.Equal(1, rule.ValidationParameters.Count);
            Assert.Equal(8, rule.ValidationParameters["max"]);
            Assert.Equal(attribute.FormatErrorMessage("Length"), rule.ErrorMessage);
        }

        [Fact]
        [ReplaceCulture]
        public void GetClientValidationRules_WithMinAndMaxLength_ReturnsValidationParameters()
        {
            // Arrange
            var provider = TestModelMetadataProvider.CreateDefaultProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), "Length");

            var attribute = new StringLengthAttribute(10) { MinimumLength = 3 };
            var adapter = new StringLengthAttributeAdapter(attribute, stringLocalizer: null);

            var actionContext = new ActionContext();
            var context = new ClientModelValidationContext(actionContext, metadata, provider);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("length", rule.ValidationType);
            Assert.Equal(2, rule.ValidationParameters.Count);
            Assert.Equal(3, rule.ValidationParameters["min"]);
            Assert.Equal(10, rule.ValidationParameters["max"]);
            Assert.Equal(attribute.FormatErrorMessage("Length"), rule.ErrorMessage);
        }
    }
}
