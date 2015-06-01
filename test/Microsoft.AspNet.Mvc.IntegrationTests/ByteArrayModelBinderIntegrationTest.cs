// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public class ByteArrayModelBinderIntegrationTest
    {
        private class Person
        {
            public byte[] Token { get; set; }
        }

        [Theory(Skip = "ModelState.Value not set due to #2445")]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BindProperty_WithData_GetsBound(bool fallBackScenario)
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Person)
            };

            var prefix = fallBackScenario ? string.Empty : "Parameter1";
            var queryStringKey = fallBackScenario ? "Token" : prefix + "." + "Token";

            // any valid base64 string
            var expectedValue = new byte[] { 12, 13 };
            var value = Convert.ToBase64String(expectedValue);
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
                request =>
                {
                    request.QueryString = QueryString.Create(queryStringKey, value);
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
            Assert.NotNull(boundPerson.Token);
            Assert.Equal(expectedValue, boundPerson.Token);

            // ModelState
            Assert.True(modelState.IsValid);

            Assert.Equal(2, modelState.Keys.Count);
            Assert.Single(modelState.Keys, k => k == prefix);
            Assert.Single(modelState.Keys, k => k == queryStringKey);

            var key = Assert.Single(modelState.Keys, k => k == queryStringKey + "[0]");
            Assert.NotNull(modelState[key].Value); // should be non null bug #2445.
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);

            key = Assert.Single(modelState.Keys, k => k == queryStringKey + "[1]");
            Assert.NotNull(modelState[key].Value); // should be non null bug #2445.
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        }

        [Fact]
        public async Task BindParameter_NoData_DoesNotGetBound()
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

                ParameterType = typeof(byte[])
            };

            // No data is passed.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
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

        [Fact(Skip = "ModelState.Value not set due to #2445")]
        public async Task BindParameter_WithData_GetsBound()
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

                ParameterType = typeof(byte[])
            };

            // any valid base64 string
            var value = "four";
            var expectedValue = Convert.FromBase64String(value);
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
                request =>
                {
                    request.QueryString = QueryString.Create("CustomParameter", value);
                });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);
            var model = Assert.IsType<byte[]>(modelBindingResult.Model);

            // Model
            Assert.Equal(expectedValue, model);

            // ModelState
            Assert.True(modelState.IsValid);

            Assert.Equal(3, modelState.Count);
            Assert.Single(modelState.Keys, k => k == "CustomParameter[0]");
            Assert.Single(modelState.Keys, k => k == "CustomParameter[1]");
            var key = Assert.Single(modelState.Keys, k => k == "CustomParameter[2]");

            Assert.NotNull(modelState[key].Value);
            Assert.Equal(value, modelState[key].Value.AttemptedValue);
            Assert.Equal(expectedValue, modelState[key].Value.RawValue);
            Assert.Empty(modelState[key].Errors);
        }
    }
}