// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Controllers;
using Microsoft.AspNet.Mvc.ModelBinding;
#if !DNXCORE50
using Microsoft.AspNet.Testing.xunit;
#endif
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public class SimpleTypeModelBinderIntegrationTest
    {
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
            Assert.Equal("1", modelState[key].AttemptedValue);
            Assert.Equal("1", modelState[key].RawValue);
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
            Assert.Equal("1", modelState[key].AttemptedValue);
            Assert.Equal("1", modelState[key].RawValue);
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
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var model = Assert.IsType<string>(modelBindingResult.Model);
            Assert.Equal("someValue", model);

            // ModelState
            Assert.True(modelState.IsValid);

            Assert.Equal(1, modelState.Keys.Count);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("Parameter1", key);
            Assert.Equal("someValue", modelState[key].AttemptedValue);
            Assert.Equal("someValue", modelState[key].RawValue);
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        }

        [Fact]
        public async Task BindParameter_WithMultipleValues_GetsBoundToFirstValue()
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
                request.QueryString = new QueryString("?Parameter1=someValue&Parameter1=otherValue");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var model = Assert.IsType<string>(modelBindingResult.Model);
            Assert.Equal("someValue", model);

            // ModelState
            Assert.True(modelState.IsValid);

            Assert.Equal(1, modelState.Keys.Count);
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
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),

                ParameterType = typeof(int)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("Parameter1", "abcd");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.False(modelBindingResult.IsModelSet);

            // Model
            Assert.Null(modelBindingResult.Model);

            // ModelState
            Assert.False(modelState.IsValid);
            Assert.Equal(1, modelState.Count);
            Assert.Equal(1, modelState.ErrorCount);

            var key = Assert.Single(modelState.Keys);
            Assert.Equal("Parameter1", key);

            var entry = modelState[key];
            Assert.Equal("abcd", entry.RawValue);
            Assert.Equal("abcd", entry.AttemptedValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

            var error = Assert.Single(entry.Errors);
            Assert.Null(error.Exception);
            Assert.Equal("The value 'abcd' is not valid for Int32.", error.ErrorMessage);
        }

#if DNXCORE50
        [Theory]
#else
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "aspnet/External#50")]
#endif
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
                request.QueryString = QueryString.Create("Parameter1", "  ");
            });
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.False(modelBindingResult.IsModelSet);

            // Model
            Assert.Null(modelBindingResult.Model);

            // ModelState
            Assert.False(modelState.IsValid);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("Parameter1", key);
            Assert.Equal("  ", modelState[key].AttemptedValue);
            Assert.Equal("  ", modelState[key].RawValue);
            var error = Assert.Single(modelState[key].Errors);
            Assert.Equal("The value '  ' is invalid.", error.ErrorMessage, StringComparer.Ordinal);
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
                .BindingDetails((Action<ModelBinding.Metadata.BindingMetadata>)(binding =>
                {
                    // A real details provider could customize message based on BindingMetadataProviderContext.
                    binding.ModelBindingMessageProvider.ValueMustNotBeNullAccessor =
                        value => $"Hurts when '{ value }' is provided.";
                }));
            var argumentBinder = new DefaultControllerActionArgumentBinder(
                metadataProvider,
                ModelBindingTestHelper.GetObjectValidator());
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
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            Assert.Null(modelBindingResult.Model);

            // ModelState
            Assert.True(modelState.IsValid);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("Parameter1", key);
            Assert.Equal(string.Empty, modelState[key].AttemptedValue);
            Assert.Equal(new string[] { string.Empty }, modelState[key].RawValue);
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
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Person),
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.Form = new FormCollection(personStore);
            });
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

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

            Assert.Equal(new[] { "Address.Lines", "Address.Zip", "Name" }, modelState.Keys.ToArray());
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