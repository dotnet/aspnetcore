// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public class BinderTypeBasedModelBinderIntegrationTest
    {
        [Fact]
        [InlineData(typeof(NullModelNotSetModelBinder), false)]
        public async Task BindParameter_WithModelBinderType_NullData_ReturnsNull()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
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
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);
            Assert.Null(modelBindingResult.Model);

            // ModelState (not set unless inner binder sets it)
            Assert.True(modelState.IsValid);
            var entry = modelState[string.Empty];
            Assert.Null(entry.AttemptedValue);
            Assert.Null(entry.RawValue);
            Assert.Empty(entry.Errors);
            Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        }

        [Fact]
        public async Task BindParameter_WithModelBinderType_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
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
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.Equal(ModelBindingResult.NoResult, modelBindingResult);

            // ModelState (not set unless inner binder sets it)
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
        }

        private class Person2
        {
        }

        [Fact]
        public async Task BindParameter_WithModelBinderType_NonGreedy_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BinderType = typeof(NullResultModelBinder)
                },

                ParameterType = typeof(Person2)
            };

            // No data is passed.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.Equal(ModelBindingResult.NoResult, modelBindingResult);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState.Keys);
        }

        // ModelBinderAttribute can be used without specifying the binder type.
        // In such cases BinderTypeBasedModelBinder acts like a non greedy binder where
        // it returns an empty ModelBindingResult allowing other ModelBinders to run.
        [Fact]
        public async Task BindParameter_WithOutModelBinderType()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BinderType = typeof(NullResultModelBinder)
                },

                ParameterType = typeof(Person2)
            };

            // No data is passed.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.Equal(ModelBindingResult.NoResult, modelBindingResult);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState.Keys);
        }

        // Ensures that prefix is part of the result returned back.
        [Fact]
        [ReplaceCulture]
        public async Task BindParameter_WithData_WithPrefix_GetsBound()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
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

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.Equal("Success", modelBindingResult.Model);
            Assert.Equal("CustomParameter", modelBindingResult.Key);

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

        [ModelBinder(BinderType = typeof(AddressModelBinder))]
        private class Address
        {
            public string Street { get; set; }
        }

        [Fact]
        public async Task BindProperty_WithData_EmptyPrefix_GetsBound()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Person)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);
            Assert.Equal(string.Empty, modelBindingResult.Key);

            // Model
            var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
            Assert.NotNull(boundPerson.Address);
            Assert.Equal("SomeStreet", boundPerson.Address.Street);

            // ModelState
            Assert.True(modelState.IsValid);

            Assert.Equal(1, modelState.Keys.Count);
            var key = Assert.Single(modelState.Keys, k => k == "Address.Street");
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
            Assert.NotNull(modelState[key].RawValue); // Value is set by test model binder, no need to validate it.
        }

        [Fact]
        public async Task BindProperty_WithData_WithPrefix_GetsBound()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "CustomParameter"
                },
                ParameterType = typeof(Person)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.Equal("CustomParameter", modelBindingResult.Key);

            // Model
            var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
            Assert.NotNull(boundPerson.Address);
            Assert.Equal("SomeStreet", boundPerson.Address.Street);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Equal(1, modelState.Keys.Count);
            var key = Assert.Single(modelState.Keys, k => k == "CustomParameter.Address.Street");
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
            Assert.NotNull(modelState[key].RawValue); // Value is set by test model binder, no need to validate it.
        }

        private class AddressModelBinder : IModelBinder
        {
            public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
            {
                if (bindingContext.ModelType != typeof(Address))
                {
                    return null;
                }

                var address = new Address() { Street = "SomeStreet" };

                bindingContext.ModelState.SetModelValue(
                    ModelNames.CreatePropertyModelName(bindingContext.ModelName, "Street"),
                    new string[] { address.Street },
                    address.Street);

                return ModelBindingResult.SuccessAsync(bindingContext.ModelName, address);
            }
        }

        private class SuccessModelBinder : IModelBinder
        {
            public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
            {
                var model = "Success";
                bindingContext.ModelState.SetModelValue(
                    bindingContext.ModelName,
                    new string[] { model },
                    model);

                return ModelBindingResult.SuccessAsync(bindingContext.ModelName, model);
            }
        }

        private class NullModelBinder : IModelBinder
        {
            public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
            {
                return ModelBindingResult.SuccessAsync(bindingContext.ModelName, model: null);
            }
        }

        private class NullModelNotSetModelBinder : IModelBinder
        {
            public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
            {
                return ModelBindingResult.FailedAsync(bindingContext.ModelName);
            }
        }

        private class NullResultModelBinder : IModelBinder
        {
            public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
            {
                return ModelBindingResult.NoResultAsync;
            }
        }
    }
}