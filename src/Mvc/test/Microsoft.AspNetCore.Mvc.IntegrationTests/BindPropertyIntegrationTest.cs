// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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

        [Fact]
        public async Task BindModelAsync_WithBindProperty_BindNever_DoesNotBindModel()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = nameof(TestController.BindNeverProp),
                ParameterType = typeof(string),
                BindingInfo = BindingInfo.GetBindingInfo(new[] { new BindPropertyAttribute() }),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.Method = "POST";
                request.QueryString = new QueryString($"?{parameter.Name}=Joey");
            });

            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var modelMetadata = modelMetadataProvider
                .GetMetadataForProperty(typeof(TestController), parameter.Name);

            // Act
            var result = await parameterBinder.BindModelAsync(
                parameter,
                testContext,
                modelMetadataProvider,
                modelMetadata);

            // Assert
            Assert.False(result.IsModelSet);
            Assert.True(testContext.ModelState.IsValid);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData(123, true)]
        public async Task BindModelAsync_WithBindProperty_EnforcesBindRequired(int? input, bool isValid)
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = nameof(TestController.BindRequiredProp),
                ParameterType = typeof(string),
                BindingInfo = BindingInfo.GetBindingInfo(new[] { new BindPropertyAttribute() }),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.Method = "POST";

                if (input.HasValue)
                {
                    request.QueryString = new QueryString($"?{parameter.Name}={input.Value}");
                }
            });

            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var modelMetadata = modelMetadataProvider
                .GetMetadataForProperty(typeof(TestController), parameter.Name);

            // Act
            var result = await parameterBinder.BindModelAsync(
                parameter,
                testContext,
                modelMetadataProvider,
                modelMetadata);

            // Assert
            Assert.Equal(input.HasValue, result.IsModelSet);
            Assert.Equal(isValid, testContext.ModelState.IsValid);
            if (isValid)
            {
                Assert.Equal(input.Value, Assert.IsType<int>(result.Model));
            }
        }

        [Theory]
        [InlineData("RequiredAndStringLengthProp", null, false)]
        [InlineData("RequiredAndStringLengthProp", "", false)]
        [InlineData("RequiredAndStringLengthProp", "abc", true)]
        [InlineData("RequiredAndStringLengthProp", "abcTooLong", false)]
        [InlineData("DisplayNameStringLengthProp", null, true)]
        [InlineData("DisplayNameStringLengthProp", "", true)]
        [InlineData("DisplayNameStringLengthProp", "abc", true)]
        [InlineData("DisplayNameStringLengthProp", "abcTooLong", false, "My Display Name")]
        public async Task BindModelAsync_WithBindProperty_EnforcesDataAnnotationsAttributes(
            string propertyName, string input, bool isValid, string displayName = null)
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = propertyName,
                ParameterType = typeof(string),
                BindingInfo = BindingInfo.GetBindingInfo(new[] { new BindPropertyAttribute() }),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.Method = "POST";

                if (input != null)
                {
                    request.QueryString = new QueryString($"?{parameter.Name}={input}");
                }
            });

            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var modelMetadata = modelMetadataProvider
                .GetMetadataForProperty(typeof(TestController), parameter.Name);

            // Act
            var result = await parameterBinder.BindModelAsync(
                parameter,
                testContext,
                modelMetadataProvider,
                modelMetadata);

            // Assert
            Assert.Equal(input != null, result.IsModelSet);
            Assert.Equal(isValid, testContext.ModelState.IsValid);
            if (!isValid)
            {
                var message = testContext.ModelState[propertyName].Errors.Single().ErrorMessage;
                Assert.Contains(displayName ?? parameter.Name, message);
            }
        }

        class TestController
        {
            [BindNever] public string BindNeverProp { get; set; }
            [BindRequired] public int BindRequiredProp { get; set; }
            [Required, StringLength(3)] public string RequiredAndStringLengthProp { get; set; }
            [DisplayName("My Display Name"), StringLength(3)] public string DisplayNameStringLengthProp { get; set; }
        }
    }
}
