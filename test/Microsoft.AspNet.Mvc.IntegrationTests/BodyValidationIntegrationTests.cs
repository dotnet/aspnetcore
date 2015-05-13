// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public class BodyValidationIntegrationTests
    {
        private class Person
        {
            [FromBody]
            [Required]
            public Address Address { get; set; }
        }

        private class Address
        {
            public string Street { get; set; }
        }

        [Fact]
        public async Task FromBodyAndRequiredOnProperty_EmptyBody_AddsModelStateError()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
              request =>
              {
                  request.Body = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));
                  request.ContentType = "application/json";
              });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("CustomParameter.Address", key);
            Assert.False(modelState.IsValid);
            var error = Assert.Single(modelState[key].Errors);
            Assert.Equal("The Address field is required.", error.ErrorMessage);
        }

        [Fact]
        public async Task FromBodyOnActionParameter_EmptyBody_BindsToNullValue()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "CustomParameter",
                    BindingSource = BindingSource.Body
                },
                ParameterType = typeof(Person)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));
                    request.ContentType = "application/json";
                });

            var httpContext = operationContext.HttpContext;
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);
            Assert.Null(modelBindingResult.Model);
            Assert.Empty(modelState.Keys);
            Assert.True(modelState.IsValid);
        }

        private class Person4
        {
            [FromBody]
            [Required]
            public int Address { get; set; }
        }

        [Fact]
        public async Task FromBodyAndRequiredOnValueTypeProperty_EmptyBody_AddsModelStateError()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person4)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));
                    request.ContentType = "application/json";
                });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<Person4>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.False(modelState.IsValid);

            // The error with an empty key is a bug(#2416)  in our implementation which does not append the prefix and
            // use that along with the path. The expected key here would be CustomParameter.Address.
            var key = Assert.Single(modelState.Keys, k => k == "");
            var error = Assert.Single(modelState[""].Errors);
            Assert.StartsWith(
                "No JSON content found and type 'System.Int32' is not nullable.",
                error.Exception.Message);
        }

        private class Person2
        {
            [FromBody]
            public Address2 Address { get; set; }
        }

        private class Address2
        {
            [Required]
            public string Street { get; set; }

            public int Zip { get; set; }
        }

        [Theory(Skip = "There should be entries for all model properties which are bound. #2445")]
        [InlineData("{ \"Zip\" : 123 }")]
        [InlineData("{}")]
        public async Task FromBodyOnTopLevelProperty_RequiredOnSubProperty_AddsModelStateError(string inputText)
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person2)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes(inputText));
                    request.ContentType = "application/json";
                });
            var httpContext = operationContext.HttpContext;
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<Person2>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.False(modelState.IsValid);
            Assert.Equal(2, modelState.Keys.Count);
            var zip = Assert.Single(modelState.Keys, k => k == "CustomParameter.Address.Zip");
            Assert.Equal(ModelValidationState.Valid, modelState[zip].ValidationState);

            var street = Assert.Single(modelState.Keys, k => k == "CustomParameter.Address.Street");
            Assert.Equal(ModelValidationState.Invalid, modelState[street].ValidationState);
            var error = Assert.Single(modelState[street].Errors);
            Assert.Equal("The Street field is required.", error.ErrorMessage);
        }

        private class Person3
        {
            [FromBody]
            public Address3 Address { get; set; }
        }

        private class Address3
        {
            public string Street { get; set; }

            [Required]
            public int Zip { get; set; }
        }

        [Theory(Skip = "There should be entries for all model properties which are bound. #2445")]
        [InlineData("{ \"Street\" : \"someStreet\" }")]
        [InlineData("{}")]
        public async Task FromBodyOnProperty_RequiredOnValueTypeSubProperty_AddsModelStateError(string inputText)
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person3)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes(inputText));
                    request.ContentType = "application/json";
                });
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<Person3>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.False(modelState.IsValid);
            var street = Assert.Single(modelState.Keys, k => k == "CustomParameter.Address.Street");
            Assert.Equal(ModelValidationState.Valid, modelState[street].ValidationState);

            // The error with an empty key is a bug(#2416) in our implementation which does not append the prefix and
            // use that along with the path. The expected key here would be Address.
            var zip = Assert.Single(modelState.Keys, k => k == "CustomParameter.Address.Zip");
            Assert.Equal(ModelValidationState.Valid, modelState[zip].ValidationState);
            var error = Assert.Single(modelState[""].Errors);
            Assert.StartsWith(
                "Required property 'Zip' not found in JSON. Path ''",
                error.Exception.Message);
        }
    }
}