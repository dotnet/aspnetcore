// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Testing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class RangeAttributeAdapterTest
    {
        [Fact]
        [ReplaceCulture]
        public void GetClientValidationRules_ReturnsValidationParameters()
        {
            // Arrange
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = provider.GetMetadataForProperty(typeof(string), "Length");
            var attribute = new RangeAttribute(typeof(decimal), "0", "100");
            var adapter = new RangeAttributeAdapter(attribute);
            var serviceCollection = new ServiceCollection();
            var requestServices = serviceCollection.BuildServiceProvider();
            var context = new ClientModelValidationContext(metadata, provider, requestServices);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("range", rule.ValidationType);
            Assert.Equal(2, rule.ValidationParameters.Count);
            Assert.Equal(0m, rule.ValidationParameters["min"]);
            Assert.Equal(100m, rule.ValidationParameters["max"]);
            Assert.Equal(@"The field Length must be between 0 and 100.", rule.ErrorMessage);
        }
    }
}
