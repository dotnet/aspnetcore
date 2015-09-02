// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class BinderTypeBasedModelBinderModelBinderTest
    {
        [Fact]
        public async Task BindModel_ReturnsNoResult_IfNoBinderTypeIsSet()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(Person));

            var binder = new BinderTypeBasedModelBinder();

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Equal(ModelBindingResult.NoResult, binderResult);
        }

        [Fact]
        public async Task BindModel_ReturnsFailedResult_EvenIfSelectedBinderReturnsNull()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(Person), binderType: typeof(NullModelBinder));

            var binder = new BinderTypeBasedModelBinder();

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, binderResult);
            Assert.False(binderResult.IsModelSet);
        }

        [Fact]
        public async Task BindModel_CallsBindAsync_OnProvidedModelBinder()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(Person), binderType: typeof(NotNullModelBinder));

            var model = new Person();
            var serviceProvider = new ServiceCollection()
                .AddSingleton<IModelBinder, NullModelBinder>()
                .BuildServiceProvider();

            bindingContext.OperationBindingContext.HttpContext.RequestServices = serviceProvider;

            var binder = new BinderTypeBasedModelBinder();

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            var p = (Person)binderResult.Model;
            Assert.Equal(model.Age, p.Age);
            Assert.Equal(model.Name, p.Name);
        }

        [Fact]
        public async Task BindModel_ForNonModelBinder_Throws()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(Person), binderType: typeof(Person));
            var binder = new BinderTypeBasedModelBinder();

            var expected = $"The type '{typeof(Person).FullName}' must implement " +
                $"'{typeof(IModelBinder).FullName}' to be used as a model binder.";

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => binder.BindModelAsync(bindingContext));

            // Assert
            Assert.Equal(expected, ex.Message);
        }

        private static ModelBindingContext GetBindingContext(Type modelType, Type binderType = null)
        {
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForType(modelType).BindingDetails(bd => bd.BinderType = binderType);

            var operationBindingContext = new OperationBindingContext
            {
                MetadataProvider = metadataProvider,
                HttpContext = new DefaultHttpContext(),
                ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
            };

            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(modelType),
                ModelName = "someName",
                ValueProvider = Mock.Of<IValueProvider>(),
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = operationBindingContext,
                BinderType = binderType
            };

            return bindingContext;
        }

        private class Person
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }

        private class NullModelBinder : IModelBinder
        {
            public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
            {
                return ModelBindingResult.NoResultAsync;
            }
        }

        private class NotNullModelBinder : IModelBinder
        {
            private readonly object _model;

            public NotNullModelBinder()
            {
                _model = new Person();
            }

            public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
            {
                return ModelBindingResult.SuccessAsync(bindingContext.ModelName, _model);
            }
        }
    }
}
#endif
