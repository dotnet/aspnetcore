// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    public class ValidationIntegrationTests
    {
        private class TransferInfo
        {
            [Range(25, 50)]
            public int AccountId { get; set; }

            public double Amount { get; set; }
        }

        private class TestController { }

        public static TheoryData<List<ParameterDescriptor>> MultipleActionParametersAndValidationData
        {
            get
            {
                return new TheoryData<List<ParameterDescriptor>>
                {
                    // Irrespective of the order in which the parameters are defined on the action,
                    // the validation on the TransferInfo's AccountId should occur.
                    // Here 'accountId' parameter is bound by the prefix 'accountId' while the 'transferInfo'
                    // property is bound using the empty prefix and the 'TransferInfo' property names.
                    new List<ParameterDescriptor>()
                    {
                        new ParameterDescriptor()
                        {
                            Name = "accountId",
                            ParameterType = typeof(int)
                        },
                        new ParameterDescriptor()
                        {
                            Name = "transferInfo",
                            ParameterType = typeof(TransferInfo),
                            BindingInfo = new BindingInfo()
                            {
                                BindingSource = BindingSource.Body
                            }
                        }
                    },
                    new List<ParameterDescriptor>()
                    {
                        new ParameterDescriptor()
                        {
                            Name = "transferInfo",
                            ParameterType = typeof(TransferInfo),
                            BindingInfo = new BindingInfo()
                            {
                                BindingSource = BindingSource.Body
                            }
                        },
                        new ParameterDescriptor()
                        {
                            Name = "accountId",
                            ParameterType = typeof(int)
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(MultipleActionParametersAndValidationData))]
        public async Task ValidationIsTriggered_OnFromBodyModels(List<ParameterDescriptor> parameters)
        {
            // Arrange
            var actionDescriptor = new ControllerActionDescriptor()
            {
                BoundProperties = new List<ParameterDescriptor>(),
                Parameters = parameters
            };
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = new QueryString("?accountId=30");
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"accountId\": 15,\"amount\": 250.0}"));
                    request.ContentType = "application/json";
                },
                actionDescriptor: actionDescriptor);

            var modelState = testContext.ModelState;

            // Act
            foreach (var parameter in parameters)
            {
                await parameterBinder.BindModelAsync(parameter, testContext);
            }

            // Assert
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(
                modelState,
                e => string.Equals(e.Key, "AccountId", StringComparison.OrdinalIgnoreCase)).Value;
            var error = Assert.Single(entry.Errors);
            Assert.Equal(ValidationAttributeUtil.GetRangeErrorMessage(25, 50, "AccountId"), error.ErrorMessage);
        }

        [Theory]
        [MemberData(nameof(MultipleActionParametersAndValidationData))]
        public async Task MultipleActionParameter_ValidModelState(List<ParameterDescriptor> parameters)
        {
            // Since validation attribute is only present on the FromBody model's property(TransferInfo's AccountId),
            // validation should not trigger for the parameter which is bound from Uri.

            // Arrange
            var actionDescriptor = new ControllerActionDescriptor()
            {
                BoundProperties = new List<ParameterDescriptor>(),
                Parameters = parameters
            };
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = new QueryString("?accountId=10");
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"accountId\": 40,\"amount\": 250.0}"));
                    request.ContentType = "application/json";
                },
                actionDescriptor: actionDescriptor);

            var modelState = testContext.ModelState;

            // Act
            foreach (var parameter in parameters)
            {
                await parameterBinder.BindModelAsync(parameter, testContext);
            }

            // Assert
            Assert.True(modelState.IsValid);
        }

        private class Order1
        {
            [Required]
            public string CustomerName { get; set; }
        }

        [Fact]
        public async Task Validation_RequiredAttribute_OnSimpleTypeProperty_WithData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order1)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.CustomerName=bill");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order1>(modelBindingResult.Model);
            Assert.Equal("bill", model.CustomerName);

            Assert.Single(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.CustomerName").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
            Assert.Empty(entry.Errors);
        }

        [Fact]
        public async Task Validation_RequiredAttribute_OnSimpleTypeProperty_NoData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order1)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order1>(modelBindingResult.Model);
            Assert.Null(model.CustomerName);

            Assert.Single(modelState);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "CustomerName").Value;
            Assert.Null(entry.RawValue);
            Assert.Null(entry.AttemptedValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

            var error = Assert.Single(entry.Errors);
            AssertRequiredError("CustomerName", error);
        }

        private class Order2
        {
            [Required]
            public Person2 Customer { get; set; }
        }

        private class Person2
        {
            public string Name { get; set; }
        }

        [Fact]
        public async Task Validation_RequiredAttribute_OnPOCOProperty_WithData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order2)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Name=bill");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order2>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal("bill", model.Customer.Name);

            Assert.Single(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
            Assert.Empty(entry.Errors);
        }

        [Fact]
        public async Task Validation_RequiredAttribute_OnPOCOProperty_NoData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order2)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order2>(modelBindingResult.Model);
            Assert.Null(model.Customer);

            Assert.Single(modelState);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "Customer").Value;
            Assert.Null(entry.RawValue);
            Assert.Null(entry.AttemptedValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

            var error = Assert.Single(entry.Errors);
            AssertRequiredError("Customer", error);
        }

        private class Order3
        {
            public Person3 Customer { get; set; }
        }

        private class Person3
        {
            public int Age { get; set; }

            [Required]
            public string Name { get; set; }
        }

        [Fact]
        public async Task Validation_RequiredAttribute_OnNestedSimpleTypeProperty_WithData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order3)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Name=bill");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order3>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal("bill", model.Customer.Name);

            Assert.Single(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
            Assert.Empty(entry.Errors);
        }

        [Fact]
        public async Task Validation_RequiredAttribute_OnNestedSimpleTypeProperty_NoDataForRequiredProperty()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order3)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                // Force creation of the Customer model.
                request.QueryString = new QueryString("?parameter.Customer.Age=17");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order3>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal(17, model.Customer.Age);
            Assert.Null(model.Customer.Name);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Null(entry.AttemptedValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

            var error = Assert.Single(entry.Errors);
            AssertRequiredError("Name", error);
        }

        private class Order4
        {
            [Required]
            public List<Item4> Items { get; set; }
        }

        private class Item4
        {
            public int ItemId { get; set; }
        }

        [Fact]
        public async Task Validation_RequiredAttribute_OnCollectionProperty_WithData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order4)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?Items[0].ItemId=17");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order4>(modelBindingResult.Model);
            Assert.NotNull(model.Items);
            Assert.Equal(17, Assert.Single(model.Items).ItemId);

            Assert.Single(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "Items[0].ItemId").Value;
            Assert.Equal("17", entry.AttemptedValue);
            Assert.Equal("17", entry.RawValue);
            Assert.Empty(entry.Errors);
        }

        [Fact]
        public async Task Validation_RequiredAttribute_OnCollectionProperty_NoData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order4)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                // Force creation of the Customer model.
                request.QueryString = new QueryString("?");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order4>(modelBindingResult.Model);
            Assert.Null(model.Items);

            Assert.Single(modelState);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "Items").Value;
            Assert.Null(entry.RawValue);
            Assert.Null(entry.AttemptedValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

            var error = Assert.Single(entry.Errors);
            AssertRequiredError("Items", error);
        }

        private class Order5
        {
            [Required]
            public int? ProductId { get; set; }

            public string Name { get; set; }
        }

        [Fact]
        public async Task Validation_RequiredAttribute_OnPOCOPropertyOfBoundElement_WithData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<Order5>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter[0].ProductId=17");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<Order5>>(modelBindingResult.Model);
            Assert.Equal(17, Assert.Single(model).ProductId);

            Assert.Single(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter[0].ProductId").Value;
            Assert.Equal("17", entry.AttemptedValue);
            Assert.Equal("17", entry.RawValue);
            Assert.Empty(entry.Errors);
        }

        [Fact]
        public async Task Validation_RequiredAttribute_OnPOCOPropertyOfBoundElement_NoDataForRequiredProperty()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<Order5>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                // Force creation of the Customer model.
                request.QueryString = new QueryString("?parameter[0].Name=bill");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<Order5>>(modelBindingResult.Model);
            var item = Assert.Single(model);
            Assert.Null(item.ProductId);
            Assert.Equal("bill", item.Name);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter[0].ProductId").Value;
            Assert.Null(entry.RawValue);
            Assert.Null(entry.AttemptedValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

            var error = Assert.Single(entry.Errors);
            AssertRequiredError("ProductId", error);
        }

        private class Order6
        {
            [StringLength(5, ErrorMessage = "Too Long.")]
            public string Name { get; set; }
        }

        [Fact]
        public async Task Validation_StringLengthAttribute_OnPropertyOfPOCO_Valid()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order6)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Name=bill");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order6>(modelBindingResult.Model);
            Assert.Equal("bill", model.Name);

            Assert.Single(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
            Assert.Empty(entry.Errors);
        }

        [Fact]
        public async Task Validation_StringLengthAttribute_OnPropertyOfPOCO_Invalid()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order6)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Name=billybob");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order6>(modelBindingResult.Model);
            Assert.Equal("billybob", model.Name);

            Assert.Single(modelState);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Name").Value;
            Assert.Equal("billybob", entry.AttemptedValue);
            Assert.Equal("billybob", entry.RawValue);

            var error = Assert.Single(entry.Errors);
            Assert.Equal("Too Long.", error.ErrorMessage);
            Assert.Null(error.Exception);
        }

        private class Order7
        {
            public Person7 Customer { get; set; }
        }

        private class Person7
        {
            [StringLength(5, ErrorMessage = "Too Long.")]
            public string Name { get; set; }
        }

        [Fact]
        public async Task Validation_StringLengthAttribute_OnPropertyOfNestedPOCO_Valid()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order7)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Name=bill");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order7>(modelBindingResult.Model);
            Assert.Equal("bill", model.Customer.Name);

            Assert.Single(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
            Assert.Empty(entry.Errors);
        }

        [Fact]
        public async Task Validation_StringLengthAttribute_OnPropertyOfNestedPOCO_Invalid()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order7)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Name=billybob");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order7>(modelBindingResult.Model);
            Assert.Equal("billybob", model.Customer.Name);

            Assert.Single(modelState);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Name").Value;
            Assert.Equal("billybob", entry.AttemptedValue);
            Assert.Equal("billybob", entry.RawValue);

            var error = Assert.Single(entry.Errors);
            Assert.Equal("Too Long.", error.ErrorMessage);
            Assert.Null(error.Exception);
        }

        [Fact]
        public async Task Validation_StringLengthAttribute_OnPropertyOfNestedPOCO_NoData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order7)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order7>(modelBindingResult.Model);
            Assert.Null(model.Customer);

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class Order8
        {
            [ValidatePerson8]
            public Person8 Customer { get; set; }
        }

        private class Person8
        {
            public string Name { get; set; }
        }

        private class ValidatePerson8Attribute : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                if (((Person8)value).Name == "bill")
                {
                    return null;
                }
                else
                {
                    return new ValidationResult("Invalid Person.");
                }
            }
        }

        [Fact]
        public async Task Validation_CustomAttribute_OnPOCOProperty_Valid()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order8)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Name=bill");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order8>(modelBindingResult.Model);
            Assert.Equal("bill", model.Customer.Name);

            Assert.Single(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
            Assert.Empty(entry.Errors);
        }

        [Fact]
        public async Task Validation_CustomAttribute_OnPOCOProperty_Invalid()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order8)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Name=billybob");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order8>(modelBindingResult.Model);
            Assert.Equal("billybob", model.Customer.Name);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Name").Value;
            Assert.Equal("billybob", entry.AttemptedValue);
            Assert.Equal("billybob", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "parameter.Customer").Value;
            Assert.Null(entry.RawValue);
            Assert.Null(entry.AttemptedValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
            var error = Assert.Single(entry.Errors);
            Assert.Equal("Invalid Person.", error.ErrorMessage);
            Assert.Null(error.Exception);
        }

        private class Order9
        {
            [ValidateProducts9]
            public List<Product9> Products { get; set; }
        }

        private class Product9
        {
            public string Name { get; set; }
        }

        private class ValidateProducts9Attribute : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                if (((List<Product9>)value)[0].Name == "bill")
                {
                    return null;
                }
                else
                {
                    return new ValidationResult("Invalid Product.");
                }
            }
        }

        [Fact]
        public async Task Validation_CustomAttribute_OnCollectionElement_Valid()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order9)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Products[0].Name=bill");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order9>(modelBindingResult.Model);
            Assert.Equal("bill", Assert.Single(model.Products).Name);

            Assert.Single(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Products[0].Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
            Assert.Empty(entry.Errors);
        }

        [Fact]
        public async Task Validation_CustomAttribute_OnCollectionElement_Invalid()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order9)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Products[0].Name=billybob");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order9>(modelBindingResult.Model);
            Assert.Equal("billybob", Assert.Single(model.Products).Name);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Products[0].Name").Value;
            Assert.Equal("billybob", entry.AttemptedValue);
            Assert.Equal("billybob", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "parameter.Products").Value;
            Assert.Null(entry.RawValue);
            Assert.Null(entry.AttemptedValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

            var error = Assert.Single(entry.Errors);
            Assert.Equal("Invalid Product.", error.ErrorMessage);
            Assert.Null(error.Exception);
        }

        private class Order10
        {
            [StringLength(5, ErrorMessage = "Too Long.")]
            public string Name { get; set; }
        }

        [Fact]
        public async Task Validation_StringLengthAttribute_OnProperyOfCollectionElement_Valid()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<Order10>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter[0].Name=bill");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<Order10>>(modelBindingResult.Model);
            Assert.Equal("bill", Assert.Single(model).Name);

            Assert.Single(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter[0].Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
            Assert.Empty(entry.Errors);
        }

        [Fact]
        public async Task Validation_StringLengthAttribute_OnProperyOfCollectionElement_Invalid()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<Order10>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter[0].Name=billybob");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<Order10>>(modelBindingResult.Model);
            Assert.Equal("billybob", Assert.Single(model).Name);

            Assert.Single(modelState);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter[0].Name").Value;
            Assert.Equal("billybob", entry.AttemptedValue);
            Assert.Equal("billybob", entry.RawValue);

            var error = Assert.Single(entry.Errors);
            Assert.Equal("Too Long.", error.ErrorMessage);
            Assert.Null(error.Exception);
        }

        [Fact]
        public async Task Validation_StringLengthAttribute_OnProperyOfCollectionElement_NoData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<Order10>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<Order10>>(modelBindingResult.Model);
            Assert.Empty(model);

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class User
        {
            public int Id { get; set; }

            public uint Zip { get; set; }

        }

        [Fact]
        public async Task Validation_FormatException_ShowsInvalidValueMessage_OnSimpleTypeProperty()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(User)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?Id=bill");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<User>(modelBindingResult.Model);
            Assert.Equal(0, model.Id);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var state = Assert.Single(modelState);
            Assert.Equal("Id", state.Key);
            var entry = state.Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
            Assert.Single(entry.Errors);

            var error = entry.Errors[0];
            Assert.Equal("The value 'bill' is not valid for Id.", error.ErrorMessage);
        }

        [Fact]
        public async Task Validation_OverflowException_ShowsInvalidValueMessage_OnSimpleTypeProperty()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(User)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?Zip=-123");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<User>(modelBindingResult.Model);
            Assert.Equal<uint>(0, model.Zip);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var state = Assert.Single(modelState);
            Assert.Equal("Zip", state.Key);
            var entry = state.Value;
            Assert.Equal("-123", entry.AttemptedValue);
            Assert.Equal("-123", entry.RawValue);
            Assert.Single(entry.Errors);

            var error = entry.Errors[0];
            Assert.Equal("The value '-123' is not valid for Zip.", error.ErrorMessage);
        }

        private class NeverValid : IValidatableObject
        {
            public string NeverValidProperty { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                var result = new ValidationResult(
                    $"'{validationContext.MemberName}' (display: '{validationContext.DisplayName}') is not valid due " +
                    $"to its {nameof(NeverValid)} type.");
                return new[] { result };
            }
        }

        private class NeverValidAttribute : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                // By default, ValidationVisitor visits _all_ properties within a non-null complex object.
                // But, like most reasonable ValidationAttributes, NeverValidAttribute ignores null property values.
                if (value == null)
                {
                    return ValidationResult.Success;
                }

                return new ValidationResult(
                    $"'{validationContext.MemberName}' (display: '{validationContext.DisplayName}') is not valid due " +
                    $"to its associated {nameof(NeverValidAttribute)}.");
            }
        }

        private class ValidateSomeProperties
        {
            [Display(Name = "Not ever valid")]
            public NeverValid NeverValidBecauseType { get; set; }

            [NeverValid]
            [Display(Name = "Never valid")]
            public string NeverValidBecauseAttribute { get; set; }

            [ValidateNever]
            [NeverValid]
            public string ValidateNever { get; set; }

            [ValidateNever]
            public int ValidateNeverLength => ValidateNever.Length;
        }

        [ValidateNever]
        private class ValidateNoProperties : ValidateSomeProperties
        {
        }

        [Fact]
        public async Task IValidatableObject_IsValidated()
        {
            // Arrange
            var parameter = new ParameterDescriptor
            {
                Name = "parameter",
                ParameterType = typeof(ValidateSomeProperties),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request => request.QueryString
                    = new QueryString($"?{nameof(ValidateSomeProperties.NeverValidBecauseType)}.{nameof(NeverValid.NeverValidProperty)}=1"));

            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var modelState = testContext.ModelState;

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(result.IsModelSet);
            var model = Assert.IsType<ValidateSomeProperties>(result.Model);
            Assert.Equal("1", model.NeverValidBecauseType.NeverValidProperty);

            Assert.False(modelState.IsValid);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.Collection(
                modelState,
                state =>
                {
                    Assert.Equal(nameof(ValidateSomeProperties.NeverValidBecauseType), state.Key);
                    Assert.Equal(ModelValidationState.Invalid, state.Value.ValidationState);

                    var error = Assert.Single(state.Value.Errors);
                    Assert.Equal(
                        "'NeverValidBecauseType' (display: 'Not ever valid') is not valid due to its NeverValid type.",
                        error.ErrorMessage);
                    Assert.Null(error.Exception);
                },
                state =>
                {
                    Assert.Equal(
                        $"{nameof(ValidateSomeProperties.NeverValidBecauseType)}.{nameof(NeverValid.NeverValidProperty)}",
                        state.Key);
                    Assert.Equal(ModelValidationState.Valid, state.Value.ValidationState);
                });
        }

        [Fact]
        public async Task CustomValidationAttribute_IsValidated()
        {
            // Arrange
            var parameter = new ParameterDescriptor
            {
                Name = "parameter",
                ParameterType = typeof(ValidateSomeProperties),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request => request.QueryString
                    = new QueryString($"?{nameof(ValidateSomeProperties.NeverValidBecauseAttribute)}=1"));

            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var modelState = testContext.ModelState;

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(result.IsModelSet);
            var model = Assert.IsType<ValidateSomeProperties>(result.Model);
            Assert.Equal("1", model.NeverValidBecauseAttribute);

            Assert.False(modelState.IsValid);
            Assert.Equal(1, modelState.ErrorCount);
            var kvp = Assert.Single(modelState);
            Assert.Equal(nameof(ValidateSomeProperties.NeverValidBecauseAttribute), kvp.Key);
            var state = kvp.Value;
            Assert.NotNull(state);
            Assert.Equal(ModelValidationState.Invalid, state.ValidationState);
            var error = Assert.Single(state.Errors);
            Assert.Equal(
                "'NeverValidBecauseAttribute' (display: 'Never valid') is not valid due to its associated NeverValidAttribute.",
                error.ErrorMessage);
            Assert.Null(error.Exception);
        }

        [Fact]
        public async Task ValidateNeverProperty_IsSkipped()
        {
            // Arrange
            var parameter = new ParameterDescriptor
            {
                Name = "parameter",
                ParameterType = typeof(ValidateSomeProperties),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request => request.QueryString
                    = new QueryString($"?{nameof(ValidateSomeProperties.ValidateNever)}=1"));

            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var modelState = testContext.ModelState;

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(result.IsModelSet);
            var model = Assert.IsType<ValidateSomeProperties>(result.Model);
            Assert.Equal("1", model.ValidateNever);

            Assert.True(modelState.IsValid);
            var kvp = Assert.Single(modelState);
            Assert.Equal(nameof(ValidateSomeProperties.ValidateNever), kvp.Key);
            var state = kvp.Value;
            Assert.NotNull(state);
            Assert.Equal(ModelValidationState.Skipped, state.ValidationState);
        }

        [Fact]
        public async Task ValidateNeverProperty_IsSkippedWithoutAccessingModel()
        {
            // Arrange
            var parameter = new ParameterDescriptor
            {
                Name = "parameter",
                ParameterType = typeof(ValidateSomeProperties),
            };

            var testContext = ModelBindingTestHelper.GetTestContext();
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var modelState = testContext.ModelState;

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(result.IsModelSet);
            var model = Assert.IsType<ValidateSomeProperties>(result.Model);

            // Note this Exception is not thrown earlier.
            Assert.Throws<NullReferenceException>(() => model.ValidateNeverLength);

            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
        }

        [Theory]
        [InlineData(nameof(ValidateSomeProperties.NeverValidBecauseType) + "." + nameof(NeverValid.NeverValidProperty))]
        [InlineData(nameof(ValidateSomeProperties.NeverValidBecauseAttribute))]
        [InlineData(nameof(ValidateSomeProperties.ValidateNever))]
        public async Task PropertyWithinValidateNeverType_IsSkipped(string propertyName)
        {
            // Arrange
            var parameter = new ParameterDescriptor
            {
                Name = "parameter",
                ParameterType = typeof(ValidateNoProperties),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request => request.QueryString = new QueryString($"?{propertyName}=1"));

            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var modelState = testContext.ModelState;

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(result.IsModelSet);
            Assert.IsType<ValidateNoProperties>(result.Model);

            Assert.True(modelState.IsValid);
            var kvp = Assert.Single(modelState);
            Assert.Equal(propertyName, kvp.Key);
            var state = kvp.Value;
            Assert.NotNull(state);
            Assert.Equal(ModelValidationState.Skipped, state.ValidationState);
        }

        private class ValidateSometimesAttribute : Attribute, IPropertyValidationFilter
        {
            private readonly string _otherProperty;

            public ValidateSometimesAttribute(string otherProperty)
            {
                // Would null-check otherProperty in real life.
                _otherProperty = otherProperty;
            }

            public bool ShouldValidateEntry(ValidationEntry entry, ValidationEntry parentEntry)
            {
                if (entry.Metadata.MetadataKind == ModelMetadataKind.Property &&
                    parentEntry.Metadata != null)
                {
                    // In real life, would throw an InvalidOperationException if otherProperty were null i.e. the
                    // property was not known. Could also assert container is non-null (see ValidationVisitor).
                    var container = parentEntry.Model;
                    var otherProperty = parentEntry.Metadata.Properties[_otherProperty];
                    if (otherProperty.PropertyGetter(container) == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private class ValidateSomePropertiesSometimes
        {
            public string Control { get; set; }

            [ValidateSometimes(nameof(Control))]
            public int ControlLength => Control.Length;
        }

        [Fact]
        public async Task PropertyToSometimesSkip_IsSkipped_IfControlIsNull()
        {
            // Arrange
            var parameter = new ParameterDescriptor
            {
                Name = "parameter",
                ParameterType = typeof(ValidateSomePropertiesSometimes),
            };

            var testContext = ModelBindingTestHelper.GetTestContext();
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var modelState = testContext.ModelState;

            // Add an entry for the ControlLength property so that we can observe Skipped versus Valid states.
            modelState.SetModelValue(
                nameof(ValidateSomePropertiesSometimes.ControlLength),
                rawValue: null,
                attemptedValue: null);

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(result.IsModelSet);
            var model = Assert.IsType<ValidateSomePropertiesSometimes>(result.Model);
            Assert.Null(model.Control);

            // Note this Exception is not thrown earlier.
            Assert.Throws<NullReferenceException>(() => model.ControlLength);

            Assert.True(modelState.IsValid);
            var kvp = Assert.Single(modelState);
            Assert.Equal(nameof(ValidateSomePropertiesSometimes.ControlLength), kvp.Key);
            Assert.Equal(ModelValidationState.Skipped, kvp.Value.ValidationState);
        }

        [Fact]
        public async Task PropertyToSometimesSkip_IsValidated_IfControlIsNotNull()
        {
            // Arrange
            var parameter = new ParameterDescriptor
            {
                Name = "parameter",
                ParameterType = typeof(ValidateSomePropertiesSometimes),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request => request.QueryString = new QueryString(
                    $"?{nameof(ValidateSomePropertiesSometimes.Control)}=1"));

            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var modelState = testContext.ModelState;

            // Add an entry for the ControlLength property so that we can observe Skipped versus Valid states.
            modelState.SetModelValue(
                nameof(ValidateSomePropertiesSometimes.ControlLength),
                rawValue: null,
                attemptedValue: null);

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(result.IsModelSet);
            var model = Assert.IsType<ValidateSomePropertiesSometimes>(result.Model);
            Assert.Equal("1", model.Control);
            Assert.Equal(1, model.ControlLength);

            Assert.True(modelState.IsValid);
            Assert.Collection(
                modelState,
                state => Assert.Equal(nameof(ValidateSomePropertiesSometimes.Control), state.Key),
                state =>
                {
                    Assert.Equal(nameof(ValidateSomePropertiesSometimes.ControlLength), state.Key);
                    Assert.Equal(ModelValidationState.Valid, state.Value.ValidationState);
                });
        }

        private class Order11
        {
            public IEnumerable<Address> ShippingAddresses { get; set; }

            public Address HomeAddress { get; set; }

            [FromBody]
            public Address OfficeAddress { get; set; }
        }

        private class Address
        {
            public int Street { get; set; }

            public string State { get; set; }

            [Range(10000, 99999)]
            public int Zip { get; set; }

            public Country Country { get; set; }
        }

        private class Country
        {
            public string Name { get; set; }
        }

        [Fact]
        public async Task TypeBasedExclusion_ForBodyAndNonBodyBoundModels()
        {
            // Arrange
            var parameter = new ParameterDescriptor
            {
                Name = "parameter",
                ParameterType = typeof(Order11)
            };

            MvcOptions testOptions = null;
            var input = "{\"Zip\":\"47\"}";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString =
                        new QueryString("?HomeAddress.Country.Name=US&ShippingAddresses[0].Zip=45&HomeAddress.Zip=46");
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes(input));
                    request.ContentType = "application/json";
                },
                options =>
                {
                    options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(Address)));
                    testOptions = options;
                });

            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testOptions);
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            Assert.Equal(3, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "HomeAddress.Country.Name").Value;
            Assert.Equal("US", entry.AttemptedValue);
            Assert.Equal("US", entry.RawValue);
            Assert.Equal(ModelValidationState.Skipped, entry.ValidationState);

            entry = Assert.Single(modelState, e => e.Key == "ShippingAddresses[0].Zip").Value;
            Assert.Equal("45", entry.AttemptedValue);
            Assert.Equal("45", entry.RawValue);
            Assert.Equal(ModelValidationState.Skipped, entry.ValidationState);

            entry = Assert.Single(modelState, e => e.Key == "HomeAddress.Zip").Value;
            Assert.Equal("46", entry.AttemptedValue);
            Assert.Equal("46", entry.RawValue);
            Assert.Equal(ModelValidationState.Skipped, entry.ValidationState);
        }

        [Fact]
        public async Task FromBody_JToken_ExcludedFromValidation()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(new TestMvcOptions().Value);
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo
                {
                    BinderModelName = "CustomParameter",
                    BindingSource = BindingSource.Body
                },
                ParameterType = typeof(JToken)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{ message: \"Hello\" }"));
                    request.ContentType = "application/json";
                });

            var httpContext = testContext.HttpContext;
            var modelState = testContext.ModelState;

            // We need to add another model state entry which should get marked as skipped so
            // we can prove that the JObject was skipped.
            modelState.SetModelValue("CustomParameter.message", "Hello", "Hello");

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            Assert.NotNull(modelBindingResult.Model);
            var message = Assert.IsType<JObject>(modelBindingResult.Model).GetValue("message").Value<string>();
            Assert.Equal("Hello", message);

            Assert.True(modelState.IsValid);
            Assert.Single(modelState);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "CustomParameter.message");
            Assert.Equal(ModelValidationState.Skipped, entry.Value.ValidationState);
        }

        // Regression test for https://github.com/aspnet/Mvc/issues/3743
        //
        // A cancellation token that's bound with the empty prefix will end up suppressing
        // the empty prefix. Since the empty prefix is a prefix of everything, this will
        // basically result in clearing out all model errors, which is BAD.
        //
        // The fix is to treat non-user-input as have a key of null, which means that the MSD
        // isn't even examined when it comes to supressing validation.
        [Fact]
        public async Task CancellationToken_WithEmptyPrefix_DoesNotSuppressUnrelatedErrors()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(new TestMvcOptions().Value);
            var parameter = new ParameterDescriptor
            {
                Name = "cancellationToken",
                ParameterType = typeof(CancellationToken)
            };

            var testContext = ModelBindingTestHelper.GetTestContext();

            var httpContext = testContext.HttpContext;
            var modelState = testContext.ModelState;

            // We need to add another model state entry - we want this to be ignored.
            modelState.SetModelValue("message", "Hello", "Hello");

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            Assert.NotNull(modelBindingResult.Model);
            Assert.IsType<CancellationToken>(modelBindingResult.Model);

            Assert.False(modelState.IsValid);
            Assert.Single(modelState);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "message");
            Assert.Equal(ModelValidationState.Unvalidated, entry.Value.ValidationState);
        }

        // Similar to CancellationToken_WithEmptyPrefix_DoesNotSuppressUnrelatedErrors - binding the body
        // with the empty prefix should not cause unrelated modelstate entries to get suppressed.
        [Fact]
        public async Task FromBody_WithEmptyPrefix_DoesNotSuppressUnrelatedErrors_Valid()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(new TestMvcOptions().Value);
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo
                {
                    BindingSource = BindingSource.Body
                },
                ParameterType = typeof(Greeting)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{ message: \"Hello\" }"));
                    request.ContentType = "application/json";
                });

            var httpContext = testContext.HttpContext;
            var modelState = testContext.ModelState;

            // We need to add another model state entry which should not get changed.
            modelState.SetModelValue("other.key", "1", "1");

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            Assert.NotNull(modelBindingResult.Model);
            var message = Assert.IsType<Greeting>(modelBindingResult.Model).Message;
            Assert.Equal("Hello", message);

            Assert.False(modelState.IsValid);
            Assert.Single(modelState);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "other.key");
            Assert.Equal(ModelValidationState.Unvalidated, entry.Value.ValidationState);
        }

        // Similar to CancellationToken_WithEmptyPrefix_DoesNotSuppressUnrelatedErrors - binding the body
        // with the empty prefix should not cause unrelated modelstate entries to get suppressed.
        [Fact]
        public async Task FromBody_WithEmptyPrefix_DoesNotSuppressUnrelatedErrors_Invalid()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(new TestMvcOptions().Value);
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo
                {
                    BindingSource = BindingSource.Body
                },
                ParameterType = typeof(Greeting)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    // This string is too long and will have a validation error.
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{ message: \"Hello There\" }"));
                    request.ContentType = "application/json";
                });

            var httpContext = testContext.HttpContext;
            var modelState = testContext.ModelState;

            // We need to add another model state entry which should not get changed.
            modelState.SetModelValue("other.key", "1", "1");

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            Assert.NotNull(modelBindingResult.Model);
            var message = Assert.IsType<Greeting>(modelBindingResult.Model).Message;
            Assert.Equal("Hello There", message);

            Assert.False(modelState.IsValid);
            Assert.Equal(2, modelState.Count);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "Message");
            Assert.Equal(ModelValidationState.Invalid, entry.Value.ValidationState);

            entry = Assert.Single(modelState, kvp => kvp.Key == "other.key");
            Assert.Equal(ModelValidationState.Unvalidated, entry.Value.ValidationState);
        }

        private class Greeting
        {
            [StringLength(5)]
            public string Message { get; set; }
        }

        private static void AssertRequiredError(string key, ModelError error)
        {
            Assert.Equal(ValidationAttributeUtil.GetRequiredErrorMessage(key), error.ErrorMessage);
            Assert.Null(error.Exception);
        }
    }
}
