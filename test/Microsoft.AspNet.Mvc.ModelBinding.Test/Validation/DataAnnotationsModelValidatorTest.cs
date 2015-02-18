// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
#if ASPNET50
using System.Linq;
#endif
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
#if ASPNET50
using Moq;
using Moq.Protected;
#endif
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class DataAnnotationsModelValidatorTest
    {
        private static DataAnnotationsModelMetadataProvider _metadataProvider =
            new DataAnnotationsModelMetadataProvider();

        [Fact]
        public void ValuesSet()
        {
            // Arrange
            var attribute = new RequiredAttribute();

            // Act
            var validator = new DataAnnotationsModelValidator(attribute);

            // Assert
            Assert.Same(attribute, validator.Attribute);
        }

        public static IEnumerable<object[]> ValidateSetsMemberNamePropertyDataSet
        {
            get
            {
                yield return new object[]
                {
                    _metadataProvider.GetModelExplorerForType(typeof(string), "Hello").GetExplorerForProperty("Length"),
                    "Length"
                };

                yield return new object[]
                {
                    _metadataProvider.GetModelExplorerForType(typeof(SampleModel), 15),
                    "SampleModel"
                };
            }
        }

#if ASPNET50
        [Theory]
        [MemberData(nameof(ValidateSetsMemberNamePropertyDataSet))]
        public void ValidateSetsMemberNamePropertyOfValidationContextForProperties(
            ModelExplorer modelExplorer,
            string expectedMemberName)
        {
            // Arrange
            var attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Protected()
                     .Setup<ValidationResult>("IsValid", ItExpr.IsAny<object>(), ItExpr.IsAny<ValidationContext>())
                     .Callback((object o, ValidationContext context) =>
                     {
                         Assert.Equal(expectedMemberName, context.MemberName);
                     })
                     .Returns(ValidationResult.Success)
                     .Verifiable();
            var validator = new DataAnnotationsModelValidator(attribute.Object);
            var validationContext = CreateValidationContext(modelExplorer);

            // Act
            var results = validator.Validate(validationContext);

            // Assert
            Assert.Empty(results);
            attribute.VerifyAll();
        }

        [Fact]
        public void ValidateWithIsValidTrue()
        {
            // Arrange
            var modelExplorer = _metadataProvider
                .GetModelExplorerForType(typeof(string), "Hello")
                .GetExplorerForProperty("Length");

            var attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Setup(a => a.IsValid(modelExplorer.Model)).Returns(true);

            var validator = new DataAnnotationsModelValidator(attribute.Object);
            var validationContext = CreateValidationContext(modelExplorer);

            // Act
            var result = validator.Validate(validationContext);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ValidateWithIsValidFalse()
        {
            // Arrange
            var modelExplorer = _metadataProvider
                .GetModelExplorerForType(typeof(string), "Hello")
                .GetExplorerForProperty("Length");

            var attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Setup(a => a.IsValid(modelExplorer.Model)).Returns(false);

            var validator = new DataAnnotationsModelValidator(attribute.Object);
            var validationContext = CreateValidationContext(modelExplorer);

            // Act
            var result = validator.Validate(validationContext);

            // Assert
            var validationResult = result.Single();
            Assert.Equal("", validationResult.MemberName);
            Assert.Equal(attribute.Object.FormatErrorMessage("Length"), validationResult.Message);
        }

        [Fact]
        public void ValidatateWithValidationResultSuccess()
        {
            // Arrange
            var modelExplorer = _metadataProvider
                .GetModelExplorerForType(typeof(string), "Hello")
                .GetExplorerForProperty("Length");

            var attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Protected()
                     .Setup<ValidationResult>("IsValid", ItExpr.IsAny<object>(), ItExpr.IsAny<ValidationContext>())
                     .Returns(ValidationResult.Success);
            var validator = new DataAnnotationsModelValidator(attribute.Object);
            var validationContext = CreateValidationContext(modelExplorer);

            // Act
            var result = validator.Validate(validationContext);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ValidateReturnsSingleValidationResultIfMemberNameSequenceIsEmpty()
        {
            // Arrange
            const string errorMessage = "Some error message";

            var modelExplorer = _metadataProvider
                .GetModelExplorerForType(typeof(string), "Hello")
                .GetExplorerForProperty("Length");

            var attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Protected()
                     .Setup<ValidationResult>("IsValid", ItExpr.IsAny<object>(), ItExpr.IsAny<ValidationContext>())
                     .Returns(new ValidationResult(errorMessage, memberNames: null));
            var validator = new DataAnnotationsModelValidator(attribute.Object);

            var validationContext = CreateValidationContext(modelExplorer);

            // Act
            var results = validator.Validate(validationContext);

            // Assert
            ModelValidationResult validationResult = Assert.Single(results);
            Assert.Equal(errorMessage, validationResult.Message);
            Assert.Empty(validationResult.MemberName);
        }

        [Fact]
        public void ValidateReturnsSingleValidationResultIfOneMemberNameIsSpecified()
        {
            // Arrange
            const string errorMessage = "A different error message";

            var metadata = _metadataProvider.GetModelExplorerForType(typeof(object), new object());

            var attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Protected()
                     .Setup<ValidationResult>("IsValid", ItExpr.IsAny<object>(), ItExpr.IsAny<ValidationContext>())
                     .Returns(new ValidationResult(errorMessage, new[] { "FirstName" }));

            var validator = new DataAnnotationsModelValidator(attribute.Object);
            var validationContext = CreateValidationContext(metadata);

            // Act
            var results = validator.Validate(validationContext);

            // Assert
            ModelValidationResult validationResult = Assert.Single(results);
            Assert.Equal(errorMessage, validationResult.Message);
            Assert.Equal("FirstName", validationResult.MemberName);
        }

        [Fact]
        public void ValidateReturnsMemberNameIfItIsDifferentFromDisplayName()
        {
            // Arrange
            var metadata = _metadataProvider.GetModelExplorerForType(typeof(SampleModel), new SampleModel());

            var attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Protected()
                     .Setup<ValidationResult>("IsValid", ItExpr.IsAny<object>(), ItExpr.IsAny<ValidationContext>())
                     .Returns(new ValidationResult("Name error", new[] { "Name" }));

            var validator = new DataAnnotationsModelValidator(attribute.Object);
            var validationContext = CreateValidationContext(metadata);

            // Act
            var results = validator.Validate(validationContext);

            // Assert
            ModelValidationResult validationResult = Assert.Single(results);
            Assert.Equal("Name", validationResult.MemberName);
        }
#endif

        [Fact]
        public void GetClientValidationRules_ReturnsEmptyRuleSet()
        {
            // Arrange
            var attribute = new FileExtensionsAttribute();
            var validator = new DataAnnotationsModelValidator<FileExtensionsAttribute>(attribute);

            var metadata = _metadataProvider.GetMetadataForProperty(
                containerType: typeof(string),
                propertyName: nameof(string.Length));

            var serviceCollection = new ServiceCollection();
            var requestServices = serviceCollection.BuildServiceProvider();

            var context = new ClientModelValidationContext(metadata, _metadataProvider, requestServices);

            // Act
            var results = validator.GetClientValidationRules(context);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void GetClientValidationRules_WithIClientModelValidator_CallsAttribute()
        {
            // Arrange
            var attribute = new TestableAttribute();
            var validator = new DataAnnotationsModelValidator<TestableAttribute>(attribute);

            var metadata = _metadataProvider.GetMetadataForProperty(
                containerType: typeof(string),
                propertyName: nameof(string.Length));

            var serviceCollection = new ServiceCollection();
            var requestServices = serviceCollection.BuildServiceProvider();

            var context = new ClientModelValidationContext(metadata, _metadataProvider, requestServices);

            // Act
            var results = validator.GetClientValidationRules(context);

            // Assert
            var rule = Assert.Single(results);
            Assert.Equal("an error", rule.ErrorMessage);
            Assert.Empty(rule.ValidationParameters);
            Assert.Equal("testable", rule.ValidationType);
        }

        [Fact]
        public void IsRequiredTests()
        {
            // Arrange & Act & Assert
            Assert.False(new DataAnnotationsModelValidator(new RangeAttribute(10, 20)).IsRequired);
            Assert.True(new DataAnnotationsModelValidator(new RequiredAttribute()).IsRequired);
            Assert.True(new DataAnnotationsModelValidator(new DerivedRequiredAttribute()).IsRequired);
        }

        private static ModelValidationContext CreateValidationContext(ModelExplorer modelExplorer)
        {
            return new ModelValidationContext(null, null, null, modelExplorer);
        }

        private class DerivedRequiredAttribute : RequiredAttribute
        {
        }

        private class SampleModel
        {
            public string Name { get; set; }
        }

        private class TestableAttribute : ValidationAttribute, IClientModelValidator
        {
            public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ClientModelValidationContext context)
            {
                return new[] { new ModelClientValidationRule(validationType: "testable", errorMessage: "an error") };
            }
        }
    }
}
