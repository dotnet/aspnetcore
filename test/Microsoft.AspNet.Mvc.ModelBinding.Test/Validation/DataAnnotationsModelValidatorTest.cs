// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
#if NET45
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
            var metadata = _metadataProvider.GetMetadataForProperty(() => 15, typeof(string), "Length");
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
                    _metadataProvider.GetMetadataForProperty(() => 15, typeof(string), "Length"),
                    "Length"
                };

                yield return new object[]
                {
                    _metadataProvider.GetMetadataForType(() => new object(), typeof(SampleModel)),
                    "SampleModel"
                };
            }
        }

#if NET45
        [Theory]
        [MemberData(nameof(ValidateSetsMemberNamePropertyDataSet))]
        public void ValidateSetsMemberNamePropertyOfValidationContextForProperties(ModelMetadata metadata,
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
            var validationContext = CreateValidationContext(metadata);

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
            var metadata = _metadataProvider.GetMetadataForProperty(() => 15, typeof(string), "Length");
            var attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Setup(a => a.IsValid(metadata.Model)).Returns(true);
            var validator = new DataAnnotationsModelValidator(attribute.Object);
            var validationContext = CreateValidationContext(metadata);

            // Act
            var result = validator.Validate(validationContext);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ValidateWithIsValidFalse()
        {
            // Arrange
            var metadata = _metadataProvider.GetMetadataForProperty(() => 15, typeof(string), "Length");
            var attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Setup(a => a.IsValid(metadata.Model)).Returns(false);
            var validator = new DataAnnotationsModelValidator(attribute.Object);
            var validationContext = CreateValidationContext(metadata);

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
            var metadata = _metadataProvider.GetMetadataForProperty(() => 15, typeof(string), "Length");
            var attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Protected()
                     .Setup<ValidationResult>("IsValid", ItExpr.IsAny<object>(), ItExpr.IsAny<ValidationContext>())
                     .Returns(ValidationResult.Success);
            var validator = new DataAnnotationsModelValidator(attribute.Object);
            var validationContext = CreateValidationContext(metadata);

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
            var metadata = _metadataProvider.GetMetadataForProperty(() => 15, typeof(string), "Length");
            var attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Protected()
                     .Setup<ValidationResult>("IsValid", ItExpr.IsAny<object>(), ItExpr.IsAny<ValidationContext>())
                     .Returns(new ValidationResult(errorMessage, memberNames: null));
            var validator = new DataAnnotationsModelValidator(attribute.Object);
            var validationContext = CreateValidationContext(metadata);

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
            var metadata = _metadataProvider.GetMetadataForType(() => new object(), typeof(object));
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
            var metadata = _metadataProvider.GetMetadataForType(() => new SampleModel(), typeof(SampleModel));
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
        public void IsRequiredTests()
        {
            // Arrange
            var metadata = _metadataProvider.GetMetadataForProperty(() => 15, typeof(string), "Length");

            // Act & Assert
            Assert.False(new DataAnnotationsModelValidator(new RangeAttribute(10, 20)).IsRequired);
            Assert.True(new DataAnnotationsModelValidator(new RequiredAttribute()).IsRequired);
            Assert.True(new DataAnnotationsModelValidator(new DerivedRequiredAttribute()).IsRequired);
        }

        private static ModelValidationContext CreateValidationContext(ModelMetadata metadata)
        {
            return new ModelValidationContext(null, null, null, metadata, null);
        }

        private class DerivedRequiredAttribute : RequiredAttribute
        {
        }

        private class SampleModel
        {
            public string Name { get; set; }
        }
    }
}