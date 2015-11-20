// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Testing;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class RequiredAttributeAdapterTest
    {
        [Fact]
        [ReplaceCulture]
        public void GetClientValidationRules_ReturnsValidationParameters_Localize()
        {
            // Arrange
            var provider = TestModelMetadataProvider.CreateDefaultProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), "Length");

            var attribute = new RequiredAttribute();

            var expectedProperties = new object[] { "Length" };
            var message = "This paramter is required.";
            var expectedMessage = "FR This parameter is required.";
            attribute.ErrorMessage = message;

            var stringLocalizer = new Mock<IStringLocalizer>();
            stringLocalizer.Setup(s => s[attribute.ErrorMessage, expectedProperties])
                .Returns(new LocalizedString(attribute.ErrorMessage, expectedMessage));

            var adapter = new RequiredAttributeAdapter(attribute, stringLocalizer: stringLocalizer.Object);

            var actionContext = new ActionContext();
            var context = new ClientModelValidationContext(actionContext, metadata, provider);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("required", rule.ValidationType);
            Assert.Empty(rule.ValidationParameters);
            Assert.Equal(expectedMessage, rule.ErrorMessage);
        }

        [Fact]
        [ReplaceCulture]
        public void GetClientValidationRules_ReturnsValidationParameters()
        {
            // Arrange
            var expected = ValidationAttributeUtil.GetRequiredErrorMessage("Length");
            var provider = TestModelMetadataProvider.CreateDefaultProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), "Length");

            var attribute = new RequiredAttribute();
            var adapter = new RequiredAttributeAdapter(attribute, stringLocalizer: null);

            var actionContext = new ActionContext();
            var context = new ClientModelValidationContext(actionContext, metadata, provider);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("required", rule.ValidationType);
            Assert.Empty(rule.ValidationParameters);
            Assert.Equal(expected, rule.ErrorMessage);
        }
    }
}
