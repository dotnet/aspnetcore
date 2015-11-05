// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
#if DNX451
using System.Linq;
#endif
using Microsoft.Extensions.Localization;
#if DNX451
using Moq;
using Moq.Protected;
#endif
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class DataAnnotationsModelValidatorTest
    {
        private static IModelMetadataProvider _metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

        [Fact]
        public void Constructor_SetsAttribute()
        {
            // Arrange
            var attribute = new RequiredAttribute();

            // Act
            var validator = new DataAnnotationsModelValidator(attribute, stringLocalizer : null);

            // Assert
            Assert.Same(attribute, validator.Attribute);
        }

        public static IEnumerable<object[]> Validate_SetsMemberName_OnValidationContext_ForProperties_Data
        {
            get
            {
                yield return new object[]
                {
                    _metadataProvider.GetMetadataForType(typeof(string)).Properties["Length"],
                    "Hello",
                    "Hello".Length,
                    "Length",
                };

                yield return new object[]
                {
                    _metadataProvider.GetMetadataForType(typeof(SampleModel)),
                    null,
                    15,
                    "SampleModel",
                };
            }
        }

#if DNX451
        [Theory]
        [MemberData(nameof(Validate_SetsMemberName_OnValidationContext_ForProperties_Data))]
        public void Validate_SetsMemberName_OnValidationContext_ForProperties(
            ModelMetadata metadata,
            object container,
            object model,
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
            var validator = new DataAnnotationsModelValidator(attribute.Object, stringLocalizer: null);
            var validationContext = new ModelValidationContext()
            {
                Metadata = metadata,
                Container = container,
                Model = model,
            };

            // Act
            var results = validator.Validate(validationContext);

            // Assert
            Assert.Empty(results);
            attribute.VerifyAll();
        }

        [Fact]
        public void Validate_Valid()
        {
            // Arrange
            var metadata = _metadataProvider.GetMetadataForType(typeof(string));
            var container = "Hello";
            var model = container.Length;

            var attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Setup(a => a.IsValid(model)).Returns(true);

            var validator = new DataAnnotationsModelValidator(attribute.Object, stringLocalizer: null);
            var validationContext = new ModelValidationContext()
            {
                Metadata = metadata,
                Container = container,
                Model = model,
            };

            // Act
            var result = validator.Validate(validationContext);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Validate_Invalid()
        {
            // Arrange
            var metadata = _metadataProvider.GetMetadataForProperty(typeof(string), "Length");
            var container = "Hello";
            var model = container.Length;

            var attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Setup(a => a.IsValid(model)).Returns(false);

            var validator = new DataAnnotationsModelValidator(attribute.Object, stringLocalizer: null);
            var validationContext = new ModelValidationContext()
            {
                Metadata = metadata,
                Container = container,
                Model = model,
            };

            // Act
            var result = validator.Validate(validationContext);

            // Assert
            var validationResult = result.Single();
            Assert.Equal("", validationResult.MemberName);
            Assert.Equal(attribute.Object.FormatErrorMessage("Length"), validationResult.Message);
        }

        [Fact]
        public void Validatate_ValidationResultSuccess()
        {
            // Arrange
            var metadata = _metadataProvider.GetMetadataForType(typeof(string));
            var container = "Hello";
            var model = container.Length;

            var attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Protected()
                     .Setup<ValidationResult>("IsValid", ItExpr.IsAny<object>(), ItExpr.IsAny<ValidationContext>())
                     .Returns(ValidationResult.Success);
            var validator = new DataAnnotationsModelValidator(attribute.Object, stringLocalizer: null);
            var validationContext = new ModelValidationContext()
            {
                Metadata = metadata,
                Container = container,
                Model = model,
            };

            // Act
            var result = validator.Validate(validationContext);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Validate_ReturnsSingleValidationResult_IfMemberNameSequenceIsEmpty()
        {
            // Arrange
            const string errorMessage = "Some error message";

            var metadata = _metadataProvider.GetMetadataForType(typeof(string));
            var container = "Hello";
            var model = container.Length;

            var attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Protected()
                     .Setup<ValidationResult>("IsValid", ItExpr.IsAny<object>(), ItExpr.IsAny<ValidationContext>())
                     .Returns(new ValidationResult(errorMessage, memberNames: null));
            var validator = new DataAnnotationsModelValidator(attribute.Object, stringLocalizer: null);

            var validationContext = new ModelValidationContext()
            {
                Metadata = metadata,
                Container = container,
                Model = model,
            };

            // Act
            var results = validator.Validate(validationContext);

            // Assert
            var validationResult = Assert.Single(results);
            Assert.Equal(errorMessage, validationResult.Message);
            Assert.Empty(validationResult.MemberName);
        }

        [Fact]
        public void Validate_ReturnsSingleValidationResult_IfOneMemberNameIsSpecified()
        {
            // Arrange
            const string errorMessage = "A different error message";

            var metadata = _metadataProvider.GetMetadataForType(typeof(object));
            var model = new object();

            var attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Protected()
                     .Setup<ValidationResult>("IsValid", ItExpr.IsAny<object>(), ItExpr.IsAny<ValidationContext>())
                     .Returns(new ValidationResult(errorMessage, new[] { "FirstName" }));

            var validator = new DataAnnotationsModelValidator(attribute.Object, stringLocalizer: null);
            var validationContext = new ModelValidationContext()
            {
                Metadata = metadata,
                Model = model,
            };

            // Act
            var results = validator.Validate(validationContext);

            // Assert
            ModelValidationResult validationResult = Assert.Single(results);
            Assert.Equal(errorMessage, validationResult.Message);
            Assert.Equal("FirstName", validationResult.MemberName);
        }

        [Fact]
        public void Validate_ReturnsMemberName_IfItIsDifferentFromDisplayName()
        {
            // Arrange
            var metadata = _metadataProvider.GetMetadataForType(typeof(SampleModel));
            var model = new SampleModel();

            var attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Protected()
                     .Setup<ValidationResult>("IsValid", ItExpr.IsAny<object>(), ItExpr.IsAny<ValidationContext>())
                     .Returns(new ValidationResult("Name error", new[] { "Name" }));

            var validator = new DataAnnotationsModelValidator(attribute.Object, stringLocalizer: null);
            var validationContext = new ModelValidationContext()
            {
                Metadata = metadata,
                Model = model,
            };

            // Act
            var results = validator.Validate(validationContext);

            // Assert
            ModelValidationResult validationResult = Assert.Single(results);
            Assert.Equal("Name", validationResult.MemberName);
        }

        [Fact]
        public void Validate_IsValidFalse_StringLocalizerReturnsLocalizerErrorMessage()
        {
            // Arrange
            var metadata = _metadataProvider.GetMetadataForType(typeof(string));
            var container = "Hello";
            var model = container.Length;

            var attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Setup(a => a.IsValid(model)).Returns(false);

            attribute.Object.ErrorMessage = "Length";

            var localizedString = new LocalizedString("Length", "Longueur est invalide");
            var stringLocalizer = new Mock<IStringLocalizer>();
            stringLocalizer.Setup(s => s["Length"]).Returns(localizedString);

            var validator = new DataAnnotationsModelValidator(attribute.Object, stringLocalizer.Object);
            var validationContext = new ModelValidationContext()
            {
                Metadata = metadata,
                Container = container,
                Model = model,
            };

            // Act
            var result = validator.Validate(validationContext);

            // Assert
            var validationResult = result.Single();
            Assert.Equal("", validationResult.MemberName);
            Assert.Equal("Longueur est invalide", validationResult.Message);
        }
#endif

        private class DerivedRequiredAttribute : RequiredAttribute
        {
        }

        private class SampleModel
        {
            public string Name { get; set; }
        }
    }
}
