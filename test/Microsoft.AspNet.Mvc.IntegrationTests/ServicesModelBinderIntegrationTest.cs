// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public class ServicesModelBinderIntegrationTest
    {
        [Fact]
        public async Task BindParameterFromService_WithData_GetsBound()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "CustomParameter",
                    BindingSource = BindingSource.Services
                },

                // Using a service type already in defaults.
                ParameterType = typeof(JsonOutputFormatter)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var outputFormatter = Assert.IsType<JsonOutputFormatter>(modelBindingResult.Model);
            Assert.NotNull(outputFormatter);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public async Task BindParameterFromService_NoPrefix_GetsBound()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "ControllerProperty",
                BindingInfo = new BindingInfo
                {
                    BindingSource = BindingSource.Services,
                },

                // Use a service type already in defaults.
                ParameterType = typeof(JsonOutputFormatter),
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var outputFormatter = Assert.IsType<JsonOutputFormatter>(modelBindingResult.Model);
            Assert.NotNull(outputFormatter);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
        }

        [Fact]
        public async Task BindEnumerableParameterFromService_NoPrefix_GetsBound()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "ControllerProperty",
                BindingInfo = new BindingInfo
                {
                    BindingSource = BindingSource.Services,
                },

                // Use a service type already in defaults.
                ParameterType = typeof(IEnumerable<JsonOutputFormatter>),
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var formatterArray = Assert.IsType<JsonOutputFormatter[]>(modelBindingResult.Model);
            Assert.Equal(1, formatterArray.Length);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
        }

        [Fact]
        public async Task BindEnumerableParameterFromService_NoService_GetsBound()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "ControllerProperty",
                BindingInfo = new BindingInfo
                {
                    BindingSource = BindingSource.Services,
                },

                // Use a service type not available in DI.
                ParameterType = typeof(IEnumerable<IActionResult>),
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var actionResultArray = Assert.IsType<IActionResult[]>(modelBindingResult.Model);
            Assert.Equal(0, actionResultArray.Length);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
        }

        [Fact]
        public async Task BindParameterFromService_NoService_Throws()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "ControllerProperty",
                BindingInfo = new BindingInfo
                {
                    BindingSource = BindingSource.Services,
                },

                // Use a service type not available in DI.
                ParameterType = typeof(IActionResult),
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
            var modelState = new ModelStateDictionary();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => argumentBinder.BindModelAsync(parameter, modelState, operationContext));
            Assert.Contains(typeof(IActionResult).FullName, exception.Message);
        }
    }
}