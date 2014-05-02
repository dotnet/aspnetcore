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
    public class CompareAttributeAdapterTest
    {
        [Fact]
        [ReplaceCulture]
        public void ClientRulesWithCompareAttribute_ErrorMessageUsesDisplayName()
        {
            // Arrange
            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            var metadata = metadataProvider.GetMetadataForProperty(() => null, typeof(PropertyDisplayNameModel), "MyProperty");
            var attribute = new CompareAttribute("OtherProperty");
            var context = new ClientModelValidationContext(metadata, metadataProvider);
            var adapter = new CompareAttributeAdapter(attribute);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("'MyPropertyDisplayName' and 'OtherPropertyDisplayName' do not match.", rule.ErrorMessage);
        }

        [Fact]
        [ReplaceCulture]
        public void ClientRulesWithCompareAttribute_ErrorMessageUsesPropertyName()
        {
            // Arrange
            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            var metadata = metadataProvider.GetMetadataForProperty(() => null, typeof(PropertyNameModel), "MyProperty");
            var attribute = new CompareAttribute("OtherProperty");
            var context = new ClientModelValidationContext(metadata, metadataProvider);
            var adapter = new CompareAttributeAdapter(attribute);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("'MyProperty' and 'OtherProperty' do not match.", rule.ErrorMessage);
        }

        [Fact]
        public void ClientRulesWithCompareAttribute_ErrorMessageUsesOverride()
        {
            // Arrange
            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            var metadata = metadataProvider.GetMetadataForProperty(() => null, typeof(PropertyNameModel), "MyProperty");
            var attribute = new CompareAttribute("OtherProperty")
            {
                ErrorMessage = "Hello '{0}', goodbye '{1}'."
            };
            var context = new ClientModelValidationContext(metadata, metadataProvider);
            var adapter = new CompareAttributeAdapter(attribute);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("Hello 'MyProperty', goodbye 'OtherProperty'.", rule.ErrorMessage);
        }

        [Fact]
        public void ClientRulesWithCompareAttribute_ErrorMessageUsesResourceOverride()
        {
            // Arrange
            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            var metadata = metadataProvider.GetMetadataForProperty(() => null, typeof(PropertyNameModel), "MyProperty");
            var attribute = new CompareAttribute("OtherProperty")
            {
                ErrorMessageResourceName = "CompareAttributeTestResource",
                ErrorMessageResourceType = typeof(Test.Resources),
            };
            var context = new ClientModelValidationContext(metadata, metadataProvider);
            var adapter = new CompareAttributeAdapter(attribute);

            // Act
            var rules = adapter.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(rules);
            Assert.Equal("Comparing MyProperty to OtherProperty.", rule.ErrorMessage);
        }

        private class PropertyDisplayNameModel
        {
            [Display(Name = "MyPropertyDisplayName")]
            public string MyProperty { get; set; }

            [Display(Name = "OtherPropertyDisplayName")]
            public string OtherProperty { get; set; }
        }

        private class PropertyNameModel
        {
            public string MyProperty { get; set; }

            public string OtherProperty { get; set; }
        }
    }
}