// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public class TypeConverterModelBinderIntegrationTest
    {
        private class Person
        {
            public Address Address { get; set; }
        }

        private class Address
        {
            public int Zip { get; set; }
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
                    BinderModelName = "CustomParameter",
                },

                ParameterType = typeof(Person)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("CustomParameter.Address.Zip", "1");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.NotNull(boundPerson.Address);
            Assert.Equal(1, boundPerson.Address.Zip);

            // ModelState
            Assert.True(modelState.IsValid);

            Assert.Equal(1, modelState.Keys.Count);
            var key = Assert.Single(modelState.Keys, k => k == "CustomParameter.Address.Zip");
            Assert.NotNull(modelState[key].Value);
            Assert.Equal("1", modelState[key].Value.AttemptedValue);
            Assert.Equal("1", modelState[key].Value.RawValue);
            Assert.NotNull(modelState[key].Value);
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        }

        [Fact]
        public async Task BindProperty_WithData_WithEmptyPrefix_GetsBound()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Person)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("Address.Zip", "1");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.NotNull(boundPerson.Address);
            Assert.Equal(1, boundPerson.Address.Zip);

            // ModelState
            Assert.True(modelState.IsValid);

            Assert.Equal(1, modelState.Keys.Count);
            var key = Assert.Single(modelState.Keys, k => k == "Address.Zip");
            Assert.NotNull(modelState[key].Value);
            Assert.Equal("1", modelState[key].Value.AttemptedValue);
            Assert.Equal("1", modelState[key].Value.RawValue);
            Assert.NotNull(modelState[key].Value);
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        }

        [Fact]
        public async Task BindParameter_WithData_GetsBound()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),

                ParameterType = typeof(string)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("Parameter1", "someValue");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var model = Assert.IsType<string>(modelBindingResult.Model);
            Assert.Equal("someValue", model);

            // ModelState
            Assert.True(modelState.IsValid);

            Assert.Equal(1, modelState.Keys.Count);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("Parameter1", key);
            Assert.NotNull(modelState[key].Value);
            Assert.Equal("someValue", modelState[key].Value.AttemptedValue);
            Assert.Equal("someValue", modelState[key].Value.RawValue);
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(bool))]
        public async Task BindParameter_WithEmptyData_DoesNotBind(Type parameterType)
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),

                ParameterType = parameterType
            };
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("Parameter1", string.Empty);
            });
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.NotNull(modelBindingResult);
            Assert.False(modelBindingResult.IsModelSet);

            // Model
            Assert.Null(modelBindingResult.Model);

            // ModelState
            Assert.False(modelState.IsValid);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("Parameter1", key);
            Assert.Equal(string.Empty, modelState[key].Value.AttemptedValue);
            Assert.Equal(string.Empty, modelState[key].Value.RawValue);
            var error = Assert.Single(modelState[key].Errors);
            Assert.Equal(error.ErrorMessage, "The value '' is invalid.", StringComparer.Ordinal);
            Assert.Null(error.Exception);
        }

        [InlineData(typeof(int?))]
        [InlineData(typeof(bool?))]
        [InlineData(typeof(string))]
        [InlineData(typeof(object))]
        [InlineData(typeof(IEnumerable))]
        public async Task BindParameter_WithEmptyData_BindsMutableAndNullableObjects(Type parameterType)
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),

                ParameterType = parameterType
            };
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("Parameter1", string.Empty);
            });
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            Assert.Null(modelBindingResult.Model);

            // ModelState
            Assert.True(modelState.IsValid);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("Parameter1", key);
            Assert.Equal(string.Empty, modelState[key].Value.AttemptedValue);
            Assert.Equal(string.Empty, modelState[key].Value.RawValue);
            Assert.Empty(modelState[key].Errors);
        }

        [Fact]
        public async Task BindParameter_NoData_DoesNotGetBound()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),

                ParameterType = typeof(string)
            };

            // No Data.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.Null(modelBindingResult);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState.Keys);
        }
    }
}