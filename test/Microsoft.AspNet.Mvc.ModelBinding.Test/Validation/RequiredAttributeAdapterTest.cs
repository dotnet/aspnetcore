// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Testing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class RequiredAttributeAdapterTest
    {
        [Fact]
        [ReplaceCulture]
        public void GetClientValidationRules_ReturnsValidationParameters()
        {
            // Arrange
            var expected = ValidationAttributeUtil.GetRequiredErrorMessage("Length");
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), "Length");
            var attribute = new RequiredAttribute();
            var adapter = new RequiredAttributeAdapter(attribute);
            var serviceCollection = new ServiceCollection();
            var requestServices = serviceCollection.BuildServiceProvider();
            var context = new ClientModelValidationContext(metadata, provider, requestServices);

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
