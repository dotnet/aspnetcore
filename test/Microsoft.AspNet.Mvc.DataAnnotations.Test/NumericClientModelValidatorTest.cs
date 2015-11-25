// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class NumericClientModelValidatorTest
    {
        [Fact]
        [ReplaceCulture]
        public void ClientRulesWithCorrectValidationTypeAndErrorMessage()
        {
            // Arrange
            var provider = TestModelMetadataProvider.CreateDefaultProvider();
            var metadata = provider.GetMetadataForProperty(typeof(TypeWithNumericProperty), "Id");

            var adapter = new NumericClientModelValidator();

            var actionContext = new ActionContext();
            var context = new ClientModelValidationContext(actionContext, metadata, provider);

            var expectedMessage = "The field DisplayId must be a number.";

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("number", rule.ValidationType);
            Assert.Equal(expectedMessage, rule.ErrorMessage);
        }

        private class TypeWithNumericProperty
        {
            [Display(Name = "DisplayId")]
            public float Id { get; set; }
        }
    }
}
