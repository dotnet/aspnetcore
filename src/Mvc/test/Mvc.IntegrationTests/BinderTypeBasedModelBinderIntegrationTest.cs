// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

public class BinderTypeBasedModelBinderIntegrationTest
{
    [Fact]
    public async Task BindParameter_WithModelBinderType_NullData_ReturnsNull()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo()
            {
                BinderType = typeof(NullModelBinder)
            },

            ParameterType = typeof(string)
        };

        // No data is passed.
        var testContext = ModelBindingTestHelper.GetTestContext();
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert

        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);
        Assert.Null(modelBindingResult.Model);

        // ModelState (not set unless inner binder sets it)
        Assert.True(modelState.IsValid);
        Assert.Empty(modelState);
    }

    [Fact]
    public async Task BindParameter_WithModelBinderType_NoData()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo()
            {
                BinderType = typeof(NullModelNotSetModelBinder)
            },

            ParameterType = typeof(string)
        };

        // No data is passed.
        var testContext = ModelBindingTestHelper.GetTestContext();
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.False(modelBindingResult.IsModelSet);

        // ModelState (not set unless inner binder sets it)
        Assert.True(modelState.IsValid);
        Assert.Empty(modelState);
    }

    private class Person2
    {
    }

    // Ensures that prefix is part of the result returned back.
    [Fact]
    [ReplaceCulture]
    public async Task BindParameter_WithData_WithPrefix_GetsBound()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo()
            {
                BinderType = typeof(SuccessModelBinder),
                BinderModelName = "CustomParameter"
            },

            ParameterType = typeof(Person2)
        };

        var testContext = ModelBindingTestHelper.GetTestContext();
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert

        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);
        Assert.Equal("Success", modelBindingResult.Model);

        // ModelState
        Assert.True(modelState.IsValid);
        var key = Assert.Single(modelState.Keys);
        Assert.Equal("CustomParameter", key);
        Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        Assert.NotNull(modelState[key].RawValue); // Value is set by test model binder, no need to validate it.
    }

    private class Person
    {
        public Address Address { get; set; }
    }

    [ModelBinder<AddressModelBinder>]
    private class Address
    {
        public string Street { get; set; }
    }

    public static TheoryData<BindingInfo> NullAndEmptyBindingInfo
    {
        get
        {
            return new TheoryData<BindingInfo>
                {
                    null,
                    new BindingInfo(),
                };
        }
    }

    // Make sure the metadata is honored when a [ModelBinder] attribute is associated with an action parameter's
    // type. This should behave identically to such an attribute on an action parameter. (Tests such as
    // BindParameter_WithData_WithPrefix_GetsBound cover associating [ModelBinder] with an action parameter.)
    //
    // This is a regression test for aspnet/Mvc#4652 and aspnet/Mvc#7595
    [Theory]
    [MemberData(nameof(NullAndEmptyBindingInfo))]
    public async Task BinderTypeOnParameterType_WithData_EmptyPrefix_GetsBound(BindingInfo bindingInfo)
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameters = typeof(TestController).GetMethod(nameof(TestController.Action)).GetParameters();
        var parameter = new ControllerParameterDescriptor
        {
            Name = "Parameter1",
            BindingInfo = bindingInfo,
            ParameterInfo = parameters[0],
            ParameterType = typeof(Address),
        };

        var testContext = ModelBindingTestHelper.GetTestContext();
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);

        // Model
        var address = Assert.IsType<Address>(modelBindingResult.Model);
        Assert.Equal("SomeStreet", address.Street);

        // ModelState
        Assert.True(modelState.IsValid);
        var kvp = Assert.Single(modelState);
        Assert.Equal("Street", kvp.Key);
        var entry = kvp.Value;
        Assert.NotNull(entry);
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.NotNull(entry.RawValue); // Value is set by test model binder, no need to validate it.
    }

    private class Person3
    {
        [ModelBinder<Address3ModelBinder>]
        public Address3 Address { get; set; }
    }

    private class Address3
    {
        public string Street { get; set; }
    }

    // Make sure the metadata is honored when a [ModelBinder] attribute is associated with a property in the type
    // hierarchy of an action parameter. (Tests such as BindProperty_WithData_EmptyPrefix_GetsBound cover
    // associating [ModelBinder] with a class somewhere in the type hierarchy of an action parameter.)
    [Theory]
    [MemberData(nameof(NullAndEmptyBindingInfo))]
    public async Task BinderTypeOnProperty_WithData_EmptyPrefix_GetsBound(BindingInfo bindingInfo)
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor
        {
            Name = "Parameter1",
            BindingInfo = bindingInfo,
            ParameterType = typeof(Person3),
        };

        var testContext = ModelBindingTestHelper.GetTestContext();
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);

        // Model
        var person = Assert.IsType<Person3>(modelBindingResult.Model);
        Assert.NotNull(person.Address);
        Assert.Equal("SomeStreet", person.Address.Street);

        // ModelState
        Assert.True(modelState.IsValid);
        var kvp = Assert.Single(modelState);
        Assert.Equal("Address.Street", kvp.Key);
        var entry = kvp.Value;
        Assert.NotNull(entry);
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        Assert.NotNull(entry.RawValue); // Value is set by test model binder, no need to validate it.
    }

    [Fact]
    public async Task BindProperty_WithData_EmptyPrefix_GetsBound()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo(),
            ParameterType = typeof(Person)
        };

        var testContext = ModelBindingTestHelper.GetTestContext();
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert

        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);

        // Model
        var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
        Assert.NotNull(boundPerson.Address);
        Assert.Equal("SomeStreet", boundPerson.Address.Street);

        // ModelState
        Assert.True(modelState.IsValid);
        var key = Assert.Single(modelState.Keys);
        Assert.Equal("Address.Street", key);
        Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        Assert.NotNull(modelState[key].RawValue); // Value is set by test model binder, no need to validate it.
    }

    [Fact]
    public async Task BindProperty_WithData_WithPrefix_GetsBound()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo()
            {
                BinderModelName = "CustomParameter"
            },
            ParameterType = typeof(Person)
        };

        var testContext = ModelBindingTestHelper.GetTestContext();
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert

        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);

        // Model
        var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
        Assert.NotNull(boundPerson.Address);
        Assert.Equal("SomeStreet", boundPerson.Address.Street);

        // ModelState
        Assert.True(modelState.IsValid);
        var key = Assert.Single(modelState.Keys);
        Assert.Equal("CustomParameter.Address.Street", key);
        Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        Assert.NotNull(modelState[key].RawValue); // Value is set by test model binder, no need to validate it.
    }

    private class AddressModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);

            Debug.Assert(bindingContext.Result == ModelBindingResult.Failed());

            if (bindingContext.ModelType != typeof(Address))
            {
                return Task.CompletedTask;
            }

            var address = new Address() { Street = "SomeStreet" };

            bindingContext.ModelState.SetModelValue(
                ModelNames.CreatePropertyModelName(bindingContext.ModelName, "Street"),
                new string[] { address.Street },
                address.Street);

            bindingContext.Result = ModelBindingResult.Success(address);
            return Task.CompletedTask;
        }
    }

    private class Address3ModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);

            Debug.Assert(bindingContext.Result == ModelBindingResult.Failed());

            if (bindingContext.ModelType != typeof(Address3))
            {
                return Task.CompletedTask;
            }

            var address = new Address3 { Street = "SomeStreet" };

            bindingContext.ModelState.SetModelValue(
                ModelNames.CreatePropertyModelName(bindingContext.ModelName, "Street"),
                new string[] { address.Street },
                address.Street);

            bindingContext.Result = ModelBindingResult.Success(address);
            return Task.CompletedTask;
        }
    }

    private class SuccessModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);
            Debug.Assert(bindingContext.Result == ModelBindingResult.Failed());

            var model = "Success";
            bindingContext.ModelState.SetModelValue(
                bindingContext.ModelName,
                new string[] { model },
                model);

            bindingContext.Result = ModelBindingResult.Success(model);
            return Task.CompletedTask;
        }
    }

    private class NullModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);
            Debug.Assert(bindingContext.Result == ModelBindingResult.Failed());

            bindingContext.Result = ModelBindingResult.Success(model: null);
            return Task.CompletedTask;
        }
    }

    private class NullModelNotSetModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);
            Debug.Assert(bindingContext.Result == ModelBindingResult.Failed());

            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }
    }

    private class TestController
    {
        public void Action(Address address)
        {
        }
    }
}
