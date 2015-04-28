// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public class CancellationTokenModelBinderIntegrationTest
    {
        private class Person
        {
            public CancellationToken Token { get; set; }
        }

        [Fact(Skip = "CancellationToken should not be validated #2447.")]
        public async Task BindProperty_WithData__WithPrefix_GetsBound()
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

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(httpContext => { });
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

            // ModelState
            Assert.True(modelState.IsValid);

            Assert.Equal(2, modelState.Keys.Count);
            Assert.Single(modelState.Keys, k => k == "CustomParameter");

            var key = Assert.Single(modelState.Keys, k => k == "CustomParameter.Token");
            Assert.Null(modelState[key].Value);
            Assert.Empty(modelState[key].Errors);

            // This Assert Fails.
            Assert.Equal(ModelValidationState.Skipped, modelState[key].ValidationState);
        }

        [Fact(Skip = "CancellationToken should not be validated #2447,Extra entries in model state dictionary. #2466")]
        public async Task BindProperty_WithData__WithEmptyPrefix_GetsBound()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Person)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(httpContext => { });
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

            // ModelState
            Assert.True(modelState.IsValid);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("Token", key);
            Assert.Null(modelState[key].Value);
            Assert.Empty(modelState[key].Errors);

            // This Assert Fails.
            Assert.Equal(ModelValidationState.Skipped, modelState[key].ValidationState);
        }

        [Fact(Skip = "CancellationToken should not be validated #2447.")]
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

                ParameterType = typeof(CancellationToken)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(httpContext => { });
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var token = Assert.IsType<CancellationToken>(modelBindingResult.Model);
            Assert.NotNull(token);

            // ModelState
            Assert.True(modelState.IsValid);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("CustomParameter", key);
            Assert.Null(modelState[key].Value);
            Assert.Empty(modelState[key].Errors);

            // This assert fails.
            Assert.Equal(ModelValidationState.Skipped, modelState[key].ValidationState);
        }
    }
}