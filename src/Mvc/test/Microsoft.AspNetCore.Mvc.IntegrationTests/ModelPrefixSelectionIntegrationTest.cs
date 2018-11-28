// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    // Integration tests for the decision logic about how a model-name/prefix is selected at the top-level
    // of ModelBinding.
    public class ModelPrefixSelectionIntegrationTest
    {
        private class Person1
        {
            [FromForm]
            public string Name { get; set; }
        }

        [Fact]
        public async Task ComplexModel_PrefixSelected_ByValueProvider()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Person1),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                // This will cause selection of the "parameter" prefix.
                request.QueryString = new QueryString("?parameter=");

                // This value won't be used, because we select the "parameter" prefix.
                request.Form = new FormCollection(new Dictionary<string, StringValues>()
                {
                    { "Name", "Billy" },
                });
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Person1>(modelBindingResult.Model);
            Assert.Null(model.Name);

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class Person2
        {
            [FromForm]
            public string Name { get; set; }
        }

        [Fact]
        public async Task ComplexModel_PrefixSelected_ByValueProviderValue_WithFilteredValueProviders()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Person2),
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Query,
                },
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                // This will cause selection of the "parameter" prefix.
                request.QueryString = new QueryString("?parameter=");

                // This value won't be used, because we select the "parameter" prefix.
                request.Form = new FormCollection(new Dictionary<string, StringValues>()
                {
                    { "Name", "Billy" },
                });
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Person2>(modelBindingResult.Model);
            Assert.Null(model.Name);

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class Person3
        {
            [FromForm]
            public string Name { get; set; }
        }

        [Fact]
        public async Task ComplexModel_EmptyPrefixSelected_NoMatchingValueProviderValue()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Person3),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                // This can't be used because of [FromForm] on the property.
                request.QueryString = new QueryString("?Name=");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Person3>(modelBindingResult.Model);
            Assert.Null(model.Name);

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class Person4
        {
            [FromForm]
            public string Name { get; set; }
        }

        [Fact]
        public async Task ComplexModel_EmptyPrefixSelected_NoMatchingValueProviderValue_WithFilteredValueProviders()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Person4),
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Query,
                },
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                // This will only match empty prefix, but can't be used because of [FromForm] on the property.
                request.QueryString = new QueryString("?Name=");

                // This value won't be used to select a prefix, because we're only looking at the query string.
                request.Form = new FormCollection(new Dictionary<string, StringValues>()
                {
                    { "parameter", string.Empty },
                });
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Person4>(modelBindingResult.Model);
            Assert.Null(model.Name);

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class Person5
        {
            [FromForm]
            public string Name { get; set; }
        }

        [Fact]
        public async Task ComplexModel_EmptyPrefixSelected_NoMatchingValueProviderValue_WithFilteredValueProviders_NoValues()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Person5),
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Query,
                },
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                // This value won't be used to select a prefix, because we're only looking at the query string.
                request.Form = new FormCollection(new Dictionary<string, StringValues>()
                {
                    { "parameter", string.Empty },
                });
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Person5>(modelBindingResult.Model);
            Assert.Null(model.Name);

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }
    }
}
