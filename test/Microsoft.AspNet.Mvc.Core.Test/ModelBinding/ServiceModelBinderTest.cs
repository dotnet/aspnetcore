// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ServiceModelBinderTest
    {
        [Fact]
        public async Task ServiceModelBinder_BindsService()
        {
            // Arrange
            var type = typeof(IService);

            var binder = new ServicesModelBinder();
            var modelBindingContext = GetBindingContext(type);

            // Act
            var result = await binder.BindModelAsync(modelBindingContext);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);
            Assert.True(result.IsModelSet);
            Assert.NotNull(result.Model);
            Assert.Equal("modelName", result.Key);

            var entry = modelBindingContext.ValidationState[result.Model];
            Assert.True(entry.SuppressValidation);
            Assert.Null(entry.Key);
            Assert.Null(entry.Metadata);
        }

        [Fact]
        public async Task ServiceModelBinder_ReturnsNoResult_ForNullBindingSource()
        {
            // Arrange
            var type = typeof(IService);

            var binder = new ServicesModelBinder();
            var modelBindingContext = GetBindingContext(type);
            modelBindingContext.BindingSource = null;

            // Act
            var result = await binder.BindModelAsync(modelBindingContext);

            // Assert
            Assert.Equal(ModelBindingResult.NoResult, result);
        }

        [Fact]
        public async Task ServiceModelBinder_ReturnsNoResult_ForNonServiceBindingSource()
        {
            // Arrange
            var type = typeof(IService);

            var binder = new ServicesModelBinder();
            var modelBindingContext = GetBindingContext(type);
            modelBindingContext.BindingSource = BindingSource.Body;

            // Act
            var result = await binder.BindModelAsync(modelBindingContext);

            // Assert
            Assert.Equal(ModelBindingResult.NoResult, result);
        }

        private static ModelBindingContext GetBindingContext(Type modelType)
        {
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForType(modelType).BindingDetails(d => d.BindingSource = BindingSource.Services);
            var modelMetadata = metadataProvider.GetMetadataForType(modelType);


            var services = new ServiceCollection();
            services.AddInstance<IService>(new Service());

            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = modelMetadata,
                ModelName = "modelName",
                FieldName = "modelName",
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = new OperationBindingContext
                {
                    ModelBinder = new HeaderModelBinder(),
                    MetadataProvider = metadataProvider,
                    HttpContext = new DefaultHttpContext()
                    {
                        RequestServices = services.BuildServiceProvider(),
                    },
                },
                BinderModelName = modelMetadata.BinderModelName,
                BindingSource = modelMetadata.BindingSource,
                ValidationState = new ValidationStateDictionary(),
            };

            return bindingContext;
        }

        private interface IService
        {
        }

        private class Service : IService
        {
        }
    }
}
