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
    // Integration tests for binding top level models with [BindProperty]
    public class BindPropertyIntegrationTest
    {
        private class Person
        {
            public string Name { get; set; }
        }

        [Fact]
        public async Task BindModelAsync_WithBindProperty_BindsModel_WhenRequestIsNotGet()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Person),
                BindingInfo = BindingInfo.GetBindingInfo(new[] { new BindPropertyAttribute() }),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.Method = "POST";
                request.QueryString = new QueryString("?parameter.Name=Joey");
            });

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(result.IsModelSet);

            Assert.Equal("Joey", Assert.IsType<Person>(result.Model).Name);
        }

        [Fact]
        public async Task BindModelAsync_WithBindProperty_SupportsGet_BindsModel_WhenRequestIsGet()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Person),
                BindingInfo = BindingInfo.GetBindingInfo(new[] { new BindPropertyAttribute() { SupportsGet = true } }),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.Method = "GET";
                request.QueryString = new QueryString("?parameter.Name=Joey");
            });

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(result.IsModelSet);

            Assert.Equal("Joey", Assert.IsType<Person>(result.Model).Name);
        }

        [Fact]
        public async Task BindModelAsync_WithBindProperty_DoesNotBindModel_WhenRequestIsGet()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Person),
                BindingInfo = BindingInfo.GetBindingInfo(new[] { new BindPropertyAttribute() }),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.Method = "GET";
                request.QueryString = new QueryString("?parameter.Name=Joey");
            });

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.False(result.IsModelSet);
        }
    }
}
