// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    // Integration tests targeting the behavior of the DictionaryModelBinder with other model binders.
    public class DictionaryModelBinderIntegrationTest
    {
        [Fact]
        public async Task DictionaryModelBinder_BindsDictionaryOfSimpleType_WithPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Dictionary<string, int>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter[0].Key=key0&parameter[0].Value=10");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Dictionary<string, int>>(modelBindingResult.Model);
            Assert.Equal(new Dictionary<string, int>() { { "key0", 10 } }, model);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[0].Key").Value;
            Assert.Equal("key0", entry.Value.AttemptedValue);
            Assert.Equal("key0", entry.Value.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[0].Value").Value;
            Assert.Equal("10", entry.Value.AttemptedValue);
            Assert.Equal("10", entry.Value.RawValue);
        }

        [Fact]
        public async Task DictionaryModelBinder_BindsDictionaryOfSimpleType_WithExplicitPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "prefix",
                },
                ParameterType = typeof(Dictionary<string, int>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?prefix[0].Key=key0&prefix[0].Value=10");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Dictionary<string, int>>(modelBindingResult.Model);
            Assert.Equal(new Dictionary<string, int>() { { "key0", 10 }, }, model);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "prefix[0].Key").Value;
            Assert.Equal("key0", entry.Value.AttemptedValue);
            Assert.Equal("key0", entry.Value.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "prefix[0].Value").Value;
            Assert.Equal("10", entry.Value.AttemptedValue);
            Assert.Equal("10", entry.Value.RawValue);
        }

        [Fact]
        public async Task DictionaryModelBinder_BindsDictionaryOfSimpleType_EmptyPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Dictionary<string, int>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?[0].Key=key0&[0].Value=10");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Dictionary<string, int>>(modelBindingResult.Model);
            Assert.Equal(new Dictionary<string, int>() { { "key0", 10 }, }, model);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "[0].Key").Value;
            Assert.Equal("key0", entry.Value.AttemptedValue);
            Assert.Equal("key0", entry.Value.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "[0].Value").Value;
            Assert.Equal("10", entry.Value.AttemptedValue);
            Assert.Equal("10", entry.Value.RawValue);
        }

        [Fact]
        public async Task DictionaryModelBinder_BindsDictionaryOfSimpleType_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Dictionary<string, int>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);
            Assert.Empty(Assert.IsType<Dictionary<string, int>>(modelBindingResult.Model));

            Assert.Equal(0, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class Person
        {
            public int Id { get; set; }
        }

        [Fact]
        public async Task DictionaryModelBinder_BindsDictionaryOfComplexType_WithPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Dictionary<string, Person>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter[0].Key=key0&parameter[0].Value.Id=10");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Dictionary<string, Person>>(modelBindingResult.Model);
            Assert.Equal(1, model.Count);
            Assert.Equal("key0", model.Keys.First());
            Assert.Equal(model.Values, model.Values);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[0].Key").Value;
            Assert.Equal("key0", entry.Value.AttemptedValue);
            Assert.Equal("key0", entry.Value.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[0].Value.Id").Value;
            Assert.Equal("10", entry.Value.AttemptedValue);
            Assert.Equal("10", entry.Value.RawValue);
        }

        [Fact]
        public async Task DictionaryModelBinder_BindsDictionaryOfComplexType_WithExplicitPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "prefix",
                },
                ParameterType = typeof(Dictionary<string, Person>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?prefix[0].Key=key0&prefix[0].Value.Id=10");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Dictionary<string, Person>>(modelBindingResult.Model);
            Assert.Equal(1, model.Count);
            Assert.Equal("key0", model.Keys.First());
            Assert.Equal(model.Values, model.Values);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "prefix[0].Key").Value;
            Assert.Equal("key0", entry.Value.AttemptedValue);
            Assert.Equal("key0", entry.Value.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "prefix[0].Value.Id").Value;
            Assert.Equal("10", entry.Value.AttemptedValue);
            Assert.Equal("10", entry.Value.RawValue);
        }

        [Fact]
        public async Task DictionaryModelBinder_BindsDictionaryOfComplexType_EmptyPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Dictionary<string, Person>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?[0].Key=key0&[0].Value.Id=10");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Dictionary<string, Person>>(modelBindingResult.Model);
            Assert.Equal(1, model.Count);
            Assert.Equal("key0", model.Keys.First());
            Assert.Equal(model.Values, model.Values);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "[0].Key").Value;
            Assert.Equal("key0", entry.Value.AttemptedValue);
            Assert.Equal("key0", entry.Value.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "[0].Value.Id").Value;
            Assert.Equal("10", entry.Value.AttemptedValue);
            Assert.Equal("10", entry.Value.RawValue);
        }

        [Fact]
        public async Task DictionaryModelBinder_BindsDictionaryOfComplexType_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Dictionary<string, Person>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);
            Assert.Empty(Assert.IsType<Dictionary<string, Person>>(modelBindingResult.Model));

            Assert.Equal(0, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }
    }
}