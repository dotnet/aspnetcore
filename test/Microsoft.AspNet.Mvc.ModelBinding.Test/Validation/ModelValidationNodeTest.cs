// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNet.Testing;
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

        // Validation order is primarily important when MaxAllowedErrors has been overridden.
        [Fact]
        public void Validate_OrdersUsingModelMetadata()
        {
            // Proper order of invocation:
            // 1. OnValidating()
            // 2. Child validators -- ordered using ModelMetadata.Order.
            // 3. OnValidated()

            // Arrange
            var expected = new[]
            {
                "In OnValidating()",
                "In LoggingValidatonAttribute.IsValid(OrderedProperty3)",
                "In LoggingValidatonAttribute.IsValid(OrderedProperty2)",
                "In LoggingValidatonAttribute.IsValid(OrderedProperty1)",
                "In LoggingValidatonAttribute.IsValid(Property3)",
                "In LoggingValidatonAttribute.IsValid(Property1)",
                "In LoggingValidatonAttribute.IsValid(Property2)",
                "In LoggingValidatonAttribute.IsValid(LastProperty)",
                "In OnValidated()"
            };

            var log = new List<string>();
            var model = new LoggingNonValidatableObject(log);
            var provider = new DataAnnotationsModelMetadataProvider();
            var modelMetadata = provider.GetMetadataForType(() => model, model.GetType());
            var node = new ModelValidationNode(modelMetadata, "theKey")
            {
                ValidateAllProperties = true,
            };
            node.Validating += (sender, e) => log.Add("In OnValidating()");
            node.Validated += (sender, e) => log.Add("In OnValidated()");
            var context = CreateContext(modelMetadata, provider);

            // Act
            node.Validate(context);

            // Assert
            Assert.Equal(expected, log);
        }

        [Fact]
        public void Validate_ChildNodes_OverridesOrdering()
        {
            // Proper order of invocation:
            // 1. OnValidating()
            // 2. Child validators -- ordered using ChildNodes, then ModelMetadata.Order.
            // 3. OnValidated()

            // Arrange
            var expected = new[]
            {
                "In OnValidating()",
                "In LoggingValidatonAttribute.IsValid(LastProperty)",
                "In LoggingValidatonAttribute.IsValid(OrderedProperty3)",
                "In LoggingValidatonAttribute.IsValid(OrderedProperty2)",
                "In LoggingValidatonAttribute.IsValid(OrderedProperty1)",
                "In LoggingValidatonAttribute.IsValid(Property3)",
                "In LoggingValidatonAttribute.IsValid(Property1)",
                "In LoggingValidatonAttribute.IsValid(Property2)",
                "In OnValidated()"
            };

            var log = new List<string>();
            var model = new LoggingNonValidatableObject(log);
            var provider = new DataAnnotationsModelMetadataProvider();
            var modelMetadata = provider.GetMetadataForType(() => model, model.GetType());
            var childMetadata = modelMetadata.Properties.FirstOrDefault(
                property => property.PropertyName == "LastProperty");
            Assert.NotNull(childMetadata);  // Guard

            var node = new ModelValidationNode(modelMetadata, "theKey")
            {
                ChildNodes =
                {
                    new ModelValidationNode(childMetadata, "theKey.LastProperty")
                },
                ValidateAllProperties = true,
            };
            node.Validating += (sender, e) => log.Add("In OnValidating()");
            node.Validated += (sender, e) => log.Add("In OnValidated()");
            var context = CreateContext(modelMetadata, provider);

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
        public void Validate_ValidatesIfModelIsNull()
        {
            // Arrange
            var modelMetadata = GetModelMetadata(typeof(ValidateAllPropertiesModel));
            var node = new ModelValidationNode(modelMetadata, "theKey");

            var context = CreateContext(modelMetadata);

            // Act
            node.Validate(context);

            // Assert
            var modelState = Assert.Single(context.ModelState);
            Assert.Equal("theKey", modelState.Key);
            Assert.Equal(ModelValidationState.Invalid, modelState.Value.ValidationState);

            var error = Assert.Single(modelState.Value.Errors);
            Assert.Equal("A value is required but was not present in the request.", error.ErrorMessage);
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
            var modelState = context.ModelState["theKey.RequiredString.Dummy"];
            Assert.NotNull(modelState);
            var error = Assert.Single(modelState.Errors);
            Assert.Equal("existing Error Text", error.ErrorMessage);

            modelState = context.ModelState["theKey.RangedInt"];
            Assert.NotNull(modelState);
            error = Assert.Single(modelState.Errors);
            Assert.Equal("The field RangedInt must be between 10 and 30.", error.ErrorMessage);

            Assert.DoesNotContain("theKey.RequiredString", context.ModelState.Keys);
            Assert.DoesNotContain("theKey.ValidString", context.ModelState.Keys);
            Assert.DoesNotContain("theKey", context.ModelState.Keys);
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
            var expectedMessage = ValidationAttributeUtil.GetRequiredErrorMessage("RequiredString");

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
            var modelState = context.ModelState[string.Empty];
            Assert.NotNull(modelState);
            var error = Assert.Single(modelState.Errors);
            Assert.IsType<TooManyModelErrorsException>(error.Exception);

            // RequiredString is validated first due to ModelMetadata.Properties ordering (Reflection-based).
            modelState = context.ModelState["theKey.RequiredString"];
            Assert.NotNull(modelState);
            error = Assert.Single(modelState.Errors);
            Assert.Equal(expectedMessage, error.ErrorMessage);

            // No room for the other validation errors.
            Assert.DoesNotContain("theKey.RangedInt", context.ModelState.Keys);
            Assert.DoesNotContain("theKey.ValidString", context.ModelState.Keys);
        }

        private static ModelMetadata GetModelMetadata()
        {
            return new EmptyModelMetadataProvider().GetMetadataForType(null, typeof(object));
        }

        private static ModelMetadata GetModelMetadata(object model)
        {
            return new DataAnnotationsModelMetadataProvider().GetMetadataForType(() => model, model.GetType());
        }

        private static ModelMetadata GetModelMetadata(Type type)
        {
            return new DataAnnotationsModelMetadataProvider().GetMetadataForType(modelAccessor: null, modelType: type);
        }

        private static ModelValidationContext CreateContext(
            ModelMetadata metadata = null,
            IModelMetadataProvider metadataProvider = null)
        {
            var providers = new IModelValidatorProvider[]
            {
                new DataAnnotationsModelValidatorProvider(),
                new DataMemberModelValidatorProvider()
            };
            if (metadataProvider == null)
            {
                metadataProvider = new EmptyModelMetadataProvider();
            }

            return new ModelValidationContext(metadataProvider,
                                              new CompositeModelValidatorProvider(providers),
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
                Assert.Null(validationContext.MemberName);
                _log.Add("In IValidatableObject.Validate()");
                yield return new ValidationResult("Sample error message", new[] { "InvalidStringProperty" });
            }

            private sealed class LoggingValidationAttribute : ValidationAttribute
            {
                protected override ValidationResult IsValid(object value, ValidationContext validationContext)
                {
                    var validatableObject = Assert.IsType<LoggingValidatableObject>(value);
                    Assert.NotNull(validationContext);
                    Assert.Equal("ValidStringProperty", validationContext.MemberName);
                    validatableObject._log.Add("In LoggingValidatonAttribute.IsValid()");
                    return ValidationResult.Success;
                }
            }
        }

        private sealed class LoggingNonValidatableObject
        {
            private readonly IList<string> _log;

            public LoggingNonValidatableObject(IList<string> log)
            {
                _log = log;
            }

            [LoggingValidation]
            [Display(Order = 10001)]
            public string LastProperty { get; set; }

            [LoggingValidation]
            public string Property3 { get; set; }
            [LoggingValidation]
            public string Property1 { get; set; }
            [LoggingValidation]
            public string Property2 { get; set; }

            [LoggingValidation]
            [Display(Order = 23)]
            public string OrderedProperty3 { get; set; }
            [LoggingValidation]
            [Display(Order = 23)]
            public string OrderedProperty2 { get; set; }
            [LoggingValidation]
            [Display(Order = 23)]
            public string OrderedProperty1 { get; set; }

            private sealed class LoggingValidationAttribute : ValidationAttribute
            {
                protected override ValidationResult IsValid(object value, ValidationContext validationContext)
                {
                    Assert.Null(value);
                    Assert.NotNull(validationContext);
                    var nonValidatableObject =
                        Assert.IsType<LoggingNonValidatableObject>(validationContext.ObjectInstance);

                    nonValidatableObject._log.Add(
                        string.Format("In LoggingValidatonAttribute.IsValid({0})", validationContext.MemberName));
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
