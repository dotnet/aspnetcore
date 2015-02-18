// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Core;
using Moq;
using Xunit;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class BinderTypeBasedModelBinderModelBinderTest
    {
        [Fact]
        public async Task BindModel_ReturnsFalseIfNoBinderTypeIsSet()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(Person));

            var binder = new BinderTypeBasedModelBinder();

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(binderResult);
        }

        [Fact]
        public async Task BindModel_ReturnsTrueEvenIfSelectedBinderReturnsFalse()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(Person));
            bindingContext.ModelMetadata.BinderType = typeof(FalseModelBinder);

            var binder = new BinderTypeBasedModelBinder();

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(binderResult);
        }

        [Fact]
        public async Task BindModel_CallsBindAsync_OnProvidedModelBinder()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(Person));
            bindingContext.ModelMetadata.BinderType = typeof(TrueModelBinder);

            var model = new Person();
            var innerModelBinder = new TrueModelBinder();
            var serviceProvider = new ServiceCollection()
                .AddSingleton(typeof(IModelBinder))
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
        public async Task BindModel_CallsBindAsync_OnProvidedModelBinderProvider()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(Person));
            bindingContext.ModelMetadata.BinderType = typeof(ModelBinderProvider);

            var model = new Person();
            var provider = new ModelBinderProvider();

            var serviceProvider = new ServiceCollection()
                .AddSingleton(typeof(IModelBinderProvider))
                .AddSingleton(typeof(IModelBinder))
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
        public async Task BindModel_ForNonModelBinderAndModelBinderProviderTypes_Throws()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(Person));
            bindingContext.ModelMetadata.BinderType = typeof(Person);
            var binder = new BinderTypeBasedModelBinder();

            var expected = "The type '" + typeof(Person).FullName + "' must implement either " +
                "'Microsoft.AspNet.Mvc.ModelBinding.IModelBinder' or " +
                "'Microsoft.AspNet.Mvc.ModelBinding.IModelBinderProvider' to be used as a model binder.";

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => binder.BindModelAsync(bindingContext));

            // Assert
            Assert.Equal(expected, ex.Message);
        }

        private static ModelBindingContext GetBindingContext(Type modelType)
        {
            var metadataProvider = new DataAnnotationsModelMetadataProvider();
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
            };

            return bindingContext;
        }

        private class Person
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }

        private class FalseModelBinder : IModelBinder
        {
            public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
            {
                return Task.FromResult<ModelBindingResult>(null);
            }
        }

        private class TrueModelBinder : IModelBinder
        {
            private readonly object _model;

            public TrueModelBinder()
            {
                _model = new Person();
            }

            public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
            {
                return Task.FromResult(new ModelBindingResult(_model, bindingContext.ModelName, true));
            }
        }

        private class ModelBinderProvider : IModelBinderProvider
        {
            private readonly IModelBinder _inner;

            public ModelBinderProvider()
            {
                var innerModelBinder = new TrueModelBinder();
                _inner = innerModelBinder;
            }

            public IReadOnlyList<IModelBinder> ModelBinders
            {
                get
                {
                    return new List<IModelBinder>() { _inner, };
                }
            }
        }
    }
}
#endif
