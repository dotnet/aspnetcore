// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations.Internal;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class ParameterBinderTest
    {
        public static TheoryData BindModelAsyncData
        {
            get
            {
                var emptyBindingInfo = new BindingInfo();
                var bindingInfoWithName = new BindingInfo
                {
                    BinderModelName = "bindingInfoName",
                    BinderType = typeof(Person),
                };

                // parameterBindingInfo, metadataBinderModelName, parameterName, expectedBinderModelName
                return new TheoryData<BindingInfo, string, string, string>
                {
                    // If the parameter name is not a prefix match, it is ignored. But name is required to create a
                    // ModelBindingContext.
                    { null, null, "parameterName", string.Empty },
                    { emptyBindingInfo, null, "parameterName", string.Empty },
                    { bindingInfoWithName, null, "parameterName", "bindingInfoName" },
                    { null, "modelBinderName", "parameterName", "modelBinderName" },
                    { null, null, "parameterName", string.Empty },
                    // Parameter's BindingInfo has highest precedence
                    { bindingInfoWithName, "modelBinderName", "parameterName", "bindingInfoName" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(BindModelAsyncData))]
        public async Task BindModelAsync_PassesExpectedBindingInfoAndMetadata_IfPrefixDoesNotMatch(
            BindingInfo parameterBindingInfo,
            string metadataBinderModelName,
            string parameterName,
            string expectedModelName)
        {
            // Arrange
            var binderExecuted = false;
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForType<Person>().BindingDetails(binding =>
            {
                binding.BinderModelName = metadataBinderModelName;
            });

            var metadata = metadataProvider.GetMetadataForType(typeof(Person));
            var modelBinder = new Mock<IModelBinder>();
            modelBinder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Callback((ModelBindingContext context) =>
                {
                    Assert.Equal(expectedModelName, context.ModelName, StringComparer.Ordinal);
                })
                .Returns(Task.CompletedTask);

            var parameterDescriptor = new ParameterDescriptor
            {
                BindingInfo = parameterBindingInfo,
                Name = parameterName,
                ParameterType = typeof(Person),
            };

            var factory = new Mock<IModelBinderFactory>(MockBehavior.Strict);
            factory
                .Setup(f => f.CreateBinder(It.IsAny<ModelBinderFactoryContext>()))
                .Callback((ModelBinderFactoryContext context) =>
                {
                    binderExecuted = true;
                    // Confirm expected data is passed through to ModelBindingFactory.
                    Assert.Same(parameterDescriptor.BindingInfo, context.BindingInfo);
                    Assert.Same(parameterDescriptor, context.CacheToken);
                    Assert.Equal(metadata, context.Metadata);
                })
                .Returns(modelBinder.Object);

            var parameterBinder = new ParameterBinder(
                metadataProvider,
                factory.Object,
                CreateMockValidatorProvider());

            var controllerContext = new ControllerContext();

            // Act & Assert
            await parameterBinder.BindModelAsync(controllerContext, new SimpleValueProvider(), parameterDescriptor);
            Assert.True(binderExecuted);

        }

        [Fact]
        public async Task BindModelAsync_PassesExpectedBindingInfoAndMetadata_IfPrefixMatches()
        {
            // Arrange
            var expectedModelName = "expectedName";
            var binderExecuted = false;

            var metadataProvider = new TestModelMetadataProvider();
            var metadata = metadataProvider.GetMetadataForType(typeof(Person));
            var modelBinder = new Mock<IModelBinder>();
            modelBinder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Callback((ModelBindingContext context) =>
                {
                    Assert.Equal(expectedModelName, context.ModelName, StringComparer.Ordinal);
                })
                .Returns(Task.CompletedTask);

            var parameterDescriptor = new ParameterDescriptor
            {
                Name = expectedModelName,
                ParameterType = typeof(Person),
            };

            var factory = new Mock<IModelBinderFactory>(MockBehavior.Strict);
            factory
                .Setup(f => f.CreateBinder(It.IsAny<ModelBinderFactoryContext>()))
                .Callback((ModelBinderFactoryContext context) =>
                {
                    binderExecuted = true;
                    // Confirm expected data is passed through to ModelBindingFactory.
                    Assert.Null(context.BindingInfo);
                    Assert.Same(parameterDescriptor, context.CacheToken);
                    Assert.Equal(metadata, context.Metadata);
                })
                .Returns(modelBinder.Object);

            var argumentBinder = new ParameterBinder(
                metadataProvider,
                factory.Object,
                CreateMockValidatorProvider());

            var valueProvider = new SimpleValueProvider
            {
                { expectedModelName, new object() },
            };
            var valueProviderFactory = new SimpleValueProviderFactory(valueProvider);

            var controllerContext = new ControllerContext();

            // Act & Assert
            await argumentBinder.BindModelAsync(controllerContext, valueProvider, parameterDescriptor);
            Assert.True(binderExecuted);
        }

        [Fact]
        public async Task BindModelAsync_EnforcesTopLevelBindRequired()
        {
            // Arrange
            var actionContext = new ControllerContext();

            var mockModelMetadata = CreateMockModelMetadata();
            mockModelMetadata.Setup(o => o.IsBindingRequired).Returns(true);
            mockModelMetadata.Setup(o => o.DisplayName).Returns("Ignored Display Name"); // Bind attribute errors are phrased in terms of the model name, not display name

            var parameterBinder = CreateParameterBinder(mockModelMetadata.Object, validator: null);
            var modelBindingResult = ModelBindingResult.Failed();

            // Act
            var result = await parameterBinder.BindModelAsync(
                actionContext,
                CreateMockModelBinder(modelBindingResult),
                CreateMockValueProvider(),
                new ParameterDescriptor { Name = "myParam", ParameterType = typeof(Person) },
                mockModelMetadata.Object,
                "ignoredvalue");

            // Assert
            Assert.False(actionContext.ModelState.IsValid);
            Assert.Equal("myParam", actionContext.ModelState.Single().Key);
            Assert.Equal(
                new DefaultModelBindingMessageProvider().MissingBindRequiredValueAccessor("myParam"),
                actionContext.ModelState.Single().Value.Errors.Single().ErrorMessage);
        }

        [Fact]
        public async Task BindModelAsync_EnforcesTopLevelRequired()
        {
            // Arrange
            var actionContext = new ControllerContext();
            var mockModelMetadata = CreateMockModelMetadata();
            mockModelMetadata.Setup(o => o.IsRequired).Returns(true);
            mockModelMetadata.Setup(o => o.DisplayName).Returns("My Display Name");
            mockModelMetadata.Setup(o => o.ValidatorMetadata).Returns(new[]
            {
                new RequiredAttribute()
            });

            var validator = new DataAnnotationsModelValidator(
                new ValidationAttributeAdapterProvider(),
                new RequiredAttribute(),
                stringLocalizer: null);

            var parameterBinder = CreateParameterBinder(
                mockModelMetadata.Object,
                validator);
            var modelBindingResult = ModelBindingResult.Success(null);

            // Act
            var result = await parameterBinder.BindModelAsync(
                actionContext,
                CreateMockModelBinder(modelBindingResult),
                CreateMockValueProvider(),
                new ParameterDescriptor { Name = "myParam", ParameterType = typeof(Person) },
                mockModelMetadata.Object,
                "ignoredvalue");

            // Assert
            Assert.False(actionContext.ModelState.IsValid);
            Assert.Equal("myParam", actionContext.ModelState.Single().Key);
            Assert.Equal(
                new RequiredAttribute().FormatErrorMessage("My Display Name"),
                actionContext.ModelState.Single().Value.Errors.Single().ErrorMessage);
        }

        [Fact]
        public async Task BindModelAsync_EnforcesTopLevelDataAnnotationsAttribute()
        {
            // Arrange
            var actionContext = new ControllerContext();
            var mockModelMetadata = CreateMockModelMetadata();
            var validationAttribute = new RangeAttribute(1, 100);
            mockModelMetadata.Setup(o => o.DisplayName).Returns("My Display Name");
            mockModelMetadata.Setup(o => o.ValidatorMetadata).Returns(new[] {
                validationAttribute
            });

            var validator = new DataAnnotationsModelValidator(
                new ValidationAttributeAdapterProvider(),
                validationAttribute,
                stringLocalizer: null);

            var parameterBinder = CreateParameterBinder(
                mockModelMetadata.Object,
                validator);
            var modelBindingResult = ModelBindingResult.Success(123);

            // Act
            var result = await parameterBinder.BindModelAsync(
                actionContext,
                CreateMockModelBinder(modelBindingResult),
                CreateMockValueProvider(),
                new ParameterDescriptor { Name = "myParam", ParameterType = typeof(Person) },
                mockModelMetadata.Object,
                50); // This value is ignored, because test explicitly set the ModelBindingResult

            // Assert
            Assert.False(actionContext.ModelState.IsValid);
            Assert.Equal("myParam", actionContext.ModelState.Single().Key);
            Assert.Equal(
                validationAttribute.FormatErrorMessage("My Display Name"),
                actionContext.ModelState.Single().Value.Errors.Single().ErrorMessage);
        }

        [Fact]
        public async Task BindModelAsync_SupportsIObjectModelValidatorForBackCompat()
        {
            // Arrange
            var actionContext = new ControllerContext();

            var mockValidator = new Mock<IObjectModelValidator>(MockBehavior.Strict);
            mockValidator
                .Setup(o => o.Validate(
                    It.IsAny<ActionContext>(),
                    It.IsAny<ValidationStateDictionary>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .Callback((ActionContext context, ValidationStateDictionary validationState, string prefix, object model) =>
                {
                    context.ModelState.AddModelError(prefix, "Test validation message");
                });

            var modelMetadata = CreateMockModelMetadata().Object;
            var parameterBinder = CreateBackCompatParameterBinder(
                modelMetadata,
                mockValidator.Object);
            var modelBindingResult = ModelBindingResult.Success(123);

            // Act
            var result = await parameterBinder.BindModelAsync(
                actionContext,
                CreateMockModelBinder(modelBindingResult),
                CreateMockValueProvider(),
                new ParameterDescriptor { Name = "myParam", ParameterType = typeof(Person) },
                modelMetadata,
                "ignored");

            // Assert
            Assert.False(actionContext.ModelState.IsValid);
            Assert.Equal("myParam", actionContext.ModelState.Single().Key);
            Assert.Equal(
                "Test validation message",
                actionContext.ModelState.Single().Value.Errors.Single().ErrorMessage);
        }

        private static Mock<FakeModelMetadata> CreateMockModelMetadata()
        {
            var mockModelMetadata = new Mock<FakeModelMetadata>();
            mockModelMetadata
                .Setup(o => o.ModelBindingMessageProvider)
                .Returns(new DefaultModelBindingMessageProvider());
            return mockModelMetadata;
        }

        private static IModelBinder CreateMockModelBinder(ModelBindingResult modelBinderResult)
        {
            var mockBinder = new Mock<IModelBinder>(MockBehavior.Strict);
            mockBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns<ModelBindingContext>(context =>
                {
                    context.Result = modelBinderResult;
                    return Task.CompletedTask;
                });
            return mockBinder.Object;
        }

        private static ParameterBinder CreateParameterBinder(
            ModelMetadata modelMetadata,
            IModelValidator validator)
        {
            var mockModelMetadataProvider = new Mock<IModelMetadataProvider>(MockBehavior.Strict);
            mockModelMetadataProvider
                .Setup(o => o.GetMetadataForType(typeof(Person)))
                .Returns(modelMetadata);

            var mockModelBinderFactory = new Mock<IModelBinderFactory>(MockBehavior.Strict);
            return new ParameterBinder(
                mockModelMetadataProvider.Object,
                mockModelBinderFactory.Object,
                CreateMockValidatorProvider(validator));
        }

        private static ParameterBinder CreateBackCompatParameterBinder(
            ModelMetadata modelMetadata,
            IObjectModelValidator validator)
        {
            var mockModelMetadataProvider = new Mock<IModelMetadataProvider>(MockBehavior.Strict);
            mockModelMetadataProvider
                .Setup(o => o.GetMetadataForType(typeof(Person)))
                .Returns(modelMetadata);

            var mockModelBinderFactory = new Mock<IModelBinderFactory>(MockBehavior.Strict);
#pragma warning disable CS0618 // Type or member is obsolete
            return new ParameterBinder(
                mockModelMetadataProvider.Object,
                mockModelBinderFactory.Object,
                validator);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private static IValueProvider CreateMockValueProvider()
        {
            var mockValueProvider = new Mock<IValueProvider>(MockBehavior.Strict);
            mockValueProvider
                .Setup(o => o.ContainsPrefix(It.IsAny<string>()))
                .Returns(true);
            return mockValueProvider.Object;
        }

        private static IModelValidatorProvider CreateMockValidatorProvider(IModelValidator validator = null)
        {
            var mockValidator = new Mock<IModelValidatorProvider>();
            mockValidator
                .Setup(o => o.CreateValidators(
                    It.IsAny<ModelValidatorProviderContext>()))
                .Callback<ModelValidatorProviderContext>(context =>
                {
                    if (validator != null)
                    {
                        foreach (var result in context.Results)
                        {
                            result.Validator = validator;
                        }
                    }
                });
            return mockValidator.Object;
        }

        private class Person : IEquatable<Person>, IEquatable<object>
        {
            public string Name { get; set; }

            public bool Equals(Person other)
            {
                return other != null && string.Equals(Name, other.Name, StringComparison.Ordinal);
            }

            bool IEquatable<object>.Equals(object obj)
            {
                return Equals(obj as Person);
            }
        }

        public abstract class FakeModelMetadata : ModelMetadata
        {
            public FakeModelMetadata()
                : base(ModelMetadataIdentity.ForType(typeof(string)))
            {
            }
        }
    }
}
