// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
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
            var testContext = ModelBindingTestHelper.GetTestContext();
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, testContext) ?? default(ModelBindingResult);

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
            var testContext = ModelBindingTestHelper.GetTestContext();
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.False(modelBindingResult.Value.IsModelSet);

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
            var testContext = ModelBindingTestHelper.GetTestContext();
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.False(modelBindingResult.Value.IsModelSet);

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
            var testContext = ModelBindingTestHelper.GetTestContext();
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.False(modelBindingResult.Value.IsModelSet);

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

            var testContext = ModelBindingTestHelper.GetTestContext();
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, testContext) ?? default(ModelBindingResult);

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

            var testContext = ModelBindingTestHelper.GetTestContext();
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, testContext) ?? default(ModelBindingResult);

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

            var testContext = ModelBindingTestHelper.GetTestContext();
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, testContext) ?? default(ModelBindingResult);

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
                if (bindingContext == null)
                {
                    throw new ArgumentNullException(nameof(bindingContext));
                }
                Debug.Assert(bindingContext.Result == null);

                if (bindingContext.ModelType != typeof(Address))
                {
                    return TaskCache.CompletedTask;
                }

                var address = new Address() { Street = "SomeStreet" };

                bindingContext.ModelState.SetModelValue(
                    ModelNames.CreatePropertyModelName(bindingContext.ModelName, "Street"),
                    new string[] { address.Street },
                    address.Street);

                bindingContext.Result = ModelBindingResult.Success(address);
                return TaskCache.CompletedTask;
            }
        }

        private class SuccessModelBinder : IModelBinder
        {
            public Task BindModelAsync(ModelBindingContext bindingContext)
            {
                if (bindingContext == null)
                {
                    throw new ArgumentNullException(nameof(bindingContext));
                }
                Debug.Assert(bindingContext.Result == null);

                var model = "Success";
                bindingContext.ModelState.SetModelValue(
                    bindingContext.ModelName,
                    new string[] { model },
                    model);

                bindingContext.Result =ModelBindingResult.Success(model);
                return TaskCache.CompletedTask;
            }
        }

        private class NullModelBinder : IModelBinder
        {
            public Task BindModelAsync(ModelBindingContext bindingContext)
            {
                if (bindingContext == null)
                {
                    throw new ArgumentNullException(nameof(bindingContext));
                }
                Debug.Assert(bindingContext.Result == null);

                bindingContext.Result =  ModelBindingResult.Success(model: null);
                return TaskCache.CompletedTask;
            }
        }

        private class NullModelNotSetModelBinder : IModelBinder
        {
            public Task BindModelAsync(ModelBindingContext bindingContext)
            {
                if (bindingContext == null)
                {
                    throw new ArgumentNullException(nameof(bindingContext));
                }
                Debug.Assert(bindingContext.Result == null);

                bindingContext.Result = ModelBindingResult.Failed();
                return TaskCache.CompletedTask;
            }
        }

        private class NullResultModelBinder : IModelBinder
        {
            public Task BindModelAsync(ModelBindingContext bindingContext)
            {
                if (bindingContext == null)
                {
                    throw new ArgumentNullException(nameof(bindingContext));
                }
                Debug.Assert(bindingContext.Result == null);

                return TaskCache.CompletedTask;
            }
        }
    }
}