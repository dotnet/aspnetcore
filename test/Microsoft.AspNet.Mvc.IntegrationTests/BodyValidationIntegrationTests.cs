// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
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
        public async Task ModelMetaDataTypeAttribute_ValidBaseClass_NoModelStateErrors()
        {
            // Arrange
            var input = "{ \"Name\": \"MVC\", \"Contact\":\"4258959019\", \"Category\":\"Technology\"," +
                "\"CompanyName\":\"Microsoft\", \"Country\":\"USA\",\"Price\": 21, " +
                "\"ProductDetails\": {\"Detail1\": \"d1\", \"Detail2\": \"d2\", \"Detail3\": \"d3\"}}";
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                ParameterType = typeof(ProductViewModel),
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Body
                }
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
              request =>
              {
                  request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));
                  request.ContentType = "application/json;charset=utf-8";
              });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<ProductViewModel>(modelBindingResult.Model);
            Assert.True(modelState.IsValid);
            Assert.NotNull(boundPerson);
        }

        [Fact]
        public async Task ModelMetaDataTypeAttribute_InvalidPropertiesAndSubPropertiesOnBaseClass_HasModelStateErrors()
        {
            // Arrange
            var input = "{ \"Price\": 2, \"ProductDetails\": {\"Detail1\": \"d1\"}}";
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Body
                },
                ParameterType = typeof(ProductViewModel)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
              request =>
              {
                  request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));
                  request.ContentType = "application/json";
              });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<ProductViewModel>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.False(modelState.IsValid);
            var modelStateErrors = CreateValidationDictionary(modelState);
            Assert.Equal("CompanyName cannot be null or empty.", modelStateErrors["CompanyName"]);
            Assert.Equal("The field Price must be between 20 and 100.", modelStateErrors["Price"]);
            // Mono issue - https://github.com/aspnet/External/issues/19
            Assert.Equal(PlatformNormalizer.NormalizeContent("The Category field is required."), modelStateErrors["Category"]);
            Assert.Equal(PlatformNormalizer.NormalizeContent("The Contact Us field is required."), modelStateErrors["Contact"]);
            Assert.Equal(
                PlatformNormalizer.NormalizeContent("The Detail2 field is required."),
                modelStateErrors["ProductDetails.Detail2"]);
            Assert.Equal(
                PlatformNormalizer.NormalizeContent("The Detail3 field is required."),
                modelStateErrors["ProductDetails.Detail3"]);
        }

        [Fact]
        public async Task ModelMetaDataTypeAttribute_InvalidComplexTypePropertyOnBaseClass_HasModelStateErrors()
        {
            // Arrange
            var input = "{ \"Contact\":\"4255678765\", \"Category\":\"Technology\"," +
                "\"CompanyName\":\"Microsoft\", \"Country\":\"USA\",\"Price\": 21 }";
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Body
                },
                ParameterType = typeof(ProductViewModel)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
              request =>
              {
                  request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));
                  request.ContentType = "application/json";
              });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<ProductViewModel>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.False(modelState.IsValid);
            var modelStateErrors = CreateValidationDictionary(modelState);
            Assert.Equal(
                PlatformNormalizer.NormalizeContent("The ProductDetails field is required."),
                modelStateErrors["ProductDetails"]);
        }

        [Fact]
        public async Task ModelMetaDataTypeAttribute_InvalidClassAttributeOnBaseClass_HasModelStateErrors()
        {
            // Arrange
            var input = "{ \"Contact\":\"4258959019\", \"Category\":\"Technology\"," +
                "\"CompanyName\":\"Microsoft\", \"Country\":\"UK\",\"Price\": 21, \"ProductDetails\": {\"Detail1\": \"d1\"," +
                " \"Detail2\": \"d2\", \"Detail3\": \"d3\"}}";
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Body
                },
                ParameterType = typeof(ProductViewModel)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
              request =>
              {
                  request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));
                  request.ContentType = "application/json";
              });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<ProductViewModel>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.False(modelState.IsValid);
            var modelStateErrors = CreateValidationDictionary(modelState);
            Assert.Single(modelStateErrors);
            Assert.Equal("Product must be made in the USA if it is not named.", modelStateErrors[""]);
        }

        [Fact]
        public async Task ModelMetaDataTypeAttribute_ValidDerivedClass_NoModelStateErrors()
        {
            // Arrange
            var input = "{ \"Name\": \"MVC\", \"Contact\":\"4258959019\", \"Category\":\"Technology\"," +
                "\"CompanyName\":\"Microsoft\", \"Country\":\"USA\", \"Version\":\"2\"," +
                "\"DatePurchased\": \"/Date(1297246301973)/\", \"Price\" : \"110\" }";
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Body
                },
                ParameterType = typeof(SoftwareViewModel)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
              request =>
              {
                  request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));
                  request.ContentType = "application/json";
              });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<SoftwareViewModel>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.True(modelState.IsValid);
        }

        [Fact]
        public async Task ModelMetaDataTypeAttribute_InvalidPropertiesOnDerivedClass_HasModelStateErrors()
        {
            // Arrange
            var input = "{ \"Name\": \"MVC\", \"Contact\":\"425-895-9019\", \"Category\":\"Technology\"," +
                "\"CompanyName\":\"Microsoft\", \"Country\":\"USA\",\"Price\": 2}";
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Body
                },
                ParameterType = typeof(SoftwareViewModel)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
              request =>
              {
                  request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));
                  request.ContentType = "application/json";
              });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<SoftwareViewModel>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.False(modelState.IsValid);
            var modelStateErrors = CreateValidationDictionary(modelState);
            Assert.Equal(2, modelStateErrors.Count);
            Assert.Equal("The field Price must be between 100 and 200.", modelStateErrors["Price"]);
            Assert.Equal("The field Contact must be a string with a maximum length of 10.", modelStateErrors["Contact"]);
        }

        [Fact]
        public async Task ModelMetaDataTypeAttribute_InvalidClassAttributeOnBaseClassProduct_HasModelStateErrors()
        {
            // Arrange
            var input = "{ \"Contact\":\"4258959019\", \"Category\":\"Technology\"," +
                "\"CompanyName\":\"Microsoft\", \"Country\":\"UK\",\"Version\":\"2\"," +
                "\"DatePurchased\": \"/Date(1297246301973)/\", \"Price\" : \"110\" }";
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Body
                },
                ParameterType = typeof(SoftwareViewModel)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
              request =>
              {
                  request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));
                  request.ContentType = "application/json";
              });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<SoftwareViewModel>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.False(modelState.IsValid);
            var modelStateErrors = CreateValidationDictionary(modelState);
            Assert.Single(modelStateErrors);
            Assert.Equal("Product must be made in the USA if it is not named.", modelStateErrors[""]);
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

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("CustomParameter.Address", key);
            Assert.False(modelState.IsValid);
            var error = Assert.Single(modelState[key].Errors);
            // Mono issue - https://github.com/aspnet/External/issues/19
            Assert.Equal(PlatformNormalizer.NormalizeContent("The Address field is required."), error.ErrorMessage);
        }

        [Fact]
        public async Task FromBodyOnActionParameter_EmptyBody_BindsToNullValue()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo
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
            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            Assert.Null(modelBindingResult.Model);

            Assert.True(modelState.IsValid);
            var entry = Assert.Single(modelState);
            Assert.Empty(entry.Key);
            Assert.Null(entry.Value.RawValue);
        }

        private class Person4
        {
            [FromBody]
            [Required]
            public int Address { get; set; }
        }

        [Fact]
        public async Task FromBodyAndRequiredOnValueTypeProperty_EmptyBody_JsonFormatterAddsModelStateError()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo
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

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<Person4>(modelBindingResult.Model);

            Assert.False(modelState.IsValid);
            var entry = Assert.Single(modelState);
            Assert.Equal("CustomParameter.Address", entry.Key);
            Assert.Null(entry.Value.AttemptedValue);
            Assert.Null(entry.Value.RawValue);
            var error = Assert.Single(entry.Value.Errors);
            Assert.NotNull(error.Exception);

            // Json.NET currently throws an exception starting with "No JSON content found and type 'System.Int32' is
            // not nullable." but do not tie test to a particular Json.NET build.
            Assert.NotEmpty(error.Exception.Message);
        }

        private class Person5
        {
            [FromBody]
            public Address5 Address { get; set; }
        }

        private class Address5
        {
            public int Number { get; set; }

            // Required attribute does not cause an error in test scenarios. JSON deserializer ok w/ missing data.
            [Required]
            public int RequiredNumber { get; set; }
        }

        [Fact]
        public async Task FromBodyAndRequiredOnInnerValueTypeProperty_NotBound_JsonFormatterSuccessful()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person5)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{ \"Number\": 5 }"));
                    request.ContentType = "application/json";
                });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<Person5>(modelBindingResult.Model);
            Assert.NotNull(boundPerson.Address);
            Assert.Equal(5, boundPerson.Address.Number);
            Assert.Equal(0, boundPerson.Address.RequiredNumber);

            Assert.True(modelState.IsValid);
            var entry = Assert.Single(modelState);
            Assert.Equal("CustomParameter.Address", entry.Key);
            Assert.NotNull(entry.Value);
            Assert.Null(entry.Value.AttemptedValue);
            Assert.Same(boundPerson.Address, entry.Value.RawValue);
            Assert.Empty(entry.Value.Errors);
        }

        [Fact]
        public async Task FromBodyWithInvalidPropertyData_JsonFormatterAddsModelError()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person5)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{ \"Number\": \"not a number\" }"));
                    request.ContentType = "application/json";
                });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<Person5>(modelBindingResult.Model);
            Assert.Null(boundPerson.Address);

            Assert.False(modelState.IsValid);
            Assert.Equal(2, modelState.Count);
            Assert.Equal(1, modelState.ErrorCount);

            var state = modelState["CustomParameter.Address"];
            Assert.NotNull(state);
            Assert.Null(state.AttemptedValue);
            Assert.Null(state.RawValue);
            Assert.Empty(state.Errors);

            state = modelState["CustomParameter.Address.Number"];
            Assert.NotNull(state);
            Assert.Null(state.AttemptedValue);
            Assert.Null(state.RawValue);
            var error = Assert.Single(state.Errors);
            Assert.NotNull(error.Exception);

            // Json.NET currently throws an Exception with a Message starting with "Could not convert string to
            // integer: not a number." but do not tie test to a particular Json.NET build.
            Assert.NotEmpty(error.Exception.Message);
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

        [Theory]
        [InlineData("{ \"Zip\" : 123 }")]
        [InlineData("{}")]
        public async Task FromBodyOnTopLevelProperty_RequiredOnSubProperty_AddsModelStateError(string inputText)
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor
            {
                BindingInfo = new BindingInfo
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person2),
                Name = "param-name",
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes(inputText));
                    request.ContentType = "application/json";
                });
            var httpContext = operationContext.HttpContext;
            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<Person2>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);

            Assert.False(modelState.IsValid);
            Assert.Equal(2, modelState.Keys.Count);
            var address = Assert.Single(modelState, kvp => kvp.Key == "CustomParameter.Address").Value;
            Assert.Equal(ModelValidationState.Unvalidated, address.ValidationState);

            var street = Assert.Single(modelState, kvp => kvp.Key == "CustomParameter.Address.Street").Value;
            Assert.Equal(ModelValidationState.Invalid, street.ValidationState);
            var error = Assert.Single(street.Errors);
            // Mono issue - https://github.com/aspnet/External/issues/19
            Assert.Equal(PlatformNormalizer.NormalizeContent("The Street field is required."), error.ErrorMessage);
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

        [Theory]
        [InlineData("{ \"Street\" : \"someStreet\" }")]
        [InlineData("{}")]
        public async Task FromBodyOnProperty_Succeeds_IgnoresRequiredOnValueTypeSubProperty(string inputText)
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor
            {
                BindingInfo = new BindingInfo
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person3),
                Name = "param-name",
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes(inputText));
                    request.ContentType = "application/json";
                });
            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            Assert.IsType<Person3>(modelBindingResult.Model);

            Assert.True(modelState.IsValid);
            var entry = Assert.Single(modelState);
            Assert.Equal("CustomParameter.Address", entry.Key);
        }

        private Dictionary<string, string> CreateValidationDictionary(ModelStateDictionary modelState)
        {
            var result = new Dictionary<string, string>();
            foreach (var item in modelState)
            {
                var errorMessage = string.Empty;
                foreach (var error in item.Value.Errors)
                {
                    if (error != null)
                    {
                        errorMessage = errorMessage + error.ErrorMessage;
                    }
                }
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    result.Add(item.Key, errorMessage);
                }
            }

            return result;
        }
    }
}