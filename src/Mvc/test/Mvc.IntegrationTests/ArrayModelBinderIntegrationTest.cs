// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

// Integration tests targeting the behavior of the ArrayModelBinder with other model binders.
public class ArrayModelBinderIntegrationTest
{
    [Fact]
    public async Task ArrayModelBinder_BindsArrayOfSimpleType_WithPrefix_Success()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(int[])
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter[0]=10&parameter[1]=11");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<int[]>(modelBindingResult.Model);
        Assert.Equal(new int[] { 10, 11 }, model);

        Assert.Equal(2, modelState.Count);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[0]").Value;
        Assert.Equal("10", entry.AttemptedValue);
        Assert.Equal("10", entry.RawValue);

        entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[1]").Value;
        Assert.Equal("11", entry.AttemptedValue);
        Assert.Equal("11", entry.RawValue);
    }

    [Fact]
    public async Task ArrayModelBinder_BindsArrayOfSimpleType_WithExplicitPrefix_Success()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            BindingInfo = new BindingInfo()
            {
                BinderModelName = "prefix",
            },
            ParameterType = typeof(int[])
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?prefix[0]=10&prefix[1]=11");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<int[]>(modelBindingResult.Model);
        Assert.Equal(new int[] { 10, 11 }, model);

        Assert.Equal(2, modelState.Count);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState, kvp => kvp.Key == "prefix[0]").Value;
        Assert.Equal("10", entry.AttemptedValue);
        Assert.Equal("10", entry.RawValue);

        entry = Assert.Single(modelState, kvp => kvp.Key == "prefix[1]").Value;
        Assert.Equal("11", entry.AttemptedValue);
        Assert.Equal("11", entry.RawValue);
    }

    [Fact]
    public async Task ArrayModelBinder_BindsArrayOfSimpleType_EmptyPrefix_Success()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(int[])
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?[0]=10&[1]=11");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<int[]>(modelBindingResult.Model);
        Assert.Equal(new int[] { 10, 11 }, model);

        Assert.Equal(2, modelState.Count);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState, kvp => kvp.Key == "[0]").Value;
        Assert.Equal("10", entry.AttemptedValue);
        Assert.Equal("10", entry.RawValue);

        entry = Assert.Single(modelState, kvp => kvp.Key == "[1]").Value;
        Assert.Equal("11", entry.AttemptedValue);
        Assert.Equal("11", entry.RawValue);
    }

    [Fact]
    public async Task ArrayModelBinder_BindsArrayOfSimpleType_NoData()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(int[])
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);
        Assert.Empty(Assert.IsType<int[]>(modelBindingResult.Model));

        Assert.Empty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    private class Person
    {
        public string Name { get; set; }
    }

    [Fact]
    public async Task ArrayModelBinder_BindsArrayOfComplexType_WithPrefix_Success()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Person[])
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter[0].Name=bill&parameter[1].Name=lang");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Person[]>(modelBindingResult.Model);
        Assert.Equal("bill", model[0].Name);
        Assert.Equal("lang", model[1].Name);

        Assert.Equal(2, modelState.Count);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[0].Name").Value;
        Assert.Equal("bill", entry.AttemptedValue);
        Assert.Equal("bill", entry.RawValue);

        entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[1].Name").Value;
        Assert.Equal("lang", entry.AttemptedValue);
        Assert.Equal("lang", entry.RawValue);
    }

    [Fact]
    public async Task ArrayModelBinder_BindsArrayOfComplexType_WithExplicitPrefix_Success()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            BindingInfo = new BindingInfo()
            {
                BinderModelName = "prefix",
            },
            ParameterType = typeof(Person[])
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?prefix[0].Name=bill&prefix[1].Name=lang");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Person[]>(modelBindingResult.Model);
        Assert.Equal("bill", model[0].Name);
        Assert.Equal("lang", model[1].Name);

        Assert.Equal(2, modelState.Count);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState, kvp => kvp.Key == "prefix[0].Name").Value;
        Assert.Equal("bill", entry.AttemptedValue);
        Assert.Equal("bill", entry.RawValue);

        entry = Assert.Single(modelState, kvp => kvp.Key == "prefix[1].Name").Value;
        Assert.Equal("lang", entry.AttemptedValue);
        Assert.Equal("lang", entry.RawValue);
    }

    [Fact]
    public async Task ArrayModelBinder_BindsArrayOfComplexType_EmptyPrefix_Success()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Person[])
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?[0].Name=bill&[1].Name=lang");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Person[]>(modelBindingResult.Model);
        Assert.Equal("bill", model[0].Name);
        Assert.Equal("lang", model[1].Name);

        Assert.Equal(2, modelState.Count);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState, kvp => kvp.Key == "[0].Name").Value;
        Assert.Equal("bill", entry.AttemptedValue);
        Assert.Equal("bill", entry.RawValue);

        entry = Assert.Single(modelState, kvp => kvp.Key == "[1].Name").Value;
        Assert.Equal("lang", entry.AttemptedValue);
        Assert.Equal("lang", entry.RawValue);
    }

    [Fact]
    public async Task ArrayModelBinder_BindsArrayOfComplexType_NoData()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Person[])
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);
        Assert.Empty(Assert.IsType<Person[]>(modelBindingResult.Model));

        Assert.Empty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    private class PersonWithReadOnlyAndInitializedProperty
    {
        public string Name { get; set; }

        public string[] Aliases { get; } = new[] { "Alias1", "Alias2" };
    }

    [Fact]
    public async Task ArrayModelBinder_BindsArrayOfComplexTypeHavingInitializedData_WithPrefix_Success_ReadOnly()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(PersonWithReadOnlyAndInitializedProperty)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter.Name=James&parameter.Aliases[0]=bill&parameter.Aliases[1]=william");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        Assert.True(modelState.IsValid);

        var model = Assert.IsType<PersonWithReadOnlyAndInitializedProperty>(modelBindingResult.Model);
        Assert.Equal("James", model.Name);
        Assert.NotNull(model.Aliases);
        Assert.Collection(
            model.Aliases,
            (e) => Assert.Equal("Alias1", e),
            (e) => Assert.Equal("Alias2", e));
    }

    [Fact]
    public async Task ArrayModelBinder_ThrowsOn1025Items_AtTopLevel()
    {
        // Arrange
        var expectedMessage = "Collection bound to 'parameter' exceeded " +
            $"{nameof(MvcOptions)}.{nameof(MvcOptions.MaxModelBindingCollectionSize)} (1024). This limit is a " +
            $"safeguard against incorrect model binders and models. Address issues in " +
            $"'{typeof(SuccessfulModel)}'. For example, this type may have a property with a model binder that " +
            $"always succeeds. See the {nameof(MvcOptions)}.{nameof(MvcOptions.MaxModelBindingCollectionSize)} " +
            "documentation for more information.";
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(SuccessfulModel[]),
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            // CollectionModelBinder binds an empty collection when value providers are all empty.
            request.QueryString = new QueryString("?a=b");
        });

        var modelState = testContext.ModelState;
        var metadata = testContext.MetadataProvider.GetMetadataForType(parameter.ParameterType);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => parameterBinder.BindModelAsync(parameter, testContext));
        Assert.Equal(expectedMessage, exception.Message);
    }
}
