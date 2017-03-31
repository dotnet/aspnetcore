// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    public class ExcludeBindingMetadataProviderIntegrationTest
    {
        [Fact(Skip = "See issue #6110")]
        public async Task BindParameter_WithTypeProperty_IsNotBound()
        {
            // Arrange
            var options = new MvcOptions();
            var setup = new MvcCoreMvcOptionsSetup(new TestHttpRequestStreamReaderFactory());

            // Adding a custom model binder for Type to ensure it doesn't get called
            options.ModelBinderProviders.Insert(0, new TypeModelBinderProvider());

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
                    { "typeArray", new[] { "SomeType1", "SomeType2" } },
                    { "typeList", new[] { "SomeType1", "SomeType2" } },
                    { "typeDictionary", new[] { "parameter[0].Key=key", "parameter[0].Value=value" } },
                    { "methodInfo", new[] { "value" } },
                    { "func", new[] { "value" } },
                });
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var boundPerson = Assert.IsType<TypesBundle>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.Equal("Fred", boundPerson.Name);

            // ModelState

            // The TypeModelBinder should not be called
            Assert.True(modelState.IsValid);
        }

        private class TypesBundle
        {
            public string Name { get; set; }

            public Type Type { get; set; }

            public Type[] TypeArray { get; set; }

            public List<Type> TypeList { get; set; }

            public Dictionary<string, Type> TypeDictionary { get; set; }

            public MethodInfo MethodInfo { get; set; }

            public Func<object> Func { get; set; }
        }

        public class TypeModelBinderProvider : IModelBinderProvider
        {
            /// <inheritdoc />
            public IModelBinder GetBinder(ModelBinderProviderContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                if (context.Metadata.ModelType == typeof(Type))
                {
                    throw new Exception();
                }

                return null;
            }
        }
    }
}