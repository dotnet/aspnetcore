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
    public class StringLengthAttributeAdapterTest
    {
        [Fact]
        [ReplaceCulture]
        public void GetClientValidationRules_WithMaxLength_ReturnsValidationParameters()
        {
            // Arrange
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = provider.GetMetadataForProperty(() => null, typeof(string), "Length");
            var attribute = new StringLengthAttribute(8);
            var adapter = new StringLengthAttributeAdapter(attribute);
            var context = new ClientModelValidationContext(metadata, provider);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("length", rule.ValidationType);
            Assert.Equal(1, rule.ValidationParameters.Count);
            Assert.Equal(8, rule.ValidationParameters["max"]);
            Assert.Equal("The field Length must be a string with a maximum length of 8.",
                         rule.ErrorMessage);
        }

        [Fact]
        [ReplaceCulture]
        public void GetClientValidationRules_WithMinAndMaxLength_ReturnsValidationParameters()
        {
            // Arrange
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = provider.GetMetadataForProperty(() => null, typeof(string), "Length");
            var attribute = new StringLengthAttribute(10) { MinimumLength = 3 };
            var adapter = new StringLengthAttributeAdapter(attribute);
            var context = new ClientModelValidationContext(metadata, provider);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("length", rule.ValidationType);
            Assert.Equal(2, rule.ValidationParameters.Count);
            Assert.Equal(3, rule.ValidationParameters["min"]);
            Assert.Equal(10, rule.ValidationParameters["max"]);
            Assert.Equal("The field Length must be a string with a minimum length of 3 and a maximum length of 10.",
                         rule.ErrorMessage);
        }
    }
}
