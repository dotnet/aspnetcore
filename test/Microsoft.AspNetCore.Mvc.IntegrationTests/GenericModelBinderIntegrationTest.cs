// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<IFormCollection>)
            };

            // Need to have a key here so that the GenericModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.index=10");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<IFormCollection>)
            };
            // Need to have a key here so that the GenericModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?index=10");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<IFormCollection>)
            };

            // Without a key here so the GenericModelBinder will not recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
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

        private class AddressBinderProvider : IModelBinderProvider
        {
            public IModelBinder GetBinder(ModelBinderProviderContext context)
            {
                var allowedBindingSource = context.BindingInfo.BindingSource;
                if (allowedBindingSource?.CanAcceptDataFrom(BindAddressAttribute.Source) == true)
                {
                    // Binding Sources are opt-in. This model either didn't specify one or specified something
                    // incompatible so let other binders run.
                    return new AddressBinder();
                }

                return null;
            }
        }

        private class AddressBinder : IModelBinder
        {
            public Task BindModelAsync(ModelBindingContext bindingContext)
            {
                if (bindingContext == null)
                {
                    throw new ArgumentNullException(nameof(bindingContext));
                }

                Debug.Assert(bindingContext.Result == ModelBindingResult.Failed());

                var allowedBindingSource = bindingContext.BindingSource;
                if (allowedBindingSource == null ||
                    !allowedBindingSource.CanAcceptDataFrom(BindAddressAttribute.Source))
                {
                    // Binding Sources are opt-in. This model either didn't specify one or specified something
                    // incompatible so let other binders run.
                    return Task.CompletedTask;
                }

                bindingContext.Result = ModelBindingResult.Success(new Address());
                return Task.CompletedTask;
            }
        }

        // This isn't an especially useful scenario - but it exercises what happens when you
        // try to use a Collection of something that is bound greedily by binding source.
        [Fact]
        public async Task GenericModelBinder_BindsCollection_ElementTypeUsesGreedyModelBinder_WithPrefix_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(binderProvider: new AddressBinderProvider());
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Address[])
            };

            // Need to have a key here so that the GenericModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(
                request => request.QueryString = new QueryString("?parameter.index=0"));

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Address[]>(modelBindingResult.Model);
            Assert.Single(model);
            Assert.NotNull(model[0]);

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        // Similar to the GenericModelBinder_BindsCollection_ElementTypeUsesGreedyModelBinder_WithPrefix_Success
        // scenario but mis-configured. Model using a BindingSource for which no ModelBinder is enabled.
        [Fact]
        public async Task GenericModelBinder_BindsCollection_ElementTypeUsesGreedyBindingSource_WithPrefix_NullElement()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Address[])
            };

            // Need to have a key here so that the GenericModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(
                request => request.QueryString = new QueryString("?parameter.index=0"));

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Address[]>(modelBindingResult.Model);
            Assert.Single(model);
            Assert.Null(model[0]);

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        // This is part of a random sampling of scenarios where a GenericModelBinder is used
        // recursively.
        [Fact]
        public async Task GenericModelBinder_BindsArrayOfDictionary_WithPrefix_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Dictionary<string, int>[])
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter[0][0].Key=key0&parameter[0][0].Value=10");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Dictionary<string, int>[])
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?[0][0].Key=key0&[0][0].Value=10");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Dictionary<string, int>[])
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

            var model = Assert.IsType<Dictionary<string, int>[]>(modelBindingResult.Model);
            Assert.NotNull(model);
            Assert.Empty(model);

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        // This is part of a random sampling of scenarios where a GenericModelBinder is used
        // recursively.
        [Fact]
        public async Task GenericModelBinder_BindsCollectionOfKeyValuePair_WithPrefix_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(ICollection<KeyValuePair<string, int>>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter[0].Key=key0&parameter[0].Value=10");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(ICollection<KeyValuePair<string, int>>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?[0].Key=key0&[0].Value=10");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(ICollection<KeyValuePair<string, int>>)
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

            var model = Assert.IsType<List<KeyValuePair<string, int>>>(modelBindingResult.Model);
            Assert.NotNull(model);
            Assert.Empty(model);

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        // This is part of a random sampling of scenarios where a GenericModelBinder is used
        // recursively.
        [Fact]
        public async Task GenericModelBinder_BindsDictionaryOfList_WithPrefix_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Dictionary<string, List<int>>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString(
                    "?parameter[0].Key=key0&parameter[0].Value[0]=10&parameter[0].Value[1]=11");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Dictionary<string, List<int>>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?[0].Key=key0&[0].Value[0]=10&[0].Value[1]=11");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Dictionary<string, List<int>>)
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

            var model = Assert.IsType<Dictionary<string, List<int>>>(modelBindingResult.Model);
            Assert.NotNull(model);
            Assert.Empty(model);

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }
    }
}