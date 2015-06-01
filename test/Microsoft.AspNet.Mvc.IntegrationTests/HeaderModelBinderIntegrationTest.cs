// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public class HeaderModelBinderIntegrationTest
    {
        private class Person
        {
            public Address Address { get; set; }
        }

        private class Address
        {
            [FromHeader(Name = "Header")]
            [Required]
            public string Street { get; set; }
        }

        [Fact]
        public async Task BindPropertyFromHeader_NoData_UsesFullPathAsKeyForModelStateErrors()
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

            // Do not add any headers.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
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

            // ModelState
            Assert.False(modelState.IsValid);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("CustomParameter.Address.Header", key);
            var error = Assert.Single(modelState[key].Errors);
            Assert.Equal("The Street field is required.", error.ErrorMessage);
        }

        [Fact(Skip = "ModelState.Value not set due to #2445")]
        public async Task BindPropertyFromHeader_WithPrefix_GetsBound()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "prefix",
                },
                ParameterType = typeof(Person)
            };

            // Do not add any headers.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request => {
                request.Headers.Add("Header", new[] { "someValue" });
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
            Assert.Equal("someValue", boundPerson.Address.Street);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Equal(3, modelState.Count);
            Assert.Single(modelState.Keys, k => k == "prefix.Address");
            Assert.Single(modelState.Keys, k => k == "prefix");

            var key = Assert.Single(modelState.Keys, k => k == "prefix.Address.Header");
            Assert.NotNull(modelState[key].Value);
            Assert.Equal("someValue", modelState[key].Value.RawValue);
            Assert.Equal("someValue", modelState[key].Value.AttemptedValue);
        }

        // The scenario is interesting as we to bind the top level model we fallback to empty prefix,
        // and hence the model state keys have an empty prefix.
        [Fact(Skip = "ModelState.Value not set due to #2445.")]
        public async Task BindPropertyFromHeader_WithData_WithEmptyPrefix_GetsBound()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Person)
            };

            // Do not add any headers.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request => {
                request.Headers.Add("Header", new[] { "someValue" });
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
            Assert.Equal("someValue", boundPerson.Address.Street);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Equal(2, modelState.Count);
            Assert.Single(modelState.Keys, k => k == "Address");
            var key = Assert.Single(modelState.Keys, k => k == "Address.Header");
            Assert.NotNull(modelState[key].Value);
            Assert.Equal("someValue", modelState[key].Value.RawValue);
            Assert.Equal("someValue", modelState[key].Value.AttemptedValue);
        }

        [Theory(Skip = "Greedy Model Binders should add a value in model state #2445.")]
        [InlineData(typeof(string[]), "value1, value2, value3")]
        [InlineData(typeof(string), "value")]
        public async Task BindParameterFromHeader_WithData_WithPrefix_ModelGetsBound(Type modelType, string value)
        {
            // Arrange
            var expectedValue = value.Split(',').Select(v => v.Trim());

            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "CustomParameter",
                    BindingSource = BindingSource.Header
                },
                ParameterType = modelType
            };

            Action<HttpRequest> action = (r) => r.Headers.Add("CustomParameter", new[] { value });
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(action);

            // Do not add any headers.
            var httpContext = operationContext.HttpContext;
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            Assert.NotNull(modelBindingResult.Model);
            Assert.IsType(modelType, modelBindingResult.Model);

            // ModelState
            Assert.True(modelState.IsValid);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("CustomParameter", key);

            Assert.NotNull(modelState[key].Value);
            Assert.Equal(expectedValue, modelState[key].Value.RawValue);
            Assert.Equal(value, modelState[key].Value.AttemptedValue);
        }
    }
}