// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

public class ValidationWithRecordIntegrationTests
{
    private record TransferInfo([Range(25, 50)] int AccountId, double Amount);

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

    private record Order1([Required] string CustomerName);

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

    private record Order2([Required] Person2 Customer);

    private record Person2(string Name);

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

    private record Order3(Person3 Customer);

    private record Person3(int Age, [Required] string Name);

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

    private record Order4([Required] List<Item4> Items);

    private record Item4(int ItemId);

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

    private record Order5([Required] int? ProductId, string Name);

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

    private record Order6([StringLength(5, ErrorMessage = "Too Long.")] string Name);

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

    private record Order7(Person7 Customer);

    private record Person7([StringLength(5, ErrorMessage = "Too Long.")] string Name);

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

    private record Order8([ValidatePerson8] Person8 Customer);

    private record Person8(string Name);

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

    private record Order9([ValidateProducts9] List<Product9> Products);

    private record Product9(string Name);

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

    private record Order10([StringLength(5, ErrorMessage = "Too Long.")] string Name);

    [Fact]
    public async Task Validation_StringLengthAttribute_OnPropertyOfCollectionElement_Valid()
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
    public async Task Validation_StringLengthAttribute_OnPropertyOfCollectionElement_Invalid()
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
    public async Task Validation_StringLengthAttribute_OnPropertyOfCollectionElement_NoData()
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

    private record User(int Id, uint Zip);

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
        Assert.Equal("The value 'bill' is not valid.", error.ErrorMessage);
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
        Assert.Equal("The value '-123' is not valid.", error.ErrorMessage);
    }

    private record NeverValid(string NeverValidProperty) : IValidatableObject
    {
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

    private record ValidateSomeProperties(
        [Display(Name = "Not ever valid")] NeverValid NeverValidBecauseType,

        [NeverValid]
            [Display(Name = "Never valid")]
            string NeverValidBecauseAttribute,

        [ValidateNever]
            [NeverValid]
            string ValidateNever)
    {

        [ValidateNever]
        public int ValidateNeverLength => ValidateNever.Length;
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

    private record ValidateSomePropertiesSometimes(string Control)
    {
        [ValidateSometimes(nameof(Control))]
        [Range(0, 10)]
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

    // This type has a IPropertyValidationFilter declared on a property, but no validators.
    // We should expect validation to short-circuit
    private record ValidateSomePropertiesSometimesWithoutValidation(string Control)
    {
        [ValidateSometimes(nameof(Control))]
        public int ControlLength => Control.Length;
    }

    [Fact]
    public async Task PropertyToSometimesSkip_IsNotValidated_IfNoValidationAttributesExistButPropertyValidationFilterExists()
    {
        // Arrange
        var parameter = new ParameterDescriptor
        {
            Name = "parameter",
            ParameterType = typeof(ValidateSomePropertiesSometimesWithoutValidation),
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
        var model = Assert.IsType<ValidateSomePropertiesSometimesWithoutValidation>(result.Model);
        Assert.Null(model.Control);

        // Note this Exception is not thrown earlier.
        Assert.Throws<NullReferenceException>(() => model.ControlLength);

        Assert.True(modelState.IsValid);
        var kvp = Assert.Single(modelState);
        Assert.Equal(nameof(ValidateSomePropertiesSometimesWithoutValidation.ControlLength), kvp.Key);
        Assert.Equal(ModelValidationState.Valid, kvp.Value.ValidationState);
    }

    private record Order11
    (
        IEnumerable<Address> ShippingAddresses,

        Address HomeAddress,

        [FromBody]
            Address OfficeAddress
    );

    private record Address
    (
        int Street,

        string State,

        [Range(10000, 99999)]
            int Zip,

        Country Country
    );

    private record Country(string Name);

    [Fact]
    public async Task TypeBasedExclusion_ForBodyAndNonBodyBoundModels()
    {
        // Arrange
        var parameter = new ParameterDescriptor
        {
            Name = "parameter",
            ParameterType = typeof(Order11)
        };

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
            });

        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
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
        var options = new TestMvcOptions().Value;
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(options);
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
            updateRequest: request =>
            {
                request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{ message: \"Hello\" }"));
                request.ContentType = "application/json";
            },
            mvcOptions: options);

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
    // isn't even examined when it comes to suppressing validation.
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

    private record Greeting([StringLength(5)] string Message);

    [Fact]
    public async Task Validation_NoAttributeInGraphOfObjects_WithDefaultValidatorProviders()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order12),
            BindingInfo = new BindingInfo
            {
                BindingSource = BindingSource.Body
            },
        };

        var input = new Order12(10, new byte[40]);

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(input)));
            request.ContentType = "application/json";
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Order12>(modelBindingResult.Model);
        Assert.Equal(input.Id, model.Id);
        Assert.Equal(input.OrderFile, model.OrderFile);
        Assert.Null(model.RelatedOrders);

        Assert.Empty(modelState);
        Assert.Equal(ModelValidationState.Valid, modelState.ValidationState);
    }

    private record Order12(int Id, byte[] OrderFile)
    {
        public IList<Order12> RelatedOrders { get; set; }
    }

    [Fact]
    public async Task Validation_ListOfType_NoValidatorOnParameter()
    {
        // Arrange
        var parameterInfo = GetType().GetMethod(nameof(Validation_ListOfType_NoValidatorOnParameterTestMethod), BindingFlags.NonPublic | BindingFlags.Static)
            .GetParameters()
            .First();

        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var modelMetadata = modelMetadataProvider.GetMetadataForParameter(parameterInfo);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(modelMetadataProvider);

        var parameter = new ParameterDescriptor()
        {
            Name = parameterInfo.Name,
            ParameterType = parameterInfo.ParameterType,
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?[0]=1&[1]=2");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext, modelMetadataProvider, modelMetadata);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<List<int>>(modelBindingResult.Model);
        Assert.Equal(new[] { 1, 2 }, model);

        Assert.False(modelMetadata.HasValidators);

        Assert.True(modelState.IsValid);
        Assert.Equal(ModelValidationState.Valid, modelState.ValidationState);

        var entry = Assert.Single(modelState, e => e.Key == "[0]").Value;
        Assert.Equal("1", entry.AttemptedValue);
        Assert.Equal("1", entry.RawValue);
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);

        entry = Assert.Single(modelState, e => e.Key == "[1]").Value;
        Assert.Equal("2", entry.AttemptedValue);
        Assert.Equal("2", entry.RawValue);
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
    }

    private static void Validation_ListOfType_NoValidatorOnParameterTestMethod(List<int> parameter) { }

    [Fact]
    public async Task Validation_ListOfType_ValidatorOnParameter()
    {
        // Arrange
        var parameterInfo = GetType().GetMethod(nameof(Validation_ListOfType_ValidatorOnParameterTestMethod), BindingFlags.NonPublic | BindingFlags.Static)
            .GetParameters()
            .First();

        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var modelMetadata = modelMetadataProvider.GetMetadataForParameter(parameterInfo);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(modelMetadataProvider);

        var parameter = new ParameterDescriptor()
        {
            Name = parameterInfo.Name,
            ParameterType = parameterInfo.ParameterType,
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?[0]=1&[1]=2");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext, modelMetadataProvider, modelMetadata);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<List<int>>(modelBindingResult.Model);
        Assert.Equal(new[] { 1, 2 }, model);

        Assert.True(modelMetadata.HasValidators);

        Assert.False(modelState.IsValid);
        Assert.Equal(ModelValidationState.Invalid, modelState.ValidationState);

        var entry = Assert.Single(modelState, e => e.Key == "").Value;
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

        entry = Assert.Single(modelState, e => e.Key == "[0]").Value;
        Assert.Equal("1", entry.AttemptedValue);
        Assert.Equal("1", entry.RawValue);
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);

        entry = Assert.Single(modelState, e => e.Key == "[1]").Value;
        Assert.Equal("2", entry.AttemptedValue);
        Assert.Equal("2", entry.RawValue);
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
    }

    private static void Validation_ListOfType_ValidatorOnParameterTestMethod([ConsistentMinLength(3)] List<int> parameter) { }

    private class ConsistentMinLength : ValidationAttribute
    {
        private readonly int _length;

        public ConsistentMinLength(int length)
        {
            _length = length;
        }

        public override bool IsValid(object value)
        {
            return value is ICollection collection && collection.Count >= _length;
        }
    }

    [Fact]
    public async Task Validation_CollectionOfType_ValidatorOnElement()
    {
        // Arrange
        var parameterInfo = GetType().GetMethod(nameof(Validation_CollectionOfType_ValidatorOnElementTestMethod), BindingFlags.NonPublic | BindingFlags.Static)
            .GetParameters()
            .First();

        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var modelMetadata = modelMetadataProvider.GetMetadataForParameter(parameterInfo);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(modelMetadataProvider);

        var parameter = new ParameterDescriptor()
        {
            Name = parameterInfo.Name,
            ParameterType = parameterInfo.ParameterType,
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?p[0].Id=1&p[1].Id=2");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext, modelMetadataProvider, modelMetadata);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Collection<InvalidEvenIds>>(modelBindingResult.Model);
        Assert.Equal(1, model[0].Id);
        Assert.Equal(2, model[1].Id);

        Assert.True(modelMetadata.HasValidators);

        Assert.False(modelState.IsValid);
        Assert.Equal(ModelValidationState.Invalid, modelState.ValidationState);

        var entry = Assert.Single(modelState, e => e.Key == "p[0].Id").Value;
        Assert.Equal("1", entry.AttemptedValue);
        Assert.Equal("1", entry.RawValue);
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);

        entry = Assert.Single(modelState, e => e.Key == "p[1]").Value;
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

        entry = Assert.Single(modelState, e => e.Key == "p[1].Id").Value;
        Assert.Equal("2", entry.AttemptedValue);
        Assert.Equal("2", entry.RawValue);
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
    }

    private static void Validation_CollectionOfType_ValidatorOnElementTestMethod(Collection<InvalidEvenIds> p) { }

    public class InvalidEvenIds : IValidatableObject
    {
        public int Id { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Id % 2 == 0)
            {
                yield return new ValidationResult("Failed validation");
            }
        }
    }

    [Fact]
    public async Task Validation_DictionaryType_NoValidators()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(IDictionary<string, int>)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter[0].Key=key0&parameter[0].Value=10");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Dictionary<string, int>>(modelBindingResult.Model);
        Assert.Collection(
            model.OrderBy(k => k.Key),
            kvp =>
            {
                Assert.Equal("key0", kvp.Key);
                Assert.Equal(10, kvp.Value);
            });

        Assert.True(modelState.IsValid);
        Assert.Equal(ModelValidationState.Valid, modelState.ValidationState);

        var entry = Assert.Single(modelState, e => e.Key == "parameter[0].Key").Value;
        Assert.Equal("key0", entry.AttemptedValue);
        Assert.Equal("key0", entry.RawValue);
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);

        entry = Assert.Single(modelState, e => e.Key == "parameter[0].Value").Value;
        Assert.Equal("10", entry.AttemptedValue);
        Assert.Equal("10", entry.RawValue);
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
    }

    [Fact]
    public async Task Validation_DictionaryType_ValueHasValidators()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, NeverValid>)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter[0].Key=key0&parameter[0].Value.NeverValidProperty=value0");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Dictionary<string, NeverValid>>(modelBindingResult.Model);
        Assert.Collection(
            model.OrderBy(k => k.Key),
            kvp =>
            {
                Assert.Equal("key0", kvp.Key);
                Assert.Equal("value0", kvp.Value.NeverValidProperty);
            });

        Assert.False(modelState.IsValid);
        Assert.Equal(ModelValidationState.Invalid, modelState.ValidationState);

        var entry = Assert.Single(modelState, e => e.Key == "parameter[0].Key").Value;
        Assert.Equal("key0", entry.AttemptedValue);
        Assert.Equal("key0", entry.RawValue);
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);

        entry = Assert.Single(modelState, e => e.Key == "parameter[0].Value.NeverValidProperty").Value;
        Assert.Equal("value0", entry.AttemptedValue);
        Assert.Equal("value0", entry.RawValue);
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);

        entry = Assert.Single(modelState, e => e.Key == "parameter[0].Value").Value;
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        Assert.Single(entry.Errors);
    }

    [Fact]
    public async Task Validation_TopLevelProperty_NoValidation()
    {
        // Arrange
        var modelType = typeof(Validation_TopLevelPropertyController);
        var propertyInfo = modelType.GetProperty(nameof(Validation_TopLevelPropertyController.Model));

        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var modelMetadata = modelMetadataProvider.GetMetadataForProperty(propertyInfo, propertyInfo.PropertyType);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(modelMetadataProvider);

        var parameter = new ParameterDescriptor()
        {
            Name = propertyInfo.Name,
            ParameterType = propertyInfo.PropertyType,
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Model.Id=12");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext, modelMetadataProvider, modelMetadata);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Validation_TopLevelPropertyModel>(modelBindingResult.Model);
        Assert.Equal(12, model.Id);

        Assert.False(modelMetadata.HasValidators);

        Assert.True(modelState.IsValid);
        Assert.Equal(ModelValidationState.Valid, modelState.ValidationState);

        var entry = Assert.Single(modelState, e => e.Key == "Model.Id").Value;
        Assert.Equal("12", entry.AttemptedValue);
        Assert.Equal("12", entry.RawValue);
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
    }

    public record Validation_TopLevelPropertyModel(int Id);

    private class Validation_TopLevelPropertyController
    {
        public Validation_TopLevelPropertyModel Model { get; set; }
    }

    [Fact]
    public async Task Validation_TopLevelProperty_ValidationOnProperty()
    {
        // Arrange
        var modelType = typeof(Validation_TopLevelProperty_ValidationOnPropertyController);
        var propertyInfo = modelType.GetProperty(nameof(Validation_TopLevelProperty_ValidationOnPropertyController.Model));

        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var modelMetadata = modelMetadataProvider.GetMetadataForProperty(propertyInfo, propertyInfo.PropertyType);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(modelMetadataProvider);

        var parameter = new ParameterDescriptor()
        {
            Name = propertyInfo.Name,
            ParameterType = propertyInfo.PropertyType,
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Model.Id=12");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext, modelMetadataProvider, modelMetadata);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Validation_TopLevelPropertyModel>(modelBindingResult.Model);
        Assert.Equal(12, model.Id);

        Assert.True(modelMetadata.HasValidators);

        Assert.False(modelState.IsValid);
        Assert.Equal(ModelValidationState.Invalid, modelState.ValidationState);

        var entry = Assert.Single(modelState, e => e.Key == "Model.Id").Value;
        Assert.Equal("12", entry.AttemptedValue);
        Assert.Equal("12", entry.RawValue);
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);

        entry = Assert.Single(modelState, e => e.Key == "Model").Value;
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
    }

    public class Validation_TopLevelProperty_ValidationOnPropertyController
    {
        [CustomValidation(typeof(Validation_TopLevelProperty_ValidationOnPropertyController), nameof(Validate))]
        public Validation_TopLevelPropertyModel Model { get; set; }

        public static ValidationResult Validate(ValidationContext context)
        {
            return new ValidationResult("Invalid result");
        }
    }

    [Fact]
    public async Task Validation_InfinitelyRecursiveType_NoValidators()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(RecursiveModel)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Property1=8");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<RecursiveModel>(modelBindingResult.Model);
        Assert.Equal(8, model.Property1);

        Assert.True(modelState.IsValid);
        Assert.Equal(ModelValidationState.Valid, modelState.ValidationState);

        var entry = Assert.Single(modelState, e => e.Key == "Property1").Value;
        Assert.Equal("8", entry.AttemptedValue);
        Assert.Equal("8", entry.RawValue);
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
    }

    private record RecursiveModel(int Property1)
    {
        public RecursiveModel Property2 { get; set; }

        public RecursiveModel Property3 => new RecursiveModel(Property1);
    }

    [Fact]
    public async Task Validation_InifnitelyRecursiveModel_ValidationOnTopLevelParameter()
    {
        // Arrange
        var parameterInfo = GetType().GetMethod(nameof(Validation_InifnitelyRecursiveModel_ValidationOnTopLevelParameterMethod), BindingFlags.NonPublic | BindingFlags.Static)
            .GetParameters()
            .First();

        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var modelMetadata = modelMetadataProvider.GetMetadataForParameter(parameterInfo);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(modelMetadataProvider);

        var parameter = new ParameterDescriptor()
        {
            Name = parameterInfo.Name,
            ParameterType = parameterInfo.ParameterType,
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Property1=8");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext, modelMetadataProvider, modelMetadata);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<RecursiveModel>(modelBindingResult.Model);
        Assert.Equal(8, model.Property1);

        Assert.True(modelState.IsValid);
        Assert.Equal(ModelValidationState.Valid, modelState.ValidationState);

        var entry = Assert.Single(modelState, e => e.Key == "Property1").Value;
        Assert.Equal("8", entry.AttemptedValue);
        Assert.Equal("8", entry.RawValue);
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
    }

    private static void Validation_InifnitelyRecursiveModel_ValidationOnTopLevelParameterMethod([Required] RecursiveModel model) { }

#pragma warning disable CS8907 // Parameter is unread. Did you forget to use it to initialize the property with that name?
    private record RecordTypeWithValidatorsOnProperties(string Property1)
#pragma warning restore CS8907 // Parameter is unread. Did you forget to use it to initialize the property with that name?
    {
        [Required]
        public string Property1 { get; init; }
    }

    [Fact]
    public async Task Validation_ValidatorsDefinedOnRecordTypeProperties()
    {
        // Arrange
        var modelType = typeof(RecordTypeWithValidatorsOnProperties);
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var modelMetadata = modelMetadataProvider.GetMetadataForType(modelType);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(modelMetadataProvider);
        var expected = $"Record type '{modelType}' has validation metadata defined on property 'Property1' that will be ignored. " +
            "'Property1' is a parameter in the record primary constructor and validation metadata must be associated with the constructor parameter.";

        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = modelType,
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Property1=8");
        });

        var modelState = testContext.ModelState;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            parameterBinder.BindModelAsync(parameter, testContext, modelMetadataProvider, modelMetadata));

        Assert.Equal(expected, ex.Message);
    }

#pragma warning disable CS8907 // Parameter is unread. Did you forget to use it to initialize the property with that name?
    private record RecordTypeWithValidatorsOnPropertiesAndParameters([Required] string Property1)
#pragma warning restore CS8907 // Parameter is unread. Did you forget to use it to initialize the property with that name?
    {
        [Required]
        public string Property1 { get; init; }
    }

    [Fact]
    public async Task Validation_ValidatorsDefinedOnRecordTypePropertiesAndParameters()
    {
        // Arrange
        var modelType = typeof(RecordTypeWithValidatorsOnPropertiesAndParameters);
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var modelMetadata = modelMetadataProvider.GetMetadataForType(modelType);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(modelMetadataProvider);
        var expected = $"Record type '{modelType}' has validation metadata defined on property 'Property1' that will be ignored. " +
            "'Property1' is a parameter in the record primary constructor and validation metadata must be associated with the constructor parameter.";

        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = modelType,
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Property1=8");
        });

        var modelState = testContext.ModelState;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            parameterBinder.BindModelAsync(parameter, testContext, modelMetadataProvider, modelMetadata));

        Assert.Equal(expected, ex.Message);
    }

#pragma warning disable CS8907 // Parameter is unread. Did you forget to use it to initialize the property with that name?
    private record RecordTypeWithValidatorsOnMixOfPropertiesAndParameters([Required] string Property1, string Property2)
#pragma warning restore CS8907 // Parameter is unread. Did you forget to use it to initialize the property with that name?
    {
        [Required]
        public string Property2 { get; init; }
    }

    [Fact]
    public async Task Validation_ValidatorsDefinedOnMixOfRecordTypePropertiesAndParameters()
    {
        // Variation of Validation_ValidatorsDefinedOnRecordTypePropertiesAndParameters, but validators
        // appear on a mix of properties and parameters.
        // Arrange
        var modelType = typeof(RecordTypeWithValidatorsOnMixOfPropertiesAndParameters);
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var modelMetadata = modelMetadataProvider.GetMetadataForType(modelType);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(modelMetadataProvider);
        var expected = $"Record type '{modelType}' has validation metadata defined on property 'Property2' that will be ignored. " +
            "'Property2' is a parameter in the record primary constructor and validation metadata must be associated with the constructor parameter.";

        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = modelType,
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Property1=8");
        });

        var modelState = testContext.ModelState;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            parameterBinder.BindModelAsync(parameter, testContext, modelMetadataProvider, modelMetadata));

        Assert.Equal(expected, ex.Message);
    }

    private record RecordTypeWithPropertiesAndParameters([Required] string Property1)
    {
        [Required]
        public string Property2 { get; init; }
    }

    [Fact]
    public async Task Validation_ValidatorsOnParametersAndProperties()
    {
        // Arrange
        var modelType = typeof(RecordTypeWithPropertiesAndParameters);
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var modelMetadata = modelMetadataProvider.GetMetadataForType(modelType);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(modelMetadataProvider);

        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = modelType,
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Property1=SomeValue");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        Assert.Equal(2, modelState.Count);
        Assert.Equal(1, modelState.ErrorCount);
        Assert.False(modelState.IsValid);

        var entry = Assert.Single(modelState, e => e.Key == "Property1").Value;
        Assert.Equal("SomeValue", entry.AttemptedValue);
        Assert.Equal("SomeValue", entry.RawValue);
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);

        entry = Assert.Single(modelState, e => e.Key == "Property2").Value;
        Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
    }

    private static void AssertRequiredError(string key, ModelError error)
    {
        Assert.Equal(ValidationAttributeUtil.GetRequiredErrorMessage(key), error.ErrorMessage);
        Assert.Null(error.Exception);
    }
}
