// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    // Integration tests targeting the behavior of the KeyValuePairModelBinder with other model binders.
    public class KeyValuePairModelBinderIntegrationTest
    {
        [Fact]
        public async Task KeyValuePairModelBinder_BindsKeyValuePairOfSimpleType_WithPrefix_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(KeyValuePair<string, int>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Key=key0&parameter.Value=10");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<KeyValuePair<string, int>>(modelBindingResult.Model);
            Assert.Equal(new KeyValuePair<string, int>("key0", 10), model);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "parameter.Key").Value;
            Assert.Equal("key0", entry.AttemptedValue);
            Assert.Equal("key0", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "parameter.Value").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
        }

        [Fact]
        public async Task KeyValuePairModelBinder_SimpleTypes_WithNoKey_AddsError()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "parameter",
                ParameterType = typeof(KeyValuePair<string, int>)
            };
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Value=10");
            });
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.False(modelBindingResult.IsModelSet);
            Assert.Equal(2, modelState.Count);

            Assert.False(modelState.IsValid);
            Assert.Equal(1, modelState.ErrorCount);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "parameter.Key").Value;
            var error = Assert.Single(entry.Errors);
            Assert.Null(error.Exception);
            Assert.Equal("A value is required.", error.ErrorMessage);

            entry = Assert.Single(modelState, kvp => kvp.Key == "parameter.Value").Value;
            Assert.Empty(entry.Errors);
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
        }

        [Fact]
        public async Task KeyValuePairModelBinder_SimpleTypes_WithNoKey_AndCustomizedMessage_AddsGivenMessage()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider
                .ForType(typeof(KeyValuePair<string, int>))
                .BindingDetails((System.Action<ModelBinding.Metadata.BindingMetadata>)(binding =>
                {
                    // A real details provider could customize message based on BindingMetadataProviderContext.
                    binding.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(
                        () => $"Hurts when nothing is provided.");
                }));

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = new QueryString("?parameter.Value=10");
                },
                metadataProvider: metadataProvider);

            var modelState = testContext.ModelState;
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
            var parameter = new ParameterDescriptor
            {
                Name = "parameter",
                ParameterType = typeof(KeyValuePair<string, int>)
            };

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.False(modelBindingResult.IsModelSet);
            Assert.Equal(2, modelState.Count);

            Assert.False(modelState.IsValid);
            Assert.Equal(1, modelState.ErrorCount);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "parameter.Key").Value;
            var error = Assert.Single(entry.Errors);
            Assert.Null(error.Exception);
            Assert.Equal("Hurts when nothing is provided.", error.ErrorMessage);

            entry = Assert.Single(modelState, kvp => kvp.Key == "parameter.Value").Value;
            Assert.Empty(entry.Errors);
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
        }

        [Fact]
        public async Task KeyValuePairModelBinder_SimpleTypes_WithNoValue_AddsError()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "parameter",
                ParameterType = typeof(KeyValuePair<string, int>)
            };
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Key=10");
            });
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.False(modelBindingResult.IsModelSet);
            Assert.Equal(2, modelState.Count);

            Assert.False(modelState.IsValid);
            Assert.Equal(1, modelState.ErrorCount);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "parameter.Key").Value;
            Assert.Empty(entry.Errors);
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "parameter.Value").Value;
            var error = Assert.Single(entry.Errors);
            Assert.Null(error.Exception);
            Assert.Equal("A value is required.", error.ErrorMessage);
        }

        [Fact]
        public async Task KeyValuePairModelBinder_SimpleTypes_WithNoValue_AndCustomizedMessage_AddsGivenMessage()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider
                .ForType(typeof(KeyValuePair<string, int>))
                .BindingDetails((System.Action<ModelBinding.Metadata.BindingMetadata>)(binding =>
                {
                    // A real details provider could customize message based on BindingMetadataProviderContext.
                    binding.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(
                        () => $"Hurts when nothing is provided.");
                }));

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = new QueryString("?parameter.Key=10");
                },
                metadataProvider: metadataProvider);

            var modelState = testContext.ModelState;
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
            var parameter = new ParameterDescriptor
            {
                Name = "parameter",
                ParameterType = typeof(KeyValuePair<string, int>)
            };

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.False(modelBindingResult.IsModelSet);
            Assert.Equal(2, modelState.Count);

            Assert.False(modelState.IsValid);
            Assert.Equal(1, modelState.ErrorCount);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "parameter.Key").Value;
            Assert.Empty(entry.Errors);
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "parameter.Value").Value;
            var error = Assert.Single(entry.Errors);
            Assert.Null(error.Exception);
            Assert.Equal("Hurts when nothing is provided.", error.ErrorMessage);
        }

        [Fact]
        public async Task KeyValuePairModelBinder_BindsKeyValuePairOfSimpleType_WithExplicitPrefix_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "prefix",
                },
                ParameterType = typeof(KeyValuePair<string, int>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?prefix.Key=key0&prefix.Value=10");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<KeyValuePair<string, int>>(modelBindingResult.Model);
            Assert.Equal(new KeyValuePair<string, int>("key0", 10), model);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "prefix.Key").Value;
            Assert.Equal("key0", entry.AttemptedValue);
            Assert.Equal("key0", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "prefix.Value").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
        }

        [Fact]
        public async Task KeyValuePairModelBinder_BindsKeyValuePairOfSimpleType_EmptyPrefix_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(KeyValuePair<string, int>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?Key=key0&Value=10");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<KeyValuePair<string, int>>(modelBindingResult.Model);
            Assert.Equal(new KeyValuePair<string, int>("key0", 10), model);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "Key").Value;
            Assert.Equal("key0", entry.AttemptedValue);
            Assert.Equal("key0", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "Value").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
        }

        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/11813")]
        public async Task KeyValuePairModelBinder_BindsKeyValuePairOfSimpleType_NoData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(KeyValuePair<string, int>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            Assert.Equal(new KeyValuePair<string, int>(), modelBindingResult.Model);

            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "Key").Value;
            Assert.Single(entry.Errors);
        }

        private class Person
        {
            public int Id { get; set; }
        }

        [Fact]
        public async Task KeyValuePairModelBinder_BindsKeyValuePairOfComplexType_WithPrefix_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(KeyValuePair<string, Person>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Key=key0&parameter.Value.Id=10");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<KeyValuePair<string, Person>>(modelBindingResult.Model);
            Assert.Equal("key0", model.Key);
            Assert.Equal(10, model.Value.Id);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "parameter.Key").Value;
            Assert.Equal("key0", entry.AttemptedValue);
            Assert.Equal("key0", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "parameter.Value.Id").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
        }

        [Fact]
        public async Task KeyValuePairModelBinder_BindsKeyValuePairOfComplexType_WithExplicitPrefix_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "prefix",
                },
                ParameterType = typeof(KeyValuePair<string, Person>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?prefix.Key=key0&prefix.Value.Id=10");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<KeyValuePair<string, Person>>(modelBindingResult.Model);
            Assert.Equal("key0", model.Key);
            Assert.Equal(10, model.Value.Id);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "prefix.Key").Value;
            Assert.Equal("key0", entry.AttemptedValue);
            Assert.Equal("key0", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "prefix.Value.Id").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
        }

        [Fact]
        public async Task KeyValuePairModelBinder_BindsKeyValuePairOfComplexType_EmptyPrefix_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(KeyValuePair<string, Person>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?Key=key0&Value.Id=10");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<KeyValuePair<string, Person>>(modelBindingResult.Model);
            Assert.Equal("key0", model.Key);
            Assert.Equal(10, model.Value.Id);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "Key").Value;
            Assert.Equal("key0", entry.AttemptedValue);
            Assert.Equal("key0", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "Value.Id").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
        }

        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/11813")]
        public async Task KeyValuePairModelBinder_BindsKeyValuePairOfComplexType_NoData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(KeyValuePair<string, Person>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            Assert.Equal(new KeyValuePair<string, Person>(), modelBindingResult.Model);

            Assert.Equal(2, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "Key").Value;
            Assert.Single(entry.Errors);

            entry = Assert.Single(modelState, kvp => kvp.Key == "Value").Value;
            Assert.Single(entry.Errors);
        }

        [Fact]
        public async Task KeyValuePairModelBinder_BindsKeyValuePairOfArray_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "p",
                ParameterType = typeof(KeyValuePair<string, string[]>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?p.Key=key1&p.Value[0]=value1&p.Value[1]=value2");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var model = Assert.IsType<KeyValuePair<string, string[]>>(modelBindingResult.Model);
            Assert.Equal("key1", model.Key);
            Assert.Equal(new[] { "value1", "value2" }, model.Value);

            Assert.Equal(3, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "p.Key").Value;
            Assert.Equal("key1", entry.AttemptedValue);
            Assert.Equal("key1", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "p.Value[0]").Value;
            Assert.Equal("value1", entry.AttemptedValue);
            Assert.Equal("value1", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "p.Value[1]").Value;
            Assert.Equal("value2", entry.AttemptedValue);
            Assert.Equal("value2", entry.RawValue);
        }
    }
}
