// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
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
        // In this example we choose IFormCollection - because IFormCollection has a dedicated
        // model  binder.
        [Fact(Skip = "Extra ModelState key because of #2446")]
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
            Assert.Equal(1, model.Count);
            Assert.NotNull(model[0]);

            Assert.Equal(0, modelState.Count); // This fails due to #2446
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        // This isn't an especially useful scenario - but it exercises what happens when you
        // try to use a Collection of something that is bound greedily by model-type.
        //
        // In this example we choose IFormCollection - because IFormCollection has a dedicated
        // model  binder.
        [Fact(Skip = "Extra ModelState key because of #2446")]
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
            Assert.Equal(1, model.Count);
            Assert.NotNull(model[0]);

            Assert.Equal(0, modelState.Count); // This fails due to #2446
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        // This isn't an especially useful scenario - but it exercises what happens when you
        // try to use a Collection of something that is bound greedily by model-type.
        //
        // In this example we choose IFormCollection - because IFormCollection has a dedicated
        // model  binder.
        [Fact(Skip = "Empty collection should be created by the collection model binder #1579")]
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
            Assert.NotNull(modelBindingResult); // This fails due to #1579
            Assert.False(modelBindingResult.IsModelSet);

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
        [Fact(Skip = "Extra ModelState key because of #2446")]
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
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter.index=0");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Address[]>(modelBindingResult.Model);
            Assert.Equal(1, model.Length);
            Assert.NotNull(model[0]);

            Assert.Equal(0, modelState.Count); // This fails due to #2446
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }
    }
}