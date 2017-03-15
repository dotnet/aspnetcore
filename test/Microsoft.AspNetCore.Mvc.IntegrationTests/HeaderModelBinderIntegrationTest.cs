// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
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
            var testContext = ModelBindingTestHelper.GetTestContext();
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);

            // ModelState
            Assert.False(modelState.IsValid);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("CustomParameter.Address.Header", key);
            var error = Assert.Single(modelState[key].Errors);
            Assert.Equal(ValidationAttributeUtil.GetRequiredErrorMessage("Street"), error.ErrorMessage);
        }

        [Fact]
        public async Task BindPropertyFromHeader_WithPrefix_GetsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "prefix",
                },
                ParameterType = typeof(Person)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request => request.Headers.Add("Header", new[] { "someValue" }));
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.NotNull(boundPerson.Address);
            Assert.Equal("someValue", boundPerson.Address.Street);

            // ModelState
            Assert.True(modelState.IsValid);
            var entry = Assert.Single(modelState);
            Assert.Equal("prefix.Address.Header", entry.Key);
            Assert.Empty(entry.Value.Errors);
            Assert.Equal(ModelValidationState.Valid, entry.Value.ValidationState);
            Assert.Equal("someValue", entry.Value.AttemptedValue);
            Assert.Equal(new string[] { "someValue" }, entry.Value.RawValue);
        }

        // The scenario is interesting as we to bind the top level model we fallback to empty prefix,
        // and hence the model state keys have an empty prefix.
        [Fact]
        public async Task BindPropertyFromHeader_WithData_WithEmptyPrefix_GetsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Person)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request => request.Headers.Add("Header", new[] { "someValue" }));
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.NotNull(boundPerson.Address);
            Assert.Equal("someValue", boundPerson.Address.Street);

            // ModelState
            Assert.True(modelState.IsValid);
            var entry = Assert.Single(modelState);
            Assert.Equal("Address.Header", entry.Key);
            Assert.Empty(entry.Value.Errors);
            Assert.Equal(ModelValidationState.Valid, entry.Value.ValidationState);
            Assert.Equal("someValue", entry.Value.AttemptedValue);
            Assert.Equal(new string[] { "someValue" }, entry.Value.RawValue);
        }

        private class ListContainer1
        {
            [FromHeader(Name = "Header")]
            public List<string> ListProperty { get; set; }
        }

        [Fact]
        public async Task BindCollectionPropertyFromHeader_WithData_IsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(ListContainer1),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request => request.Headers.Add("Header", new[] { "someValue" }));
            var modelState = testContext.ModelState;

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(result.IsModelSet);

            // Model
            var boundContainer = Assert.IsType<ListContainer1>(result.Model);
            Assert.NotNull(boundContainer);
            Assert.NotNull(boundContainer.ListProperty);
            var entry = Assert.Single(boundContainer.ListProperty);
            Assert.Equal("someValue", entry);

            // ModelState
            Assert.True(modelState.IsValid);
            var kvp = Assert.Single(modelState);
            Assert.Equal("Header", kvp.Key);
            var modelStateEntry = kvp.Value;
            Assert.NotNull(modelStateEntry);
            Assert.Empty(modelStateEntry.Errors);
            Assert.Equal(ModelValidationState.Valid, modelStateEntry.ValidationState);
            Assert.Equal("someValue", modelStateEntry.AttemptedValue);
            Assert.Equal(new[] { "someValue" }, modelStateEntry.RawValue);
        }

        private class ListContainer2
        {
            [FromHeader(Name = "Header")]
            public List<string> ListProperty { get; } = new List<string> { "One", "Two", "Three" };
        }

        [Fact]
        public async Task BindReadOnlyCollectionPropertyFromHeader_WithData_IsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(ListContainer2),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request => request.Headers.Add("Header", new[] { "someValue" }));
            var modelState = testContext.ModelState;

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(result.IsModelSet);

            // Model
            var boundContainer = Assert.IsType<ListContainer2>(result.Model);
            Assert.NotNull(boundContainer);
            Assert.NotNull(boundContainer.ListProperty);
            var entry = Assert.Single(boundContainer.ListProperty);
            Assert.Equal("someValue", entry);

            // ModelState
            Assert.True(modelState.IsValid);
            var kvp = Assert.Single(modelState);
            Assert.Equal("Header", kvp.Key);
            var modelStateEntry = kvp.Value;
            Assert.NotNull(modelStateEntry);
            Assert.Empty(modelStateEntry.Errors);
            Assert.Equal(ModelValidationState.Valid, modelStateEntry.ValidationState);
            Assert.Equal("someValue", modelStateEntry.AttemptedValue);
            Assert.Equal(new[] { "someValue" }, modelStateEntry.RawValue);
        }

        [Theory]
        [InlineData(typeof(string[]), "value1, value2, value3")]
        [InlineData(typeof(string), "value")]
        public async Task BindParameterFromHeader_WithData_WithPrefix_ModelGetsBound(Type modelType, string value)
        {
            // Arrange
            object expectedValue;
            object expectedRawValue;
            if (modelType == typeof(string))
            {
                expectedValue = value;
                expectedRawValue = new string[] { value };
            }
            else
            {
                expectedValue = value.Split(',').Select(v => v.Trim()).ToArray();
                expectedRawValue = expectedValue;
            }

            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo
                {
                    BinderModelName = "CustomParameter",
                    BindingSource = BindingSource.Header
                },
                ParameterType = modelType
            };

            Action<HttpRequest> action = r => r.Headers.Add("CustomParameter", new[] { value });
            var testContext = ModelBindingTestHelper.GetTestContext(action);

            // Do not add any headers.
            var httpContext = testContext.HttpContext;
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            Assert.NotNull(modelBindingResult.Model);
            Assert.IsType(modelType, modelBindingResult.Model);

            // ModelState
            Assert.True(modelState.IsValid);
            var entry = Assert.Single(modelState);
            Assert.Equal("CustomParameter", entry.Key);
            Assert.Empty(entry.Value.Errors);
            Assert.Equal(ModelValidationState.Valid, entry.Value.ValidationState);
            Assert.Equal(value, entry.Value.AttemptedValue);
            Assert.Equal(expectedRawValue, entry.Value.RawValue);
        }
    }
}