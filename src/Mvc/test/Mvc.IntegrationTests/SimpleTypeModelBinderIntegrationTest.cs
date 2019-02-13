// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    public class SimpleTypeModelBinderIntegrationTest
    {
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
                    BinderModelName = "CustomParameter",
                },

                ParameterType = typeof(Person)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = QueryString.Create("CustomParameter.Address.Zip", "1");
            });

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
            Assert.Equal(1, boundPerson.Address.Zip);

            // ModelState
            Assert.True(modelState.IsValid);

            Assert.Single(modelState.Keys);
            var key = Assert.Single(modelState.Keys, k => k == "CustomParameter.Address.Zip");
            Assert.Equal("1", modelState[key].AttemptedValue);
            Assert.Equal("1", modelState[key].RawValue);
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        }

        [Fact]
        public async Task BindProperty_WithData_WithEmptyPrefix_GetsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Person)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = QueryString.Create("Address.Zip", "1");
            });

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
            Assert.Equal(1, boundPerson.Address.Zip);

            // ModelState
            Assert.True(modelState.IsValid);

            Assert.Single(modelState.Keys);
            var key = Assert.Single(modelState.Keys, k => k == "Address.Zip");
            Assert.Equal("1", modelState[key].AttemptedValue);
            Assert.Equal("1", modelState[key].RawValue);
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        }

        [Fact]
        public async Task BindParameter_WithData_GetsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),

                ParameterType = typeof(string)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = QueryString.Create("Parameter1", "someValue");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var model = Assert.IsType<string>(modelBindingResult.Model);
            Assert.Equal("someValue", model);

            // ModelState
            Assert.True(modelState.IsValid);

            Assert.Single(modelState.Keys);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("Parameter1", key);
            Assert.Equal("someValue", modelState[key].AttemptedValue);
            Assert.Equal("someValue", modelState[key].RawValue);
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        }

        [Fact]
        [ReplaceCulture("en-GB", "en-GB")]
        public async Task BindDecimalParameter_WithData_GetsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(decimal),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = QueryString.Create("Parameter1", "32,000.99");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var model = Assert.IsType<decimal>(modelBindingResult.Model);
            Assert.Equal(32000.99M, model);

            // ModelState
            Assert.True(modelState.IsValid);

            Assert.Single(modelState.Keys);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("Parameter1", key);
            Assert.Equal("32,000.99", modelState[key].AttemptedValue);
            Assert.Equal("32,000.99", modelState[key].RawValue);
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        }

        [Fact]
        public async Task BindParameter_WithMultipleValues_GetsBoundToFirstValue()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),

                ParameterType = typeof(string)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?Parameter1=someValue&Parameter1=otherValue");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var model = Assert.IsType<string>(modelBindingResult.Model);
            Assert.Equal("someValue", model);

            // ModelState
            Assert.True(modelState.IsValid);

            Assert.Single(modelState.Keys);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("Parameter1", key);
            Assert.Equal("someValue,otherValue", modelState[key].AttemptedValue);
            Assert.Equal(new string[] { "someValue", "otherValue" }, modelState[key].RawValue);
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        }

        [Fact]
        public async Task BindParameter_NonConvertableValue_GetsError()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),

                ParameterType = typeof(int)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = QueryString.Create("Parameter1", "abcd");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.False(modelBindingResult.IsModelSet);

            // Model
            Assert.Null(modelBindingResult.Model);

            // ModelState
            Assert.False(modelState.IsValid);
            Assert.Single(modelState);
            Assert.Equal(1, modelState.ErrorCount);

            var key = Assert.Single(modelState.Keys);
            Assert.Equal("Parameter1", key);

            var entry = modelState[key];
            Assert.Equal("abcd", entry.RawValue);
            Assert.Equal("abcd", entry.AttemptedValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

            var error = Assert.Single(entry.Errors);
            Assert.Null(error.Exception);
            Assert.Equal("The value 'abcd' is not valid.", error.ErrorMessage);
        }

        [Fact]
        public async Task BindParameter_NonConvertableValue_GetsCustomErrorMessage()
        {
            // Arrange
            var parameterType = typeof(int);
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider
                .ForType(parameterType)
                .BindingDetails(binding =>
                {
                    // A real details provider could customize message based on BindingMetadataProviderContext.
                    binding.ModelBindingMessageProvider.SetNonPropertyAttemptedValueIsInvalidAccessor(
                        (value) => $"Hmm, '{ value }' is not a valid value.");
                });

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = QueryString.Create("Parameter1", "abcd");
                },
                metadataProvider: metadataProvider);

            var modelState = testContext.ModelState;
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = parameterType
            };

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.False(modelBindingResult.IsModelSet);

            // Model
            Assert.Null(modelBindingResult.Model);

            // ModelState
            Assert.False(modelState.IsValid);
            Assert.Single(modelState);
            Assert.Equal(1, modelState.ErrorCount);

            var key = Assert.Single(modelState.Keys);
            Assert.Equal("Parameter1", key);

            var entry = modelState[key];
            Assert.Equal("abcd", entry.RawValue);
            Assert.Equal("abcd", entry.AttemptedValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

            var error = Assert.Single(entry.Errors);
            Assert.Null(error.Exception);
            Assert.Equal($"Hmm, 'abcd' is not a valid value.", error.ErrorMessage);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(bool))]
        public async Task BindParameter_WithEmptyData_DoesNotBind(Type parameterType)
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),

                ParameterType = parameterType
            };
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = QueryString.Create("Parameter1", "");
            });
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.False(modelBindingResult.IsModelSet);

            // Model
            Assert.Null(modelBindingResult.Model);

            // ModelState
            Assert.False(modelState.IsValid);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("Parameter1", key);
            Assert.Equal("", modelState[key].AttemptedValue);
            Assert.Equal("", modelState[key].RawValue);
            var error = Assert.Single(modelState[key].Errors);
            Assert.Equal("The value '' is invalid.", error.ErrorMessage, StringComparer.Ordinal);
            Assert.Null(error.Exception);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(bool))]
        public async Task BindParameter_WithEmptyData_AndPerTypeMessage_AddsGivenMessage(Type parameterType)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider
                .ForType(parameterType)
                .BindingDetails(binding =>
                {
                    // A real details provider could customize message based on BindingMetadataProviderContext.
                    binding.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(
                        value => $"Hurts when '{ value }' is provided.");
                });

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = QueryString.Create("Parameter1", string.Empty);
                },
                metadataProvider: metadataProvider);

            var modelState = testContext.ModelState;
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),

                ParameterType = parameterType
            };

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            // ModelBindingResult
            Assert.False(modelBindingResult.IsModelSet);

            // Model
            Assert.Null(modelBindingResult.Model);

            // ModelState
            Assert.False(modelState.IsValid);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("Parameter1", key);
            Assert.Equal(string.Empty, modelState[key].AttemptedValue);
            Assert.Equal(string.Empty, modelState[key].RawValue);
            var error = Assert.Single(modelState[key].Errors);
            Assert.Equal("Hurts when '' is provided.", error.ErrorMessage, StringComparer.Ordinal);
            Assert.Null(error.Exception);
        }

        [Theory]
        [InlineData(typeof(int?))]
        [InlineData(typeof(bool?))]
        [InlineData(typeof(string))]
        public async Task BindParameter_WithEmptyData_BindsReferenceAndNullableObjects(Type parameterType)
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),

                ParameterType = parameterType
            };
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = QueryString.Create("Parameter1", string.Empty);
            });
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            Assert.Null(modelBindingResult.Model);

            // ModelState
            Assert.True(modelState.IsValid);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("Parameter1", key);
            Assert.Equal(string.Empty, modelState[key].AttemptedValue);
            Assert.Equal(string.Empty, modelState[key].RawValue);
            Assert.Empty(modelState[key].Errors);
        }

        [Fact]
        public async Task BindParameter_NoData_Fails()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),

                ParameterType = typeof(string)
            };

            // No Data.
            var testContext = ModelBindingTestHelper.GetTestContext();

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.Equal(ModelBindingResult.Failed(), modelBindingResult);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState.Keys);
        }

        public static TheoryData<IDictionary<string, StringValues>> PersonStoreData
        {
            get
            {
                return new TheoryData<IDictionary<string, StringValues>>
                {
                    new Dictionary<string, StringValues>
                    {
                        { "name", new[] { "Fred" } },
                        { "address.zip", new[] { "98052" } },
                        { "address.lines", new[] { "line 1", "line 2" } },
                    },
                    new Dictionary<string, StringValues>
                    {
                        { "address.lines[]", new[] { "line 1", "line 2" } },
                        { "address[].zip", new[] { "98052" } },
                        { "name[]", new[] { "Fred" } },
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(PersonStoreData))]
        public async Task BindParameter_FromFormData_BindsCorrectly(Dictionary<string, StringValues> personStore)
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Person),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.Form = new FormCollection(personStore);
            });
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.Equal("Fred", boundPerson.Name);
            Assert.NotNull(boundPerson.Address);
            Assert.Equal(new[] { "line 1", "line 2" }, boundPerson.Address.Lines);
            Assert.Equal(98052, boundPerson.Address.Zip);

            // ModelState
            Assert.True(modelState.IsValid);

            Assert.Equal(new[] { "Address.Lines", "Address.Zip", "Name" }, modelState.Keys.OrderBy(p => p).ToArray());
            var entry = modelState["Address.Lines"];
            Assert.NotNull(entry);
            Assert.Empty(entry.Errors);
            Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
            Assert.Equal("line 1,line 2", entry.AttemptedValue);
            Assert.Equal(new[] { "line 1", "line 2" }, entry.RawValue);
        }

        private class Person
        {
            public Address Address { get; set; }

            public string Name { get; set; }
        }

        private class Address
        {
            public string[] Lines { get; set; }

            public int Zip { get; set; }
        }
    }
}
