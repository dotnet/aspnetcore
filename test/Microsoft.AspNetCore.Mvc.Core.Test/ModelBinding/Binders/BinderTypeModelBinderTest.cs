// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class BinderTypeModelBinderTest
    {
        [Fact]
        public async Task BindModel_ReturnsFailedResult_EvenIfSelectedBinderReturnsNull()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(Person), binderType: typeof(NullModelBinder));

            var binder = new BinderTypeModelBinder(typeof(NullModelBinder));

            // Act
            var binderResult = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), binderResult);
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

            var binder = new BinderTypeModelBinder(typeof(NotNullModelBinder));

            // Act
            var binderResult = await binder.BindModelResultAsync(bindingContext);

            // Assert
            var p = (Person)binderResult.Model;
            Assert.Equal(model.Age, p.Age);
            Assert.Equal(model.Name, p.Name);
        }

        [Fact]
        public void BindModel_ForNonModelBinder_Throws()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(Person), binderType: typeof(Person));

            var expected = $"The type '{typeof(Person).FullName}' must implement " +
                $"'{typeof(IModelBinder).FullName}' to be used as a model binder.";

            // Act & Assert
            ExceptionAssert.ThrowsArgument(
                () => new BinderTypeModelBinder(typeof(Person)),
                "binderType",
                expected);
        }

        private static DefaultModelBindingContext GetBindingContext(Type modelType, Type binderType = null)
        {
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForType(modelType).BindingDetails(bd => bd.BinderType = binderType);

            var operationBindingContext = new OperationBindingContext
            {
                ActionContext = new ActionContext()
                {
                    HttpContext = new DefaultHttpContext(),
                },
                MetadataProvider = metadataProvider,
                ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
            };

            var bindingContext = new DefaultModelBindingContext
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

        private class NullModelBinder : IModelBinder
        {
            public Task BindModelAsync(ModelBindingContext bindingContext)
            {
                return Task.FromResult(0);
            }
        }

        private class NotNullModelBinder : IModelBinder
        {
            private readonly object _model;

            public NotNullModelBinder()
            {
                _model = new Person();
            }

            public Task BindModelAsync(ModelBindingContext bindingContext)
            {
                bindingContext.Result = ModelBindingResult.Success(bindingContext.ModelName, _model);
                return Task.FromResult(0);
            }
        }
    }
}
