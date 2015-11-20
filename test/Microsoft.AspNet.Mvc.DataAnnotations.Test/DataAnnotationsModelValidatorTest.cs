// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNet.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;
using Moq;
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
            var validator = new DataAnnotationsModelValidator(
                new ValidationAttributeAdapterProvider(),
                attribute,
                stringLocalizer: null);

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

        [Theory]
        [MemberData(nameof(Validate_SetsMemberName_OnValidationContext_ForProperties_Data))]
        public void Validate_SetsMemberName_OnValidationContext_ForProperties(
            ModelMetadata metadata,
            object container,
            object model,
            string expectedMemberName)
        {
            // Arrange
            var attribute = new Mock<TestableValidationAttribute> { CallBase = true };
            attribute
                .Setup(p => p.IsValidPublic(It.IsAny<object>(), It.IsAny<ValidationContext>()))
                .Callback((object o, ValidationContext context) =>
                {
                    Assert.Equal(expectedMemberName, context.MemberName);
                })
                .Returns(ValidationResult.Success)
                .Verifiable();
            var validator = new DataAnnotationsModelValidator(
                new ValidationAttributeAdapterProvider(),
                attribute.Object,
                stringLocalizer: null);
            var validationContext = new ModelValidationContext(
                actionContext: new ActionContext(),
                modelMetadata: metadata,
                metadataProvider: _metadataProvider,
                container: container,
                model: model);

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

            var validator = new DataAnnotationsModelValidator(
                new ValidationAttributeAdapterProvider(),
                attribute.Object,
                stringLocalizer: null);
            var validationContext = new ModelValidationContext(
                actionContext: new ActionContext(),
                modelMetadata: metadata,
                metadataProvider: _metadataProvider,
                container: container,
                model: model);

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

            var validator = new DataAnnotationsModelValidator(
                new ValidationAttributeAdapterProvider(),
                attribute.Object,
                stringLocalizer: null);
            var validationContext = new ModelValidationContext(
                actionContext: new ActionContext(),
                modelMetadata: metadata,
                metadataProvider: _metadataProvider,
                container: container,
                model: model);

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

            var attribute = new Mock<TestableValidationAttribute> { CallBase = true };
            attribute
                .Setup(p => p.IsValidPublic(It.IsAny<object>(), It.IsAny<ValidationContext>()))
                .Returns(ValidationResult.Success);
            var validator = new DataAnnotationsModelValidator(
                new ValidationAttributeAdapterProvider(),
                attribute.Object,
                stringLocalizer: null);
            var validationContext = new ModelValidationContext(
                actionContext: new ActionContext(),
                modelMetadata: metadata,
                metadataProvider: _metadataProvider,
                container: container,
                model: model);

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

            var attribute = new Mock<TestableValidationAttribute> { CallBase = true };
            attribute
                 .Setup(p => p.IsValidPublic(It.IsAny<object>(), It.IsAny<ValidationContext>()))
                 .Returns(new ValidationResult(errorMessage, memberNames: null));
            var validator = new DataAnnotationsModelValidator(
                new ValidationAttributeAdapterProvider(),
                attribute.Object,
                stringLocalizer: null);

            var validationContext = new ModelValidationContext(
                actionContext: new ActionContext(),
                modelMetadata: metadata,
                metadataProvider: _metadataProvider,
                container: container,
                model: model);

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

            var attribute = new Mock<TestableValidationAttribute> { CallBase = true };
            attribute
                 .Setup(p => p.IsValidPublic(It.IsAny<object>(), It.IsAny<ValidationContext>()))
                 .Returns(new ValidationResult(errorMessage, new[] { "FirstName" }));

            var validator = new DataAnnotationsModelValidator(
                new ValidationAttributeAdapterProvider(),
                attribute.Object,
                stringLocalizer: null);
            var validationContext = new ModelValidationContext(
                actionContext: new ActionContext(),
                modelMetadata: metadata,
                metadataProvider: _metadataProvider,
                container: null,
                model: model);

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

            var attribute = new Mock<TestableValidationAttribute> { CallBase = true };
            attribute
                 .Setup(p => p.IsValidPublic(It.IsAny<object>(), It.IsAny<ValidationContext>()))
                 .Returns(new ValidationResult("Name error", new[] { "Name" }));

            var validator = new DataAnnotationsModelValidator(
                new ValidationAttributeAdapterProvider(),
                attribute.Object,
                stringLocalizer: null);
            var validationContext = new ModelValidationContext(
                actionContext: new ActionContext(),
                modelMetadata: metadata,
                metadataProvider: _metadataProvider,
                container: null,
                model: model);

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

            var attribute = new MaxLengthAttribute(4);
            attribute.ErrorMessage = "{0} should have no more than {1} characters.";

            var localizedString = new LocalizedString(attribute.ErrorMessage, "Longueur est invalide : 4");
            var stringLocalizer = new Mock<IStringLocalizer>();
            stringLocalizer.Setup(s => s[attribute.ErrorMessage, It.IsAny<object[]>()]).Returns(localizedString);

            var validator = new DataAnnotationsModelValidator(
                new ValidationAttributeAdapterProvider(),
                attribute,
                stringLocalizer.Object);
            var validationContext = new ModelValidationContext(
                actionContext: new ActionContext(),
                modelMetadata: metadata,
                metadataProvider: _metadataProvider,
                container: container,
                model: "abcde");

            // Act
            var result = validator.Validate(validationContext);

            // Assert
            var validationResult = result.Single();
            Assert.Equal("", validationResult.MemberName);
            Assert.Equal("Longueur est invalide : 4", validationResult.Message);
        }

        private const string LocalizationKey = "LocalizeIt";

        public static TheoryData Validate_AttributesIncludeValues
        {
            get
            {
                var pattern = "apattern";
                var length = 5;
                var regex = "^((?!" + pattern + ").)*$";

                return new TheoryData<ValidationAttribute, string, object[]>
                {
                    {
                        new RegularExpressionAttribute(regex) { ErrorMessage = LocalizationKey },
                        pattern,
                        new object[] { nameof(SampleModel), regex }
                    },
                    {
                        new MaxLengthAttribute(length) { ErrorMessage = LocalizationKey },
                        pattern,
                        new object[] { nameof(SampleModel), length }},
                    {
                        new MaxLengthAttribute(length) { ErrorMessage = LocalizationKey },
                        pattern,
                        new object[] { nameof(SampleModel), length }
                    },
                    {
                        new CompareAttribute(pattern) { ErrorMessage = LocalizationKey },
                        pattern,
                        new object[] { nameof(SampleModel), pattern }},
                    {
                        new MinLengthAttribute(length) { ErrorMessage = LocalizationKey },
                        "a",
                        new object[] { nameof(SampleModel), length }
                    },
                    {
                        new CreditCardAttribute() { ErrorMessage = LocalizationKey },
                        pattern,
                        new object[] { nameof(SampleModel), "CreditCard" }
                    },
                    {
                        new StringLengthAttribute(length) { ErrorMessage = LocalizationKey, MinimumLength = 1},
                        "",
                        new object[] { nameof(SampleModel), 1, length }
                    },
                    {
                        new RangeAttribute(0, length) { ErrorMessage = LocalizationKey },
                        pattern,
                        new object[] { nameof(SampleModel), 0, length}
                    },
                    {
                        new EmailAddressAttribute() { ErrorMessage = LocalizationKey },
                        pattern,
                        new object[] { nameof(SampleModel), "EmailAddress" }
                    },
                    {
                        new PhoneAttribute() { ErrorMessage = LocalizationKey },
                        pattern,
                        new object[] { nameof(SampleModel), "PhoneNumber" }
                    },
                    {
                        new UrlAttribute() { ErrorMessage = LocalizationKey },
                        pattern,
                        new object[] { nameof(SampleModel), "Url"  }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(Validate_AttributesIncludeValues))]
        public void Validate_IsValidFalse_StringLocalizerGetsArguments(
            ValidationAttribute attribute,
            string model,
            object[] values)
        {
            // Arrange
            var stringLocalizer = new Mock<IStringLocalizer>();

            var validator = new DataAnnotationsModelValidator(
                new ValidationAttributeAdapterProvider(),
                attribute,
                stringLocalizer.Object);

            var metadata = _metadataProvider.GetMetadataForType(typeof(SampleModel));
            var validationContext = new ModelValidationContext(
                actionContext: new ActionContext(),
                modelMetadata: metadata,
                metadataProvider: _metadataProvider,
                container: null,
                model: model);

            // Act
            validator.Validate(validationContext);

            // Assert
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(values) + " " + attribute.GetType().Name;

            stringLocalizer.Verify(l => l[LocalizationKey, values], json);
        }

        public abstract class TestableValidationAttribute : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                return IsValidPublic(value, validationContext);
            }

            public abstract ValidationResult IsValidPublic(object value, ValidationContext validationContext);
        }

        private class SampleModel
        {
            public string Name { get; set; }
        }
    }
}
