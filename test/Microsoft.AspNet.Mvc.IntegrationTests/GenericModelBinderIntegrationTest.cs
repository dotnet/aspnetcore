// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    // Integration tests targeting the behavior of the GenericModelBinder and related classes
    // with other model binders.
    public class GenericModelBinderIntegrationTest
    {
        // This isn't an especially useful scenario - but it exercises what happens when you
        // try to use a Collection of something that is bound greedily by model-type.
        //
        // In this example we choose IFormCollection because IFormCollection has a dedicated
        // model binder.
        [Fact]
        public async Task GenericModelBinder_BindsCollection_ElementTypeFromGreedyModelBinder_WithPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<IFormCollection>)
            };

            // Need to have a key here so that the GenericModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter.index=10");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<IFormCollection>>(modelBindingResult.Model);
            var formCollection = Assert.Single(model);
            Assert.NotNull(formCollection);

            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
        }

        // This isn't an especially useful scenario - but it exercises what happens when you
        // try to use a Collection of something that is bound greedily by model-type.
        //
        // In this example we choose IFormCollection - because IFormCollection has a dedicated
        // model  binder.
        [Fact]
        public async Task GenericModelBinder_BindsCollection_ElementTypeFromGreedyModelBinder_EmptyPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<IFormCollection>)
            };
            // Need to have a key here so that the GenericModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?index=10");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<IFormCollection>>(modelBindingResult.Model);
            var formCollection = Assert.Single(model);
            Assert.NotNull(formCollection);

            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
        }

        // This isn't an especially useful scenario - but it exercises what happens when you
        // try to use a Collection of something that is bound greedily by model-type.
        //
        // In this example we choose IFormCollection - because IFormCollection has a dedicated
        // model  binder.
        [Fact]
        public async Task GenericModelBinder_BindsCollection_ElementTypeFromGreedyModelBinder_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<IFormCollection>)
            };

            // Without a key here so the GenericModelBinder will not recurse to bind elements.
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

            var model = Assert.IsType<List<IFormCollection>>(modelBindingResult.Model);
            Assert.Empty(model);
        }

        [BindAddress]
        private class Address
        {
        }

        private class BindAddressAttribute : Attribute, IBindingSourceMetadata
        {
            public static readonly BindingSource Source = new BindingSource(
                "Address",
                displayName: "Address",
                isGreedy: true,
                isFromRequest: true);

            public BindingSource BindingSource
            {
                get
                {
                    return Source;
                }
            }
        }

        private class AddressBinder : BindingSourceModelBinder
        {
            public AddressBinder()
                : base(BindAddressAttribute.Source)
            {
            }

            protected override Task<ModelBindingResult> BindModelCoreAsync(ModelBindingContext bindingContext)
            {
                return Task.FromResult(new ModelBindingResult(
                    new Address(),
                    bindingContext.ModelName,
                    isModelSet: true));
            }
        }

        // This isn't an especially useful scenario - but it exercises what happens when you
        // try to use a Collection of something that is bound greedily by binding source.
        [Fact]
        public async Task GenericModelBinder_BindsCollection_ElementTypeUsesGreedyModelBinder_WithPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Address[])
            };

            // Need to have a key here so that the GenericModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
                request => request.QueryString = new QueryString("?parameter.index=0"),
                options => options.ModelBinders.Add(new AddressBinder()));

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Address[]>(modelBindingResult.Model);
            Assert.Equal(1, model.Length);
            Assert.NotNull(model[0]);

            Assert.Equal(0, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        // Similar to the GenericModelBinder_BindsCollection_ElementTypeUsesGreedyModelBinder_WithPrefix_Success
        // scenario but mis-configured. Model using a BindingSource for which no ModelBinder is enabled.
        [Fact]
        public async Task GenericModelBinder_BindsCollection_ElementTypeUsesGreedyBindingSource_WithPrefix_NullElement()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Address[])
            };

            // Need to have a key here so that the GenericModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
                request => request.QueryString = new QueryString("?parameter.index=0"));

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Address[]>(modelBindingResult.Model);
            Assert.Equal(1, model.Length);
            Assert.Null(model[0]);

            Assert.Equal(0, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        // This is part of a random sampling of scenarios where a GenericModelBinder is used
        // recursively.
        [Fact]
        public async Task GenericModelBinder_BindsArrayOfDictionary_WithPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Dictionary<string, int>[])
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter[0][0].Key=key0&parameter[0][0].Value=10");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Dictionary<string, int>[]>(modelBindingResult.Model);
            var dictionary = Assert.Single(model);
            var kvp = Assert.Single(dictionary);
            Assert.Equal("key0", kvp.Key);
            Assert.Equal(10, kvp.Value);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter[0][0].Key").Value;
            Assert.Equal("key0", entry.AttemptedValue);
            Assert.Equal("key0", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "parameter[0][0].Value").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
        }

        // This is part of a random sampling of scenarios where a GenericModelBinder is used
        // recursively.
        [Fact]
        public async Task GenericModelBinder_BindsArrayOfDictionary_EmptyPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Dictionary<string, int>[])
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?[0][0].Key=key0&[0][0].Value=10");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Dictionary<string, int>[]>(modelBindingResult.Model);
            var dictionary = Assert.Single(model);
            var kvp = Assert.Single(dictionary);
            Assert.Equal("key0", kvp.Key);
            Assert.Equal(10, kvp.Value);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "[0][0].Key").Value;
            Assert.Equal("key0", entry.AttemptedValue);
            Assert.Equal("key0", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "[0][0].Value").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
        }

        // This is part of a random sampling of scenarios where a GenericModelBinder is used
        // recursively.
        [Fact]
        public async Task GenericModelBinder_BindsArrayOfDictionary_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Dictionary<string, int>[])
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

            var model = Assert.IsType<Dictionary<string, int>[]>(modelBindingResult.Model);
            Assert.NotNull(model);
            Assert.Empty(model);

            Assert.Equal(0, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        // This is part of a random sampling of scenarios where a GenericModelBinder is used
        // recursively.
        [Fact]
        public async Task GenericModelBinder_BindsCollectionOfKeyValuePair_WithPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(ICollection<KeyValuePair<string, int>>)
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

            var model = Assert.IsType<List<KeyValuePair<string, int>>>(modelBindingResult.Model);
            var kvp = Assert.Single(model);
            Assert.Equal("key0", kvp.Key);
            Assert.Equal(10, kvp.Value);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter[0].Key").Value;
            Assert.Equal("key0", entry.AttemptedValue);
            Assert.Equal("key0", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "parameter[0].Value").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
        }

        // This is part of a random sampling of scenarios where a GenericModelBinder is used
        // recursively.
        [Fact]
        public async Task GenericModelBinder_BindsCollectionOfKeyValuePair_EmptyPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(ICollection<KeyValuePair<string, int>>)
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

            var model = Assert.IsType<List<KeyValuePair<string, int>>>(modelBindingResult.Model);
            var kvp = Assert.Single(model);
            Assert.Equal("key0", kvp.Key);
            Assert.Equal(10, kvp.Value);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "[0].Key").Value;
            Assert.Equal("key0", entry.AttemptedValue);
            Assert.Equal("key0", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "[0].Value").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
        }

        // This is part of a random sampling of scenarios where a GenericModelBinder is used
        // recursively.
        [Fact]
        public async Task GenericModelBinder_BindsCollectionOfKeyValuePair_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(ICollection<KeyValuePair<string, int>>)
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

            var model = Assert.IsType<List<KeyValuePair<string, int>>>(modelBindingResult.Model);
            Assert.NotNull(model);
            Assert.Empty(model);

            Assert.Equal(0, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        // This is part of a random sampling of scenarios where a GenericModelBinder is used
        // recursively.
        [Fact]
        public async Task GenericModelBinder_BindsDictionaryOfList_WithPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Dictionary<string, List<int>>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString(
                    "?parameter[0].Key=key0&parameter[0].Value[0]=10&parameter[0].Value[1]=11");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Dictionary<string, List<int>>>(modelBindingResult.Model);
            var kvp = Assert.Single(model);
            Assert.Equal("key0", kvp.Key);
            Assert.Equal(new List<int>() { 10, 11 }, kvp.Value);

            Assert.Equal(3, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter[0].Key").Value;
            Assert.Equal("key0", entry.AttemptedValue);
            Assert.Equal("key0", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "parameter[0].Value[0]").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "parameter[0].Value[1]").Value;
            Assert.Equal("11", entry.AttemptedValue);
            Assert.Equal("11", entry.RawValue);
        }

        // This is part of a random sampling of scenarios where a GenericModelBinder is used
        // recursively.
        [Fact]
        public async Task GenericModelBinder_BindsDictionaryOfList_EmptyPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Dictionary<string, List<int>>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?[0].Key=key0&[0].Value[0]=10&[0].Value[1]=11");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Dictionary<string, List<int>>>(modelBindingResult.Model);
            var kvp = Assert.Single(model);
            Assert.Equal("key0", kvp.Key);
            Assert.Equal(new List<int>() { 10, 11 }, kvp.Value);

            Assert.Equal(3, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "[0].Key").Value;
            Assert.Equal("key0", entry.AttemptedValue);
            Assert.Equal("key0", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "[0].Value[0]").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "[0].Value[1]").Value;
            Assert.Equal("11", entry.AttemptedValue);
            Assert.Equal("11", entry.RawValue);
        }

        // This is part of a random sampling of scenarios where a GenericModelBinder is used
        // recursively.
        [Fact]
        public async Task GenericModelBinder_BindsDictionaryOfList_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Dictionary<string, List<int>>)
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

            var model = Assert.IsType<Dictionary<string, List<int>>>(modelBindingResult.Model);
            Assert.NotNull(model);
            Assert.Empty(model);

            Assert.Equal(0, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }
    }
}