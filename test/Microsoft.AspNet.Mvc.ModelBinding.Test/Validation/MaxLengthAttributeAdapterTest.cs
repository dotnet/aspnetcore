using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class MaxLengthAttributeAdapterTest
    {
        [Fact]
        [ReplaceCulture]
        public void ClientRulesWithMaxLengthAttribute()
        {
            // Arrange
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = provider.GetMetadataForProperty(() => null, typeof(string), "Length");
            var attribute = new MaxLengthAttribute(10);
            var adapter = new MaxLengthAttributeAdapter(attribute);
            var context = new ClientModelValidationContext(metadata, provider);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("maxlength", rule.ValidationType);
            Assert.Equal(1, rule.ValidationParameters.Count);
            Assert.Equal(10, rule.ValidationParameters["max"]);
            Assert.Equal("The field Length must be a string or array type with a maximum length of '10'.", rule.ErrorMessage);
        }

        [Fact]
        [ReplaceCulture]
        public void ClientRulesWithMaxLengthAttributeAndCustomMessage()
        {
            // Arrange
            var propertyName = "Length";
            var message = "{0} must be at most {1}";
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = provider.GetMetadataForProperty(() => null, typeof(string), propertyName);
            var attribute = new MaxLengthAttribute(5) { ErrorMessage = message };
            var adapter = new MaxLengthAttributeAdapter(attribute);
            var context = new ClientModelValidationContext(metadata, provider);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("maxlength", rule.ValidationType);
            Assert.Equal(1, rule.ValidationParameters.Count);
            Assert.Equal(5, rule.ValidationParameters["max"]);
            Assert.Equal("Length must be at most 5", rule.ErrorMessage);
        }
    }
}