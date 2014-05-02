// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class MinLengthAttributeAdapterTest
    {
        [Fact]
        [ReplaceCulture]
        public void ClientRulesWithMinLengthAttribute()
        {
            // Arrange
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = provider.GetMetadataForProperty(() => null, typeof(string), "Length");
            var attribute = new MinLengthAttribute(6);
            var adapter = new MinLengthAttributeAdapter(attribute);
            var context = new ClientModelValidationContext(metadata, provider);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("minlength", rule.ValidationType);
            Assert.Equal(1, rule.ValidationParameters.Count);
            Assert.Equal(6, rule.ValidationParameters["min"]);
            Assert.Equal("The field Length must be a string or array type with a minimum length of '6'.", rule.ErrorMessage);
        }

        [Fact]
        [ReplaceCulture]
        public void ClientRulesWithMinLengthAttributeAndCustomMessage()
        {
            // Arrange
            var propertyName = "Length";
            var message = "Array must have at least {1} items.";
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = provider.GetMetadataForProperty(() => null, typeof(string), propertyName);
            var attribute = new MinLengthAttribute(2) { ErrorMessage = message };
            var adapter = new MinLengthAttributeAdapter(attribute);
            var context = new ClientModelValidationContext(metadata, provider);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("minlength", rule.ValidationType);
            Assert.Equal(1, rule.ValidationParameters.Count);
            Assert.Equal(2, rule.ValidationParameters["min"]);
            Assert.Equal("Array must have at least 2 items.", rule.ErrorMessage);
        }
    }
}