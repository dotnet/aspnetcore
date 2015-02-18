// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class KeyValuePairModelBinderTest
    {
        [Fact]
        public async Task BindModel_MissingKey_ReturnsTrue_AndAddsModelValidationError()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider();

            // Create string binder to create the value but not the key.
            var bindingContext = GetBindingContext(valueProvider, CreateStringBinder());
            var binder = new KeyValuePairModelBinder<int, string>();

            // Act
            var retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(retVal);
            Assert.Null(retVal.Model);
            Assert.False(bindingContext.ModelState.IsValid);
            Assert.Equal("someName", bindingContext.ModelName);
            var error = Assert.Single(bindingContext.ModelState["someName.Key"].Errors);
            Assert.Equal("A value is required.", error.ErrorMessage);
        }

        [Fact]
        public async Task BindModel_MissingValue_ReturnsTrue_AndAddsModelValidationError()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider();

            // Create int binder to create the value but not the key.
            var bindingContext = GetBindingContext(valueProvider, CreateIntBinder());
            var binder = new KeyValuePairModelBinder<int, string>();

            // Act
            var retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(retVal);
            Assert.Null(retVal.Model);
            Assert.False(bindingContext.ModelState.IsValid);
            Assert.Equal("someName", bindingContext.ModelName);
            Assert.Equal(bindingContext.ModelState["someName.Value"].Errors.First().ErrorMessage, "A value is required.");
        }

        [Fact]
        public async Task BindModel_MissingKeyAndMissingValue_DoNotAddModelStateError()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider();

            // Create int binder to create the value but not the key.
            var bindingContext = GetBindingContext(valueProvider);
            var mockBinder = new Mock<IModelBinder>();
            mockBinder.Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                      .Returns(Task.FromResult<ModelBindingResult>(null));

            bindingContext.OperationBindingContext.ModelBinder = mockBinder.Object;
            var binder = new KeyValuePairModelBinder<int, string>();

            // Act
            var retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(retVal);
            Assert.True(bindingContext.ModelState.IsValid);
            Assert.Equal(0, bindingContext.ModelState.ErrorCount);
        }

        [Fact]
        public async Task BindModel_SubBindingSucceeds()
        {
            // Arrange
            var innerBinder = new CompositeModelBinder(new[] { CreateStringBinder(), CreateIntBinder() });
            var valueProvider = new SimpleHttpValueProvider();
            var bindingContext = GetBindingContext(valueProvider, innerBinder);

            var binder = new KeyValuePairModelBinder<int, string>();

            // Act
            var retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(retVal);
            Assert.Equal(new KeyValuePair<int, string>(42, "some-value"), retVal.Model);
        }

        [Fact]
        public async Task TryBindStrongModel_BinderExists_BinderReturnsCorrectlyTypedObject_ReturnsTrue()
        {
            // Arrange
            ModelBindingContext bindingContext = GetBindingContext(new SimpleHttpValueProvider());
            var binder = new KeyValuePairModelBinder<int, string>();

            // Act
            var retVal = await binder.TryBindStrongModel<int>(bindingContext, "key");

            // Assert
            Assert.True(retVal.IsModelSet);
            Assert.Equal(42, retVal.Model);
            Assert.Empty(bindingContext.ModelState);
        }

        [Fact]
        public async Task TryBindStrongModel_BinderExists_BinderReturnsIncorrectlyTypedObject_ReturnsTrue()
        {
            // Arrange
            var innerBinder = new Mock<IModelBinder>();
            innerBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext mbc) =>
                {
                    Assert.Equal("someName.key", mbc.ModelName);
                    return Task.FromResult(new ModelBindingResult(null, string.Empty, true));
                });
            var bindingContext = GetBindingContext(new SimpleHttpValueProvider(), innerBinder.Object);


            var binder = new KeyValuePairModelBinder<int, string>();

            // Act
            var retVal = await binder.TryBindStrongModel<int>(bindingContext, "key");

            // Assert
            Assert.True(retVal.IsModelSet);
            Assert.Null(retVal.Model);
            Assert.Empty(bindingContext.ModelState);
        }

        private static ModelBindingContext GetBindingContext(
            IValueProvider valueProvider,
            IModelBinder innerBinder = null,
            Type keyValuePairType = null)
        {
            var metataProvider = new EmptyModelMetadataProvider();
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = metataProvider.GetMetadataForType(keyValuePairType ?? typeof(KeyValuePair<int, string>)),
                ModelName = "someName",
                ValueProvider = valueProvider,
                OperationBindingContext = new OperationBindingContext
                {
                    ModelBinder = innerBinder ?? CreateIntBinder(),
                    MetadataProvider = metataProvider,
                    ValidatorProvider = new DataAnnotationsModelValidatorProvider()
                }
            };
            return bindingContext;
        }

        private static IModelBinder CreateIntBinder()
        {
            var mockIntBinder = new Mock<IModelBinder>();
            mockIntBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext mbc) =>
                {
                    if (mbc.ModelType == typeof(int))
                    {
                        return Task.FromResult(new ModelBindingResult(42, mbc.ModelName, true));
                    }
                    return Task.FromResult<ModelBindingResult>(null);
                });
            return mockIntBinder.Object;
        }

        private static IModelBinder CreateStringBinder()
        {
            var mockStringBinder = new Mock<IModelBinder>();
            mockStringBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext mbc) =>
                {
                    if (mbc.ModelType == typeof(string))
                    {
                        return Task.FromResult(new ModelBindingResult("some-value", mbc.ModelName, true));
                    }
                    return Task.FromResult<ModelBindingResult>(null);
                });
            return mockStringBinder.Object;
        }
    }
}
#endif
