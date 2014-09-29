// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelValidationNodeTest
    {
        [Fact]
        public void ConstructorSetsCollectionInstance()
        {
            // Arrange
            var metadata = GetModelMetadata();
            var modelStateKey = "someKey";
            var childNodes = new[]
            {
                new ModelValidationNode(metadata, "someKey0"),
                new ModelValidationNode(metadata, "someKey1")
            };

            // Act
            var node = new ModelValidationNode(metadata, modelStateKey, childNodes);

            // Assert
            Assert.Equal(childNodes, node.ChildNodes.ToArray());
        }

        [Fact]
        public void PropertiesAreSet()
        {
            // Arrange
            var metadata = GetModelMetadata();
            var modelStateKey = "someKey";

            // Act
            var node = new ModelValidationNode(metadata, modelStateKey);

            // Assert
            Assert.Equal(metadata, node.ModelMetadata);
            Assert.Equal(modelStateKey, node.ModelStateKey);
            Assert.NotNull(node.ChildNodes);
            Assert.Empty(node.ChildNodes);
        }

        [Fact]
        public void CombineWith()
        {
            // Arrange
            var expected = new[]
            {
                "Validating parent1.",
                "Validating parent2.",
                "Validated parent1.",
                "Validated parent2."
            };
            var log = new List<string>();

            var allChildNodes = new[]
            {
                new ModelValidationNode(GetModelMetadata(), "key1"),
                new ModelValidationNode(GetModelMetadata(), "key2"),
                new ModelValidationNode(GetModelMetadata(), "key3"),
            };

            var parentNode1 = new ModelValidationNode(GetModelMetadata(), "parent1");
            parentNode1.ChildNodes.Add(allChildNodes[0]);
            parentNode1.Validating += (sender, e) => log.Add("Validating parent1.");
            parentNode1.Validated += (sender, e) => log.Add("Validated parent1.");

            var parentNode2 = new ModelValidationNode(GetModelMetadata(), "parent2");
            parentNode2.ChildNodes.Add(allChildNodes[1]);
            parentNode2.ChildNodes.Add(allChildNodes[2]);
            parentNode2.Validating += (sender, e) => log.Add("Validating parent2.");
            parentNode2.Validated += (sender, e) => log.Add("Validated parent2.");
            var context = CreateContext();

            // Act
            parentNode1.CombineWith(parentNode2);
            parentNode1.Validate(context);

            // Assert
            Assert.Equal(expected, log);
            Assert.Equal(allChildNodes, parentNode1.ChildNodes.ToArray());
        }

        [Fact]
        public void CombineWith_OtherNodeIsSuppressed_DoesNothing()
        {
            // Arrange
            var log = new List<string>();

            var allChildNodes = new[]
            {
                new ModelValidationNode(GetModelMetadata(), "key1"),
                new ModelValidationNode(GetModelMetadata(), "key2"),
                new ModelValidationNode(GetModelMetadata(), "key3"),
            };

            var expectedChildNodes = new[]
            {
                allChildNodes[0]
            };

            var parentNode1 = new ModelValidationNode(GetModelMetadata(), "parent1");
            parentNode1.ChildNodes.Add(allChildNodes[0]);
            parentNode1.Validating += (sender, e) => log.Add("Validating parent1.");
            parentNode1.Validated += (sender, e) => log.Add("Validated parent1.");

            var parentNode2 = new ModelValidationNode(GetModelMetadata(), "parent2");
            parentNode2.ChildNodes.Add(allChildNodes[1]);
            parentNode2.ChildNodes.Add(allChildNodes[2]);
            parentNode2.Validating += (sender, e) => log.Add("Validating parent2.");
            parentNode2.Validated += (sender, e) => log.Add("Validated parent2.");
            parentNode2.SuppressValidation = true;
            var context = CreateContext();

            // Act
            parentNode1.CombineWith(parentNode2);
            parentNode1.Validate(context);

            // Assert
            Assert.Equal(new[] { "Validating parent1.", "Validated parent1." }, log.ToArray());
            Assert.Equal(expectedChildNodes, parentNode1.ChildNodes.ToArray());
        }

        [Fact]
        public void Validate_Ordering()
        {
            // Proper order of invocation:
            // 1. OnValidating()
            // 2. Child validators
            // 3. This validator
            // 4. OnValidated()

            // Arrange
            var expected = new[]
            {
                "In OnValidating()",
                "In LoggingValidatonAttribute.IsValid()",
                "In IValidatableObject.Validate()",
                "In OnValidated()"
            };
            var log = new List<string>();
            var model = new LoggingValidatableObject(log);
            var modelMetadata = GetModelMetadata(model);
            var provider = new EmptyModelMetadataProvider();
            var childMetadata = provider.GetMetadataForProperty(() => model, model.GetType(), "ValidStringProperty");
            var node = new ModelValidationNode(modelMetadata, "theKey");
            node.Validating += (sender, e) => log.Add("In OnValidating()");
            node.Validated += (sender, e) => log.Add("In OnValidated()");
            node.ChildNodes.Add(new ModelValidationNode(childMetadata, "theKey.ValidStringProperty"));
            var context = CreateContext(modelMetadata);

            // Act
            node.Validate(context);

            // Assert
            Assert.Equal(expected, log);
        }

        [Fact]
        public void Validate_SkipsRemainingValidationIfModelStateIsInvalid()
        {
            // Because a property validator fails, the model validator shouldn't run

            // Arrange
            var expected = new[]
            {
                "In OnValidating()",
                "In IValidatableObject.Validate()",
                "In OnValidated()"
            };
            var log = new List<string>();
            var model = new LoggingValidatableObject(log);
            var modelMetadata = GetModelMetadata(model);
            var provider = new EmptyModelMetadataProvider();
            var childMetadata = provider.GetMetadataForProperty(() => model,
                                                                model.GetType(),
                                                                "InvalidStringProperty");
            var node = new ModelValidationNode(modelMetadata, "theKey");
            node.ChildNodes.Add(new ModelValidationNode(childMetadata, "theKey.InvalidStringProperty"));
            node.Validating += (sender, e) => log.Add("In OnValidating()");
            node.Validated += (sender, e) => log.Add("In OnValidated()");
            var context = CreateContext(modelMetadata);

            // Act
            node.Validate(context);

            // Assert
            Assert.Equal(expected, log);
            Assert.Equal("Sample error message",
                         context.ModelState["theKey.InvalidStringProperty"].Errors[0].ErrorMessage);
        }

        [Fact]
        public void Validate_SkipsValidationIfHandlerCancels()
        {
            // Arrange
            var log = new List<string>();
            var model = new LoggingValidatableObject(log);
            var modelMetadata = GetModelMetadata(model);
            var node = new ModelValidationNode(modelMetadata, "theKey");
            node.Validating += (sender, e) =>
            {
                log.Add("In OnValidating()");
                e.Cancel = true;
            };
            node.Validated += (sender, e) => log.Add("In OnValidated()");
            var context = CreateContext(modelMetadata);

            // Act
            node.Validate(context);

            // Assert
            Assert.Equal(new[] { "In OnValidating()" }, log.ToArray());
        }

        [Fact]
        public void Validate_SkipsValidationIfSuppressed()
        {
            // Arrange
            var log = new List<string>();
            var model = new LoggingValidatableObject(log);
            var modelMetadata = GetModelMetadata(model);
            var node = new ModelValidationNode(modelMetadata, "theKey")
            {
                SuppressValidation = true
            };

            node.Validating += (sender, e) => log.Add("In OnValidating()");
            node.Validated += (sender, e) => log.Add("In OnValidated()");
            var context = CreateContext();

            // Act
            node.Validate(context);

            // Assert
            Assert.Empty(log);
        }

        [Fact]
        [ReplaceCulture]
        public void Validate_ValidateAllProperties_AddsValidationErrors()
        {
            // Arrange
            var model = new ValidateAllPropertiesModel
            {
                RequiredString = null /* error */,
                RangedInt = 0 /* error */,
                ValidString = "dog"
            };

            var modelMetadata = GetModelMetadata(model);
            var node = new ModelValidationNode(modelMetadata, "theKey")
            {
                ValidateAllProperties = true
            };
            var context = CreateContext(modelMetadata);
            context.ModelState.AddModelError("theKey.RequiredString.Dummy", "existing Error Text");

            // Act
            node.Validate(context);

            // Assert
            Assert.False(context.ModelState.ContainsKey("theKey.RequiredString"));
            Assert.Equal("existing Error Text",
                         context.ModelState["theKey.RequiredString.Dummy"].Errors[0].ErrorMessage);
            Assert.Equal("The field RangedInt must be between 10 and 30.",
                         context.ModelState["theKey.RangedInt"].Errors[0].ErrorMessage);
            Assert.False(context.ModelState.ContainsKey("theKey.ValidString"));
            Assert.False(context.ModelState.ContainsKey("theKey"));
        }

        [Fact]
        [ReplaceCulture]
        public void Validate_ShortCircuits_IfModelStateHasReachedMaxNumberOfErrors()
        {
            // Arrange
            var model = new ValidateAllPropertiesModel
            {
                RequiredString = null /* error */,
                RangedInt = 0 /* error */,
                ValidString = "cat"  /* error */
            };

            var modelMetadata = GetModelMetadata(model);
            var node = new ModelValidationNode(modelMetadata, "theKey")
            {
                ValidateAllProperties = true
            };
            var context = CreateContext(modelMetadata);
            context.ModelState.MaxAllowedErrors = 3;
            context.ModelState.AddModelError("somekey", "error text");

            // Act
            node.Validate(context);

            // Assert
            Assert.Equal(3, context.ModelState.Count);
            Assert.IsType<TooManyModelErrorsException>(context.ModelState[""].Errors[0].Exception);
            Assert.Equal(ValidationAttributeUtil.GetRequiredErrorMessage("RequiredString"), 
                        context.ModelState["theKey.RequiredString"].Errors[0].ErrorMessage);
            Assert.False(context.ModelState.ContainsKey("theKey.RangedInt"));
            Assert.False(context.ModelState.ContainsKey("theKey.ValidString"));
        }

        private static ModelMetadata GetModelMetadata()
        {
            return new EmptyModelMetadataProvider().GetMetadataForType(null, typeof(object));
        }

        private static ModelMetadata GetModelMetadata(object o)
        {
            return new DataAnnotationsModelMetadataProvider().GetMetadataForType(() => o, o.GetType());
        }

        private static ModelValidationContext CreateContext(ModelMetadata metadata = null)
        {
            var providers = new IModelValidatorProvider[]
            {
                new DataAnnotationsModelValidatorProvider(),
                new DataMemberModelValidatorProvider()
            };

            var provider = new Mock<IModelValidatorProviderProvider>();
            provider.SetupGet(p => p.ModelValidatorProviders)
                    .Returns(providers);

            return new ModelValidationContext(new EmptyModelMetadataProvider(),
                                              new CompositeModelValidatorProvider(provider.Object),
                                              new ModelStateDictionary(),
                                              metadata,
                                              null);
        }

        private sealed class LoggingValidatableObject : IValidatableObject
        {
            private readonly IList<string> _log;

            public LoggingValidatableObject(IList<string> log)
            {
                _log = log;
            }

            [LoggingValidation]
            public string ValidStringProperty { get; set; }
            public string InvalidStringProperty { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                _log.Add("In IValidatableObject.Validate()");
                yield return new ValidationResult("Sample error message", new[] { "InvalidStringProperty" });
            }

            private sealed class LoggingValidationAttribute : ValidationAttribute
            {
                protected override ValidationResult IsValid(object value, ValidationContext validationContext)
                {
                    LoggingValidatableObject lvo = (LoggingValidatableObject)value;
                    lvo._log.Add("In LoggingValidatonAttribute.IsValid()");
                    return ValidationResult.Success;
                }
            }
        }

        private class ValidateAllPropertiesModel
        {
            [Required]
            public string RequiredString { get; set; }

            [Range(10, 30)]
            public int RangedInt { get; set; }

            [RegularExpression("dog")]
            public string ValidString { get; set; }
        }
    }
}
#endif
