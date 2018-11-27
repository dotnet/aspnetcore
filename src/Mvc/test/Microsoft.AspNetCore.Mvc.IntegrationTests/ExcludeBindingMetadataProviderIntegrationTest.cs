// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    public class ExcludeBindingMetadataProviderIntegrationTest
    {
        [Fact]
        public async Task BindParameter_WithTypeProperty_IsNotBound()
        {
            // Arrange
            var options = new MvcOptions();
            var setup = new MvcCoreMvcOptionsSetup(new TestHttpRequestStreamReaderFactory());
            var modelBinderProvider = new TypeModelBinderProvider();

            // Adding a custom model binder for Type to ensure it doesn't get called
            options.ModelBinderProviders.Insert(0, modelBinderProvider);

            setup.Configure(options);

            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(options);
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(TypesBundle),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.Form = new FormCollection(new Dictionary<string, StringValues>
                {
                    { "name", new[] { "Fred" } },
                    { "type", new[] { "SomeType" } },
                });
            });

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var boundPerson = Assert.IsType<TypesBundle>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.Equal("Fred", boundPerson.Name);

            // The TypeModelBinder should not be called
            Assert.False(modelBinderProvider.Invoked);
        }

        [Fact()]
        public async Task BindParameter_WithTypeProperty_IsBound()
        {
            // Arrange
            var options = new MvcOptions();
            var modelBinderProvider = new TypeModelBinderProvider();

            // Adding a custom model binder for Type to ensure it doesn't get called
            options.ModelBinderProviders.Insert(0, modelBinderProvider);

            var setup = new MvcCoreMvcOptionsSetup(new TestHttpRequestStreamReaderFactory());
            setup.Configure(options);

            // Remove the ExcludeBindingMetadataProvider
            for (var i = options.ModelMetadataDetailsProviders.Count - 1; i >= 0; i--)
            {
                if (options.ModelMetadataDetailsProviders[i] is ExcludeBindingMetadataProvider)
                {
                    options.ModelMetadataDetailsProviders.RemoveAt(i);
                }
            }

            var metadataProvider = TestModelMetadataProvider.CreateProvider(options.ModelMetadataDetailsProviders);
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.Form = new FormCollection(new Dictionary<string, StringValues>
                    {
                        { "name", new[] { "Fred" } },
                        { "type", new[] { "SomeType" } },
                    });
                },
                metadataProvider: metadataProvider,
                mvcOptions: options);

            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(TypesBundle),
            };

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var boundPerson = Assert.IsType<TypesBundle>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.Equal("Fred", boundPerson.Name);

            // The TypeModelBinder should be called
            Assert.True(modelBinderProvider.Invoked);
        }

        private class TypesBundle
        {
            public string Name { get; set; }

            public Type Type { get; set; }
        }

        public class TypeModelBinderProvider : IModelBinderProvider
        {
            public bool Invoked { get; set; }

            /// <inheritdoc />
            public IModelBinder GetBinder(ModelBinderProviderContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                if (context.Metadata.ModelType == typeof(Type))
                {
                    Invoked = true;
                }

                return null;
            }
        }
    }
}
