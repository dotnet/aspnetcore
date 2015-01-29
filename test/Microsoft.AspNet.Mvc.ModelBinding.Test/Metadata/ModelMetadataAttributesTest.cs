// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelMetadataAttributesTest
    {
        [Fact]
        public void GetAttributesForBaseProperty_IncludesMetadataAttributes()
        {
            // Arrange
            var modelType = typeof(BaseViewModel);
            var property = modelType.GetRuntimeProperties().FirstOrDefault(p => p.Name == "BaseProperty");

            // Act
            var attributes = ModelAttributes.GetAttributesForProperty(modelType, property);

            // Assert
            Assert.Single(attributes.OfType<RequiredAttribute>());
            Assert.Single(attributes.OfType<StringLengthAttribute>());
        }

        [Fact]
        public void GetAttributesForTestProperty_ModelOverridesMetadataAttributes()
        {
            // Arrange
            var modelType = typeof(BaseViewModel);
            var property = modelType.GetRuntimeProperties().FirstOrDefault(p => p.Name == "TestProperty");

            // Act
            var attributes = ModelAttributes.GetAttributesForProperty(modelType, property);

            // Assert
            var rangeAttribute = attributes.OfType<RangeAttribute>().FirstOrDefault();
            Assert.NotNull(rangeAttribute);
            Assert.Equal(0, (int)rangeAttribute.Minimum);
            Assert.Equal(10, (int)rangeAttribute.Maximum);
            Assert.Single(attributes.OfType<FromHeaderAttribute>());
        }

        [Fact]
        public void GetAttributesForBasePropertyFromDerivedModel_IncludesMetadataAttributes()
        {
            // Arrange
            var modelType = typeof(DerivedViewModel);
            var property = modelType.GetRuntimeProperties().FirstOrDefault(p => p.Name == "BaseProperty");

            // Act
            var attributes = ModelAttributes.GetAttributesForProperty(modelType, property);

            // Assert
            Assert.Single(attributes.OfType<RequiredAttribute>());
            Assert.Single(attributes.OfType<StringLengthAttribute>());
        }

        [Fact]
        public void GetAttributesForTestPropertyFromDerived_IncludesMetadataAttributes()
        {
            // Arrange
            var modelType = typeof(DerivedViewModel);
            var property = modelType.GetRuntimeProperties().FirstOrDefault(p => p.Name == "TestProperty");

            // Act
            var attributes = ModelAttributes.GetAttributesForProperty(modelType, property);

            // Assert
            Assert.Single(attributes.OfType<RequiredAttribute>());
            Assert.Single(attributes.OfType<StringLengthAttribute>());
            Assert.DoesNotContain(typeof(RangeAttribute), attributes);
        }

        [Fact]
        public void GetAttributesForVirtualPropertyFromDerived_IncludesMetadataAttributes()
        {
            // Arrange
            var modelType = typeof(DerivedViewModel);
            var property = modelType.GetRuntimeProperties().FirstOrDefault(p => p.Name == "VirtualProperty");

            // Act
            var attributes = ModelAttributes.GetAttributesForProperty(modelType, property);

            // Assert
            Assert.Single(attributes.OfType<RequiredAttribute>());
            Assert.Single(attributes.OfType<RangeAttribute>());
        }

        [Fact]
        public void GetFromServiceAttributeFromBase_IncludesMetadataAttributes()
        {
            // Arrange
            var modelType = typeof(DerivedViewModel);
            var property = modelType.GetRuntimeProperties().FirstOrDefault(p => p.Name == "Calculator");

            // Act
            var attributes = ModelAttributes.GetAttributesForProperty(modelType, property);

            // Assert
            Assert.Single(attributes.OfType<RequiredAttribute>());
            Assert.Single(attributes.OfType<FromServicesAttribute>());
        }

        [Fact]
        public void GetAttributesForType_IncludesMetadataAttributes()
        {
            // Arrange & Act
            var attributes = ModelAttributes.GetAttributesForType(typeof(BaseViewModel));

            // Assert
            Assert.Single(attributes.OfType<ClassValidator>());
        }

        // Helper classes

        [ClassValidator]
        private class BaseModel
        {
            [StringLength(10)]
            public string BaseProperty { get; set; }

            [Range(10,100)]
            [FromHeader]
            public string TestProperty { get; set; }

            [Required]
            public virtual int VirtualProperty { get; set; }

            [FromServices]
            public ICalculator Calculator { get; set; }
        }

        private class DerivedModel : BaseModel
        {
            [Required]
            public string DerivedProperty { get; set; }

            [Required]
            public new string TestProperty { get; set; }

            [Range(10,100)]
            public override int VirtualProperty { get; set; }
            
        }

        [ModelMetadataType(typeof(BaseModel))]
        private class BaseViewModel
        {
            [Range(0,10)]
            public string TestProperty { get; set; }

            [Required]
            public string BaseProperty { get; set; }

            [Required]
            public ICalculator Calculator { get; set; }
        }

        [ModelMetadataType(typeof(DerivedModel))]
        private class DerivedViewModel : BaseViewModel
        {
            [StringLength(2)]
            public new string TestProperty { get; set; }

            public int VirtualProperty { get; set; }

        }

        public interface ICalculator
        {
            int Operation(char @operator, int left, int right);
        }

        private class ClassValidator : ValidationAttribute
        {
            public override Boolean IsValid(Object value)
            {
                return true;
            }
        }
    }
}