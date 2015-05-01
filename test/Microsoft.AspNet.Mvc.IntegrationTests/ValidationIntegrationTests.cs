// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public class ValidationIntegrationTests
    {
        private class Order1
        {
            [Required]
            public string CustomerName { get; set; }
        }

        [Fact(Skip = "Extra ModelState key because of #2446")]
        public async Task Validation_RequiredAttribute_OnSimpleTypeProperty_WithData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order1)
            };
            
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter.CustomerName=bill");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order1>(modelBindingResult.Model);
            Assert.Equal("bill", model.CustomerName);

            Assert.Equal(1, modelState.Count); // This fails due to #2446
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.CustomerName").Value;
            Assert.Equal("bill", entry.Value.AttemptedValue);
            Assert.Equal("bill", entry.Value.RawValue);
            Assert.Empty(entry.Errors);
        }

        [Fact(Skip = "Extra ModelState key because of #2446")]
        public async Task Validation_RequiredAttribute_OnSimpleTypeProperty_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order1)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order1>(modelBindingResult.Model);
            Assert.Null(model.CustomerName);

            Assert.Equal(1, modelState.Count); // This fails due to #2446
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "CustomerName").Value;
            Assert.Null(entry.Value);
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

        [Fact(Skip = "Extra ModelState key because of #2446")]
        public async Task Validation_RequiredAttribute_OnPOCOProperty_WithData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order2)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Name=bill");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order2>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal("bill", model.Customer.Name);

            Assert.Equal(1, modelState.Count); // This fails due to #2446
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Name").Value;
            Assert.Equal("bill", entry.Value.AttemptedValue);
            Assert.Equal("bill", entry.Value.RawValue);
            Assert.Empty(entry.Errors);
        }

        [Fact]
        public async Task Validation_RequiredAttribute_OnPOCOProperty_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order2)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order2>(modelBindingResult.Model);
            Assert.Null(model.Customer);

            Assert.Equal(1, modelState.Count);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "Customer").Value;
            Assert.Null(entry.Value);
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

        [Fact(Skip = "Extra ModelState key because of #2446")]
        public async Task Validation_RequiredAttribute_OnNestedSimpleTypeProperty_WithData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order3)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Name=bill");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order3>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal("bill", model.Customer.Name);

            Assert.Equal(1, modelState.Count); // This fails due to #2446
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Name").Value;
            Assert.Equal("bill", entry.Value.AttemptedValue);
            Assert.Equal("bill", entry.Value.RawValue);
            Assert.Empty(entry.Errors);
        }

        [Fact(Skip = "Extra ModelState key because of #2446")]
        public async Task Validation_RequiredAttribute_OnNestedSimpleTypeProperty_NoDataForRequiredProperty()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order3)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                // Force creation of the Customer model.
                request.QueryString = new QueryString("?parameter.Customer.Age=17");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order3>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal(17, model.Customer.Age);
            Assert.Null(model.Customer.Name);

            Assert.Equal(2, modelState.Count); // This fails due to #2446
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Name").Value;
            Assert.Null(entry.Value);
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

        [Fact(Skip = "Extra ModelState key because of #2446")]
        public async Task Validation_RequiredAttribute_OnCollectionProperty_WithData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order4)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?Items[0].ItemId=17");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order4>(modelBindingResult.Model);
            Assert.NotNull(model.Items);
            Assert.Equal(17, Assert.Single(model.Items).ItemId);

            Assert.Equal(1, modelState.Count); // This fails due to #2446
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "Items[0].ItemId").Value;
            Assert.Equal("17", entry.Value.AttemptedValue);
            Assert.Equal("17", entry.Value.RawValue);
            Assert.Empty(entry.Errors);
        }

        [Fact]
        public async Task Validation_RequiredAttribute_OnCollectionProperty_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order4)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                // Force creation of the Customer model.
                request.QueryString = new QueryString("?");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order4>(modelBindingResult.Model);
            Assert.Null(model.Items);

            Assert.Equal(1, modelState.Count);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "Items").Value;
            Assert.Null(entry.Value);
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

        [Fact(Skip = "Extra ModelState key because of #2446")]
        public async Task Validation_RequiredAttribute_OnPOCOPropertyOfBoundElement_WithData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<Order5>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter[0].ProductId=17");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<Order5>>(modelBindingResult.Model);
            Assert.Equal(17, Assert.Single(model).ProductId);

            Assert.Equal(1, modelState.Count); // This fails due to #2446
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter[0].ProductId").Value;
            Assert.Equal("17", entry.Value.AttemptedValue);
            Assert.Equal("17", entry.Value.RawValue);
            Assert.Empty(entry.Errors);
        }

        [Fact]
        public async Task Validation_RequiredAttribute_OnPOCOPropertyOfBoundElement_NoDataForRequiredProperty()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<Order5>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                // Force creation of the Customer model.
                request.QueryString = new QueryString("?parameter[0].Name=bill");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<Order5>>(modelBindingResult.Model);
            var item = Assert.Single(model);
            Assert.Null(item.ProductId);
            Assert.Equal("bill", item.Name);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter[0].ProductId").Value;
            Assert.Null(entry.Value);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

            var error = Assert.Single(entry.Errors);
            AssertRequiredError("ProductId", error);
        }

        private static void AssertRequiredError(string key, ModelError error)
        {
            Assert.Equal(string.Format("The {0} field is required.", key), error.ErrorMessage);
            Assert.Null(error.Exception);
        }
    }
}
