// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class ServicesModelBinderTest
    {
        [Fact]
        public async Task ServiceModelBinder_BindsService()
        {
            // Arrange
            var type = typeof(IService);

            var binder = new ServicesModelBinder();
            var bindingContext = GetBindingContext(type);

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);

            var entry = bindingContext.ValidationState[bindingContext.Result.Model];
            Assert.True(entry.SuppressValidation);
            Assert.Null(entry.Key);
            Assert.Null(entry.Metadata);
        }

        private static DefaultModelBindingContext GetBindingContext(Type modelType)
        {
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForType(modelType).BindingDetails(d => d.BindingSource = BindingSource.Services);
            var modelMetadata = metadataProvider.GetMetadataForType(modelType);

            var services = new ServiceCollection();
            services.AddSingleton<IService>(new Service());

            var bindingContext = new DefaultModelBindingContext
            {
                ActionContext = new ActionContext()
                {
                    HttpContext = new DefaultHttpContext()
                    {
                        RequestServices = services.BuildServiceProvider(),
                    }
                },
                ModelMetadata = modelMetadata,
                ModelName = "modelName",
                FieldName = "modelName",
                ModelState = new ModelStateDictionary(),
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
