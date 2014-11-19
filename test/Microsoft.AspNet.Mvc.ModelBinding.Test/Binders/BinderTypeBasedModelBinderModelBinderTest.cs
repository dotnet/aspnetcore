// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.PipelineCore;
using Microsoft.Framework.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class BinderTypeBasedModelBinderModelBinderTest
    {
        [Fact]
        public async Task BindModel_ReturnsFalseIfNoBinderTypeIsSet()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(Person));

            var binder = new BinderTypeBasedModelBinder(Mock.Of<ITypeActivator>());

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(binderResult);
        }

        [Fact]
        public async Task BindModel_ReturnsTrueEvenIfSelectedBinderReturnsFalse()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(Person));
            bindingContext.ModelMetadata.BinderType = typeof(FalseModelBinder);

            var innerModelBinder = new FalseModelBinder();

            var mockITypeActivator = new Mock<ITypeActivator>();
            mockITypeActivator
                .Setup(o => o.CreateInstance(It.IsAny<IServiceProvider>(), typeof(FalseModelBinder)))
                .Returns(innerModelBinder);

            var binder = new BinderTypeBasedModelBinder(mockITypeActivator.Object);

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(binderResult);
        }

        [Fact]
        public async Task BindModel_CallsBindAsync_OnProvidedModelBinder()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(Person));
            bindingContext.ModelMetadata.BinderType = typeof(TrueModelBinder);

            var model = new Person();
            var innerModelBinder = new TrueModelBinder(model);

            var mockITypeActivator = new Mock<ITypeActivator>();
            mockITypeActivator
                .Setup(o => o.CreateInstance(It.IsAny<IServiceProvider>(), typeof(TrueModelBinder)))
                .Returns(innerModelBinder);

            var binder = new BinderTypeBasedModelBinder(mockITypeActivator.Object);

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(binderResult);
            Assert.Same(model, bindingContext.Model);
        }

        [Fact]
        public async Task BindModel_CallsBindAsync_OnProvidedModelBinderProvider()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(Person));
            bindingContext.ModelMetadata.BinderType = typeof(ModelBinderProvider);

            var model = new Person();
            var innerModelBinder = new TrueModelBinder(model);

            var provider = new ModelBinderProvider(innerModelBinder);

            var mockITypeActivator = new Mock<ITypeActivator>();
            mockITypeActivator
                .Setup(o => o.CreateInstance(It.IsAny<IServiceProvider>(), typeof(ModelBinderProvider)))
                .Returns(provider);

            var binder = new BinderTypeBasedModelBinder(mockITypeActivator.Object);

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(binderResult);
            Assert.Same(model, bindingContext.Model);
        }

        [Fact]
        public async Task BindModel_ForNonModelBinderAndModelBinderProviderTypes_Throws()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(Person));
            bindingContext.ModelMetadata.BinderType = typeof(string);
            var binder = new BinderTypeBasedModelBinder(Mock.Of<ITypeActivator>());

            var expected = "The type 'System.String' must implement either " +
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
                ModelMetadata = metadataProvider.GetMetadataForType(null, modelType),
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
            public Task<bool> BindModelAsync(ModelBindingContext bindingContext)
            {
                return Task.FromResult(false);
            }
        }

        private class TrueModelBinder : IModelBinder
        {
            private readonly object _model;

            public TrueModelBinder(object model)
            {
                _model = model;
            }

            public Task<bool> BindModelAsync(ModelBindingContext bindingContext)
            {
                bindingContext.Model = _model;
                return Task.FromResult(true);
            }
        }

        private class ModelBinderProvider : IModelBinderProvider
        {
            private readonly IModelBinder _inner;

            public ModelBinderProvider(IModelBinder inner)
            {
                _inner = inner;
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
