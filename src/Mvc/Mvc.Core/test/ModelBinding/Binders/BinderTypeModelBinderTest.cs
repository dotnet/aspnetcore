// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class BinderTypeModelBinderTest
{
    [Fact]
    public async Task BindModel_ReturnsFailedResult_EvenIfSelectedBinderReturnsNull()
    {
        // Arrange
        var bindingContext = GetBindingContext(typeof(Person), binderType: typeof(NullModelBinder));

        var binder = new BinderTypeModelBinder(typeof(NullModelBinder));

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
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

        bindingContext.HttpContext.RequestServices = serviceProvider;

        var binder = new BinderTypeModelBinder(typeof(NotNullModelBinder));

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        var person = (Person)bindingContext.Result.Model;
        Assert.Equal(model.Age, person.Age);
        Assert.Equal(model.Name, person.Name);
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

        var bindingContext = new DefaultModelBindingContext
        {
            ActionContext = new ActionContext()
            {
                HttpContext = new DefaultHttpContext() { RequestServices = new ServiceCollection().BuildServiceProvider() },
            },
            ModelMetadata = metadataProvider.GetMetadataForType(modelType),
            ModelName = "someName",
            ValueProvider = Mock.Of<IValueProvider>(),
            ModelState = new ModelStateDictionary(),
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
            bindingContext.Result = ModelBindingResult.Success(_model);
            return Task.CompletedTask;
        }
    }
}
