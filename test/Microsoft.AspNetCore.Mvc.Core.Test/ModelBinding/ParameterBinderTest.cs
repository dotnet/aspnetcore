// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Internal;
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
                .Returns(TaskCache.CompletedTask);

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
                CreateMockValidator());

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
                .Returns(TaskCache.CompletedTask);

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
                CreateMockValidator());

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

        private static IObjectModelValidator CreateMockValidator()
        {
            var mockValidator = new Mock<IObjectModelValidator>();
            mockValidator
                .Setup(o => o.Validate(
                    It.IsAny<ActionContext>(),
                    It.IsAny<ValidationStateDictionary>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()));
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
    }
}
