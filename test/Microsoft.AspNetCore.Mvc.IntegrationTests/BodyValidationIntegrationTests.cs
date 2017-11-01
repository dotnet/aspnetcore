// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    public class BodyValidationIntegrationTests
    {
        [Fact]
        public async Task ModelMetadataTypeAttribute_ValidBaseClass_NoModelStateErrors()
        {
            // Arrange
            var input = "{ \"Name\": \"MVC\", \"Contact\":\"4258959019\", \"Category\":\"Technology\"," +
                "\"CompanyName\":\"Microsoft\", \"Country\":\"USA\",\"Price\": 21, " +
                "\"ProductDetails\": {\"Detail1\": \"d1\", \"Detail2\": \"d2\", \"Detail3\": \"d3\"}}";
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                ParameterType = typeof(ProductViewModel),
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Body
                }
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
              request =>
              {
                  request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));
                  request.ContentType = "application/json;charset=utf-8";
              });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<ProductViewModel>(modelBindingResult.Model);
            Assert.True(modelState.IsValid);
            Assert.NotNull(boundPerson);
        }

        [Fact]
        public async Task ModelMetadataType_ValidArray_NoModelStateErrors()
        {
            // Arrange
            var input = "[" +
                "{ \"Name\": \"MVC\", \"Contact\":\"4258959019\", \"Category\":\"Technology\"," +
                "\"CompanyName\":\"Microsoft\", \"Country\":\"USA\",\"Price\": 21, " +
                "\"ProductDetails\": {\"Detail1\": \"d1\", \"Detail2\": \"d2\", \"Detail3\": \"d3\"}}," +
                "{ \"Name\": \"MVC too\", \"Contact\":\"4258959020\", \"Category\":\"Technology\"," +
                "\"CompanyName\":\"Microsoft\", \"Country\":\"USA\",\"Price\": 22, " +
                "\"ProductDetails\": {\"Detail1\": \"d2\", \"Detail2\": \"d3\", \"Detail3\": \"d4\"}}" +
                "]";
            var argumentBinding = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                ParameterType = typeof(IEnumerable<ProductViewModel>),
                BindingInfo = new BindingInfo
                {
                    BindingSource = BindingSource.Body,
                },
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));
                request.ContentType = "application/json;charset=utf-8";
            });
            var modelState = testContext.ModelState;

            // Act
            var result = await argumentBinding.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelState.IsValid);
            Assert.True(result.IsModelSet);
            var products = Assert.IsAssignableFrom<IEnumerable<ProductViewModel>>(result.Model);
            Assert.Equal(2, products.Count());
        }

        [Fact]
        public async Task ModelMetadataTypeAttribute_InvalidPropertiesAndSubPropertiesOnBaseClass_HasModelStateErrors()
        {
            // Arrange
            var input = "{ \"Price\": 2, \"ProductDetails\": {\"Detail1\": \"d1\"}}";
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Body
                },
                ParameterType = typeof(ProductViewModel)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
              request =>
              {
                  request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));
                  request.ContentType = "application/json";
              });

            var modelState = testContext.ModelState;

            var priceRange = ValidationAttributeUtil.GetRangeErrorMessage(20, 100, "Price");
            var categoryRequired = ValidationAttributeUtil.GetRequiredErrorMessage("Category");
            var contactUsRequired = ValidationAttributeUtil.GetRequiredErrorMessage("Contact Us");
            var detail2Required = ValidationAttributeUtil.GetRequiredErrorMessage("Detail2");
            var detail3Required = ValidationAttributeUtil.GetRequiredErrorMessage("Detail3");

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<ProductViewModel>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.False(modelState.IsValid);
            var modelStateErrors = CreateValidationDictionary(modelState);

            Assert.Equal("CompanyName cannot be null or empty.", modelStateErrors["CompanyName"]);
            Assert.Equal(priceRange, modelStateErrors["Price"]);
            Assert.Equal(categoryRequired, modelStateErrors["Category"]);
            Assert.Equal(contactUsRequired, modelStateErrors["Contact"]);
            Assert.Equal(detail2Required, modelStateErrors["ProductDetails.Detail2"]);
            Assert.Equal(detail3Required, modelStateErrors["ProductDetails.Detail3"]);
        }

        [Fact]
        public async Task ModelMetadataTypeAttribute_InvalidComplexTypePropertyOnBaseClass_HasModelStateErrors()
        {
            // Arrange
            var input = "{ \"Contact\":\"4255678765\", \"Category\":\"Technology\"," +
                "\"CompanyName\":\"Microsoft\", \"Country\":\"USA\",\"Price\": 21 }";
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Body
                },
                ParameterType = typeof(ProductViewModel)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
              request =>
              {
                  request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));
                  request.ContentType = "application/json";
              });

            var modelState = testContext.ModelState;

            var productDetailsRequired = ValidationAttributeUtil.GetRequiredErrorMessage("ProductDetails");

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<ProductViewModel>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.False(modelState.IsValid);
            var modelStateErrors = CreateValidationDictionary(modelState);
            Assert.Equal(productDetailsRequired, modelStateErrors["ProductDetails"]);
        }

        [Fact]
        public async Task ModelMetadataTypeAttribute_InvalidClassAttributeOnBaseClass_HasModelStateErrors()
        {
            // Arrange
            var input = "{ \"Contact\":\"4258959019\", \"Category\":\"Technology\"," +
                "\"CompanyName\":\"Microsoft\", \"Country\":\"UK\",\"Price\": 21, \"ProductDetails\": {\"Detail1\": \"d1\"," +
                " \"Detail2\": \"d2\", \"Detail3\": \"d3\"}}";
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Body
                },
                ParameterType = typeof(ProductViewModel)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
              request =>
              {
                  request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));
                  request.ContentType = "application/json";
              });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

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
        public async Task ModelMetadataTypeAttribute_ValidDerivedClass_NoModelStateErrors()
        {
            // Arrange
            var input = "{ \"Name\": \"MVC\", \"Contact\":\"4258959019\", \"Category\":\"Technology\"," +
                "\"CompanyName\":\"Microsoft\", \"Country\":\"USA\", \"Version\":\"2\"," +
                "\"DatePurchased\": \"/Date(1297246301973)/\", \"Price\" : \"110\" }";
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Body
                },
                ParameterType = typeof(SoftwareViewModel)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
              request =>
              {
                  request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));
                  request.ContentType = "application/json";
              });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<SoftwareViewModel>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.True(modelState.IsValid);
        }

        [Fact]
        public async Task ModelMetadataTypeAttribute_InvalidPropertiesOnDerivedClass_HasModelStateErrors()
        {
            // Arrange
            var input = "{ \"Name\": \"MVC\", \"Contact\":\"425-895-9019\", \"Category\":\"Technology\"," +
                "\"CompanyName\":\"Microsoft\", \"Country\":\"USA\",\"Price\": 2}";
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Body
                },
                ParameterType = typeof(SoftwareViewModel)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
              request =>
              {
                  request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));
                  request.ContentType = "application/json";
              });

            var modelState = testContext.ModelState;

            var priceRange = ValidationAttributeUtil.GetRangeErrorMessage(100, 200, "Price");
            var contactLength = ValidationAttributeUtil.GetStringLengthErrorMessage(null, 10, "Contact");

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<SoftwareViewModel>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.False(modelState.IsValid);
            var modelStateErrors = CreateValidationDictionary(modelState);
            Assert.Equal(2, modelStateErrors.Count);

            Assert.Equal(priceRange, modelStateErrors["Price"]);
            Assert.Equal(contactLength, modelStateErrors["Contact"]);
        }

        [Fact]
        public async Task ModelMetadataTypeAttribute_InvalidClassAttributeOnBaseClassProduct_HasModelStateErrors()
        {
            // Arrange
            var input = "{ \"Contact\":\"4258959019\", \"Category\":\"Technology\"," +
                "\"CompanyName\":\"Microsoft\", \"Country\":\"UK\",\"Version\":\"2\"," +
                "\"DatePurchased\": \"/Date(1297246301973)/\", \"Price\" : \"110\" }";
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Body
                },
                ParameterType = typeof(SoftwareViewModel)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
              request =>
              {
                  request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));
                  request.ContentType = "application/json";
              });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<SoftwareViewModel>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            Assert.False(modelState.IsValid);
            var modelStateErrors = CreateValidationDictionary(modelState);
            Assert.Single(modelStateErrors);
            Assert.Equal("Product must be made in the USA if it is not named.", modelStateErrors[""]);
        }

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
        public async Task FromBodyAllowingEmptyInputAndRequiredOnProperty_EmptyBody_AddsModelStateError()
        {
            // Arrange
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
              request =>
              {
                  request.Body = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));
                  request.ContentType = "application/json";
              });

            var modelState = testContext.ModelState;

            var addressRequired = ValidationAttributeUtil.GetRequiredErrorMessage("Address");

            var optionsAccessor = testContext.GetService<IOptions<MvcOptions>>();
            optionsAccessor.Value.AllowEmptyInputInBodyModelBinding = true;
            
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(optionsAccessor.Value);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("CustomParameter.Address", key);
            Assert.False(modelState.IsValid);
            var error = Assert.Single(modelState[key].Errors);
            Assert.Equal(addressRequired, error.ErrorMessage);
        }

        [Fact]
        public async Task FromBodyAllowingEmptyInputOnActionParameter_EmptyBody_BindsToNullValue()
        {
            // Arrange
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

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));
                    request.ContentType = "application/json";
                });

            var httpContext = testContext.HttpContext;
            var modelState = testContext.ModelState;

            var optionsAccessor = testContext.GetService<IOptions<MvcOptions>>();
            optionsAccessor.Value.AllowEmptyInputInBodyModelBinding = true;
            
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(optionsAccessor.Value);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            Assert.Null(modelBindingResult.Model);

            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person4)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));
                    request.ContentType = "application/json";
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<Person4>(modelBindingResult.Model);

            Assert.False(modelState.IsValid);
            var entry = Assert.Single(modelState);
            Assert.Equal("CustomParameter.Address", entry.Key);
            Assert.Null(entry.Value.AttemptedValue);
            Assert.Null(entry.Value.RawValue);
            var error = Assert.Single(entry.Value.Errors);
            Assert.Null(error.Exception);

            // Json.NET currently throws an exception starting with "No JSON content found and type 'System.Int32' is
            // not nullable." but do not tie test to a particular Json.NET build.
            Assert.NotEmpty(error.ErrorMessage);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person5)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{ \"Number\": 5 }"));
                    request.ContentType = "application/json";
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<Person5>(modelBindingResult.Model);
            Assert.NotNull(boundPerson.Address);
            Assert.Equal(5, boundPerson.Address.Number);
            Assert.Equal(0, boundPerson.Address.RequiredNumber);

            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
        }

        [Fact]
        public async Task FromBodyWithInvalidPropertyData_JsonFormatterAddsModelError()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person5)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{ \"Number\": \"not a number\" }"));
                    request.ContentType = "application/json";
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<Person5>(modelBindingResult.Model);
            Assert.Null(boundPerson.Address);

            Assert.False(modelState.IsValid);
            Assert.Single(modelState);
            Assert.Equal(1, modelState.ErrorCount);

            var state = modelState["CustomParameter.Address.Number"];
            Assert.NotNull(state);
            Assert.Null(state.AttemptedValue);
            Assert.Null(state.RawValue);
            var error = Assert.Single(state.Errors);
            Assert.Null(error.Exception);

            // Json.NET currently throws an Exception with a Message starting with "Could not convert string to
            // integer: not a number." but do not tie test to a particular Json.NET build.
            Assert.NotEmpty(error.ErrorMessage);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public async Task FromBodyWithEmptyBody_JsonFormatterAddsModelErrorWhenExpected(
            bool allowEmptyInputInBodyModelBindingSetting, bool expectedModelStateIsValid)
        {
            // Arrange
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person5)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));
                    request.ContentType = "application/json";
                });

            var optionsAccessor = testContext.GetService<IOptions<MvcOptions>>();
            optionsAccessor.Value.AllowEmptyInputInBodyModelBinding = allowEmptyInputInBodyModelBindingSetting;
            var modelState = testContext.ModelState;

            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(optionsAccessor.Value);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<Person5>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);
            
            if (expectedModelStateIsValid)
            {
                Assert.True(modelState.IsValid);
            }
            else
            {
                Assert.False(modelState.IsValid);
                var entry = Assert.Single(modelState);
                Assert.Equal("CustomParameter.Address", entry.Key);
                var street = entry.Value;
                Assert.Equal(ModelValidationState.Invalid, street.ValidationState);
                var error = Assert.Single(street.Errors);

                // Since the message doesn't come from DataAnnotations, we don't have a way to get the
                // exact string, so just check it's nonempty.
                Assert.NotEmpty(error.ErrorMessage);
            }
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                BindingInfo = new BindingInfo
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person2),
                Name = "param-name",
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes(inputText));
                    request.ContentType = "application/json";
                });
            var httpContext = testContext.HttpContext;
            var modelState = testContext.ModelState;

            var streetRequired = ValidationAttributeUtil.GetRequiredErrorMessage("Street");

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var boundPerson = Assert.IsType<Person2>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);

            Assert.False(modelState.IsValid);
            var entry = Assert.Single(modelState);
            Assert.Equal("CustomParameter.Address.Street", entry.Key);
            var street = entry.Value;
            Assert.Equal(ModelValidationState.Invalid, street.ValidationState);
            var error = Assert.Single(street.Errors);
            Assert.Equal(streetRequired, error.ErrorMessage);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                BindingInfo = new BindingInfo
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person3),
                Name = "param-name",
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes(inputText));
                    request.ContentType = "application/json";
                });
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            Assert.IsType<Person3>(modelBindingResult.Model);

            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
        }

        private class Person6
        {
            public Address6 Address { get; set; }
        }

        private class Address6
        {
            public string Street { get; set; }
        }

        // [FromBody] cannot be associated with a type. But a [FromBody] or [ModelBinder] subclass or custom
        // IBindingSourceMetadata implementation might not have the same restriction. Make sure the metadata is honored
        // when such an attribute is associated with a class somewhere in the type hierarchy of an action parameter.
        [Theory]
        [MemberData(
            nameof(BinderTypeBasedModelBinderIntegrationTest.NullAndEmptyBindingInfo),
            MemberType = typeof(BinderTypeBasedModelBinderIntegrationTest))]
        public async Task FromBodyOnPropertyType_WithData_Succeeds(BindingInfo bindingInfo)
        {
            // Arrange
            var inputText = "{ \"Street\" : \"someStreet\" }";
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider
                .ForProperty<Person6>(nameof(Person6.Address))
                .BindingDetails(binding => binding.BindingSource = BindingSource.Body);

            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(metadataProvider);
            var parameter = new ParameterDescriptor
            {
                Name = "parameter-name",
                BindingInfo = bindingInfo,
                ParameterType = typeof(Person6),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes(inputText));
                    request.ContentType = "application/json";
                });
            testContext.MetadataProvider = metadataProvider;
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var person = Assert.IsType<Person6>(modelBindingResult.Model);
            Assert.NotNull(person.Address);
            Assert.Equal("someStreet", person.Address.Street, StringComparer.Ordinal);

            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
        }

        // [FromBody] cannot be associated with a type. But a [FromBody] or [ModelBinder] subclass or custom
        // IBindingSourceMetadata implementation might not have the same restriction. Make sure the metadata is honored
        // when such an attribute is associated with an action parameter's type.
        [Theory]
        [MemberData(
            nameof(BinderTypeBasedModelBinderIntegrationTest.NullAndEmptyBindingInfo),
            MemberType = typeof(BinderTypeBasedModelBinderIntegrationTest))]
        public async Task FromBodyOnParameterType_WithData_Succeeds(BindingInfo bindingInfo)
        {
            // Arrange
            var inputText = "{ \"Street\" : \"someStreet\" }";
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider
                .ForType<Address6>()
                .BindingDetails(binding => binding.BindingSource = BindingSource.Body);

            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(metadataProvider);
            var parameter = new ParameterDescriptor
            {
                Name = "parameter-name",
                BindingInfo = bindingInfo,
                ParameterType = typeof(Address6),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes(inputText));
                    request.ContentType = "application/json";
                });
            testContext.MetadataProvider = metadataProvider;
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var address = Assert.IsType<Address6>(modelBindingResult.Model);
            Assert.Equal("someStreet", address.Street, StringComparer.Ordinal);

            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
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