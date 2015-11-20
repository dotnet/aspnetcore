// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Testing;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class RangeAttributeAdapterTest
    {
        [Fact]
        [ReplaceCulture]
        public void GetClientValidationRules_ReturnsValidationParameters_WithoutLocalization()
        {
            // Arrange
            var provider = TestModelMetadataProvider.CreateDefaultProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), "Length");

            var attribute = new RangeAttribute(typeof(decimal), "0", "100");
            attribute.ErrorMessage = "The field Length must be between {1} and {2}.";

            var expectedMessage = "The field Length must be between 0 and 100.";

            var adapter = new RangeAttributeAdapter(attribute, stringLocalizer: null);

            var actionContext = new ActionContext();
            var context = new ClientModelValidationContext(actionContext, metadata, provider);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("range", rule.ValidationType);
            Assert.Equal(2, rule.ValidationParameters.Count);
            Assert.Equal(0m, rule.ValidationParameters["min"]);
            Assert.Equal(100m, rule.ValidationParameters["max"]);
            Assert.Equal(expectedMessage, rule.ErrorMessage);
        }

        [Fact]
        [ReplaceCulture]
        public void GetClientValidationRules_ReturnsValidationParameters()
        {
            // Arrange
            var provider = TestModelMetadataProvider.CreateDefaultProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), "Length");

            var attribute = new RangeAttribute(typeof(decimal), "0", "100");
            attribute.ErrorMessage = "The field Length must be between {1} and {2}.";

            var expectedProperties = new object[] { "Length", 0m, 100m };
            var expectedMessage = "The field Length must be between 0 and 100.";

            var stringLocalizer = new Mock<IStringLocalizer>();
            stringLocalizer.Setup(s => s[attribute.ErrorMessage, expectedProperties])
                .Returns(new LocalizedString(attribute.ErrorMessage, expectedMessage));

            var adapter = new RangeAttributeAdapter(attribute, stringLocalizer: stringLocalizer.Object);

            var actionContext = new ActionContext();
            var context = new ClientModelValidationContext(actionContext, metadata, provider);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("range", rule.ValidationType);
            Assert.Equal(2, rule.ValidationParameters.Count);
            Assert.Equal(0m, rule.ValidationParameters["min"]);
            Assert.Equal(100m, rule.ValidationParameters["max"]);
            Assert.Equal(expectedMessage, rule.ErrorMessage);
        }
    }
}
