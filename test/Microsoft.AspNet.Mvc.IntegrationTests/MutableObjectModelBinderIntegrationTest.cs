// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features.Internal;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Controllers;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    // Integration tests targeting the behavior of the MutableObjectModelBinder and related classes
    // with other model binders.
    public class MutableObjectModelBinderIntegrationTest
    {
        private const string AddressBodyContent = "{ \"street\" : \"" + AddressStreetContent + "\" }";
        private const string AddressStreetContent = "1 Microsoft Way";

        private static readonly byte[] ByteArrayContent = Encoding.BigEndianUnicode.GetBytes("abcd");
        private static readonly string ByteArrayEncoded = Convert.ToBase64String(ByteArrayContent);

        private class Order1
        {
            public int ProductId { get; set; }

            public Person1 Customer { get; set; }
        }

        private class Person1
        {
            public string Name { get; set; }

            [FromBody]
            public Address1 Address { get; set; }
        }

        private class Address1
        {
            public string Street { get; set; }
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsNestedPOCO_WithBodyModelBinder_WithPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order1)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Name=bill");
                SetJsonBodyContent(request, AddressBodyContent);
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order1>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal("bill", model.Customer.Name);
            Assert.NotNull(model.Customer.Address);
            Assert.Equal(AddressStreetContent, model.Customer.Address.Street);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Address").Value;
            Assert.Null(entry.AttemptedValue); // ModelState entries for body don't include original text.
            Assert.Same(model.Customer.Address, entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsNestedPOCO_WithBodyModelBinder_WithEmptyPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order1)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?Customer.Name=bill");
                SetJsonBodyContent(request, AddressBodyContent);
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order1>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal("bill", model.Customer.Name);
            Assert.NotNull(model.Customer.Address);
            Assert.Equal(AddressStreetContent, model.Customer.Address.Street);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "Customer.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "Customer.Address").Value;
            Assert.Null(entry.AttemptedValue); // ModelState entries for body don't include original text.
            Assert.Same(model.Customer.Address, entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsNestedPOCO_WithBodyModelBinder_WithPrefix_NoBodyData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order1)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Name=bill");
                request.ContentType = "application/json";
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order1>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal("bill", model.Customer.Name);
            Assert.Null(model.Customer.Address);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Address").Value;
            Assert.Null(entry.AttemptedValue);
            Assert.Null(entry.RawValue);
            entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
        }

        // We don't provide enough data in this test for the 'Person' model to be created. So even though there is
        // body data in the request, it won't be used.
        [Fact]
        public async Task MutableObjectModelBinder_BindsNestedPOCO_WithBodyModelBinder_WithPrefix_PartialData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order1)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter.ProductId=10");
                SetJsonBodyContent(request, AddressBodyContent);
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order1>(modelBindingResult.Model);
            Assert.Null(model.Customer);
            Assert.Equal(10, model.ProductId);

            Assert.Equal(1, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.ProductId").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
        }

        // We don't provide enough data in this test for the 'Person' model to be created. So even though there is
        // body data in the request, it won't be used.
        [Fact]
        public async Task MutableObjectModelBinder_BindsNestedPOCO_WithBodyModelBinder_WithPrefix_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order1)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?");
                SetJsonBodyContent(request, AddressBodyContent);
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order1>(modelBindingResult.Model);
            Assert.Null(model.Customer);

            Assert.Equal(0, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class Order3
        {
            public int ProductId { get; set; }

            public Person3 Customer { get; set; }
        }

        private class Person3
        {
            public string Name { get; set; }

            public byte[] Token { get; set; }
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsNestedPOCO_WithByteArrayModelBinder_WithPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order3)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString =
                    new QueryString("?parameter.Customer.Name=bill&parameter.Customer.Token=" + ByteArrayEncoded);
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order3>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal("bill", model.Customer.Name);
            Assert.Equal(ByteArrayContent, model.Customer.Token);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Token").Value;
            Assert.Equal(ByteArrayEncoded, entry.AttemptedValue);
            Assert.Equal(ByteArrayEncoded, entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsNestedPOCO_WithByteArrayModelBinder_WithEmptyPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order3)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?Customer.Name=bill&Customer.Token=" + ByteArrayEncoded);
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order3>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal("bill", model.Customer.Name);
            Assert.Equal(ByteArrayContent, model.Customer.Token);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "Customer.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "Customer.Token").Value;
            Assert.Equal(ByteArrayEncoded, entry.AttemptedValue);
            Assert.Equal(ByteArrayEncoded, entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsNestedPOCO_WithByteArrayModelBinder_WithPrefix_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order3)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Name=bill");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order3>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal("bill", model.Customer.Name);
            Assert.Null(model.Customer.Token);

            Assert.Equal(1, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
        }

        private class Order4
        {
            public int ProductId { get; set; }

            public Person4 Customer { get; set; }
        }

        private class Person4
        {
            public string Name { get; set; }

            public IEnumerable<IFormFile> Documents { get; set; }
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsNestedPOCO_WithFormFileModelBinder_WithPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order4)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Name=bill");
                SetFormFileBodyContent(request, "Hello, World!", "parameter.Customer.Documents");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order4>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal("bill", model.Customer.Name);
            Assert.Single(model.Customer.Documents);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Documents").Value;
            Assert.Null(entry.AttemptedValue); // FormFile entries for body don't include original text.
            Assert.Null(entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsNestedPOCO_WithFormFileModelBinder_WithEmptyPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order4)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?Customer.Name=bill");
                SetFormFileBodyContent(request, "Hello, World!", "Customer.Documents");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order4>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal("bill", model.Customer.Name);
            Assert.Single(model.Customer.Documents);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "Customer.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "Customer.Documents").Value;
            Assert.Null(entry.AttemptedValue); // FormFile entries don't include the model.
            Assert.Null(entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsNestedPOCO_WithFormFileModelBinder_WithPrefix_NoBodyData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order4)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Name=bill");

                // Deliberately leaving out any form data.
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order4>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal("bill", model.Customer.Name);
            Assert.Empty(model.Customer.Documents);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Documents").Value;
            Assert.Null(entry.AttemptedValue); // FormFile entries don't include the model.
            Assert.Null(entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
        }

        // We don't provide enough data in this test for the 'Person' model to be created. So even though there are
        // form files in the request, it won't be used.
        [Fact]
        public async Task MutableObjectModelBinder_BindsNestedPOCO_WithFormFileModelBinder_WithPrefix_PartialData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order4)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter.ProductId=10");
                SetFormFileBodyContent(request, "Hello, World!", "parameter.Customer.Documents");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order4>(modelBindingResult.Model);
            Assert.Null(model.Customer);
            Assert.Equal(10, model.ProductId);

            Assert.Equal(1, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.ProductId").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
        }

        // We don't provide enough data in this test for the 'Person' model to be created. So even though there is
        // body data in the request, it won't be used.
        [Fact]
        public async Task MutableObjectModelBinder_BindsNestedPOCO_WithFormFileModelBinder_WithPrefix_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order4)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?");
                SetFormFileBodyContent(request, "Hello, World!", "parameter.Customer.Documents");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order4>(modelBindingResult.Model);
            Assert.Null(model.Customer);

            Assert.Equal(0, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class Order5
        {
            public string Name { get; set; }

            public int[] ProductIds { get; set; }
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsArrayProperty_WithPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order5)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString =
                    new QueryString("?parameter.Name=bill&parameter.ProductIds[0]=10&parameter.ProductIds[1]=11");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order5>(modelBindingResult.Model);
            Assert.Equal("bill", model.Name);
            Assert.Equal(new int[] { 10, 11 }, model.ProductIds);

            Assert.Equal(3, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "parameter.ProductIds[0]").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "parameter.ProductIds[1]").Value;
            Assert.Equal("11", entry.AttemptedValue);
            Assert.Equal("11", entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsArrayProperty_EmptyPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order5)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?Name=bill&ProductIds[0]=10&ProductIds[1]=11");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order5>(modelBindingResult.Model);
            Assert.Equal("bill", model.Name);
            Assert.Equal(new int[] { 10, 11 }, model.ProductIds);

            Assert.Equal(3, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "ProductIds[0]").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "ProductIds[1]").Value;
            Assert.Equal("11", entry.AttemptedValue);
            Assert.Equal("11", entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsArrayProperty_NoCollectionData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order5)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Name=bill");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order5>(modelBindingResult.Model);
            Assert.Equal("bill", model.Name);
            Assert.Null(model.ProductIds);

            Assert.Equal(1, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsArrayProperty_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order5)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order5>(modelBindingResult.Model);
            Assert.Null(model.Name);
            Assert.Null(model.ProductIds);

            Assert.Equal(0, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class Order6
        {
            public string Name { get; set; }

            public List<int> ProductIds { get; set; }
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsListProperty_WithPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order6)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString =
                    new QueryString("?parameter.Name=bill&parameter.ProductIds[0]=10&parameter.ProductIds[1]=11");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order6>(modelBindingResult.Model);
            Assert.Equal("bill", model.Name);
            Assert.Equal(new List<int>() { 10, 11 }, model.ProductIds);

            Assert.Equal(3, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "parameter.ProductIds[0]").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "parameter.ProductIds[1]").Value;
            Assert.Equal("11", entry.AttemptedValue);
            Assert.Equal("11", entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsListProperty_EmptyPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order6)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?Name=bill&ProductIds[0]=10&ProductIds[1]=11");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order6>(modelBindingResult.Model);
            Assert.Equal("bill", model.Name);
            Assert.Equal(new List<int>() { 10, 11 }, model.ProductIds);

            Assert.Equal(3, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "ProductIds[0]").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "ProductIds[1]").Value;
            Assert.Equal("11", entry.AttemptedValue);
            Assert.Equal("11", entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsListProperty_NoCollectionData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order6)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Name=bill");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order6>(modelBindingResult.Model);
            Assert.Equal("bill", model.Name);
            Assert.Null(model.ProductIds);

            Assert.Equal(1, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsListProperty_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order6)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order6>(modelBindingResult.Model);
            Assert.Null(model.Name);
            Assert.Null(model.ProductIds);

            Assert.Equal(0, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class Order7
        {
            public string Name { get; set; }

            public Dictionary<string, int> ProductIds { get; set; }
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsDictionaryProperty_WithPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order7)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString =
                    new QueryString("?parameter.Name=bill&parameter.ProductIds[0].Key=key0&parameter.ProductIds[0].Value=10");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order7>(modelBindingResult.Model);
            Assert.Equal("bill", model.Name);
            Assert.Equal(new Dictionary<string, int>() { { "key0", 10 } }, model.ProductIds);

            Assert.Equal(3, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "parameter.ProductIds[0].Key").Value;
            Assert.Equal("key0", entry.AttemptedValue);
            Assert.Equal("key0", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "parameter.ProductIds[0].Value").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsDictionaryProperty_EmptyPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order7)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?Name=bill&ProductIds[0].Key=key0&ProductIds[0].Value=10");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order7>(modelBindingResult.Model);
            Assert.Equal("bill", model.Name);
            Assert.Equal(new Dictionary<string, int>() { { "key0", 10 } }, model.ProductIds);

            Assert.Equal(3, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "ProductIds[0].Key").Value;
            Assert.Equal("key0", entry.AttemptedValue);
            Assert.Equal("key0", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "ProductIds[0].Value").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsDictionaryProperty_NoCollectionData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order7)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Name=bill");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order7>(modelBindingResult.Model);
            Assert.Equal("bill", model.Name);
            Assert.Null(model.ProductIds);

            Assert.Equal(1, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsDictionaryProperty_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order7)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order7>(modelBindingResult.Model);
            Assert.Null(model.Name);
            Assert.Null(model.ProductIds);

            Assert.Equal(0, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class Order8
        {
            public string Name { get; set; }

            public KeyValuePair<string, int> ProductId { get; set; }
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsKeyValuePairProperty_WithPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order8)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString =
                    new QueryString("?parameter.Name=bill&parameter.ProductId.Key=key0&parameter.ProductId.Value=10");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order8>(modelBindingResult.Model);
            Assert.Equal("bill", model.Name);
            Assert.Equal(new KeyValuePair<string, int>("key0", 10), model.ProductId);

            Assert.Equal(3, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "parameter.ProductId.Key").Value;
            Assert.Equal("key0", entry.AttemptedValue);
            Assert.Equal("key0", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "parameter.ProductId.Value").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsKeyValuePairProperty_EmptyPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order8)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?Name=bill&ProductId.Key=key0&ProductId.Value=10");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order8>(modelBindingResult.Model);
            Assert.Equal("bill", model.Name);
            Assert.Equal(new KeyValuePair<string, int>("key0", 10), model.ProductId);

            Assert.Equal(3, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "ProductId.Key").Value;
            Assert.Equal("key0", entry.AttemptedValue);
            Assert.Equal("key0", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "ProductId.Value").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsKeyValuePairProperty_NoCollectionData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order8)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Name=bill");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order8>(modelBindingResult.Model);
            Assert.Equal("bill", model.Name);
            Assert.Equal(default(KeyValuePair<string, int>), model.ProductId);

            Assert.Equal(1, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsKeyValuePairProperty_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order8)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order8>(modelBindingResult.Model);
            Assert.Null(model.Name);
            Assert.Equal(default(KeyValuePair<string, int>), model.ProductId);

            Assert.Equal(0, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class Order9
        {
            public Person9 Customer { get; set; }
        }

        private class Person9
        {
            [FromBody]
            public Address1 Address { get; set; }
        }

        // If a nested POCO object has all properties bound from a greedy source, then it should be populated
        // if the top-level object is created.
        [Fact]
        public async Task MutableObjectModelBinder_BindsNestedPOCO_WithAllGreedyBoundProperties()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order9)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?");
                SetJsonBodyContent(request, AddressBodyContent);
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order9>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);

            Assert.NotNull(model.Customer.Address);
            Assert.Equal(AddressStreetContent, model.Customer.Address.Street);

            Assert.Equal(1, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "Customer.Address").Value;
            Assert.Null(entry.AttemptedValue);
            Assert.Same(model.Customer.Address, entry.RawValue);
        }

        private class Order10
        {
            [BindRequired]
            public Person10 Customer { get; set; }
        }

        private class Person10
        {
            public string Name { get; set; }
        }

        [Fact]
        public async Task MutableObjectModelBinder_WithRequiredComplexProperty_NoData_GetsErrors()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order10)
            };

            // No Data
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order10>(modelBindingResult.Model);
            Assert.Null(model.Customer);

            Assert.Equal(1, modelState.Count);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "Customer").Value;
            Assert.Null(entry.RawValue);
            Assert.Null(entry.AttemptedValue);
            var error = Assert.Single(modelState["Customer"].Errors);
            Assert.Equal("A value for the 'Customer' property was not provided.", error.ErrorMessage);
        }

        [Fact]
        public async Task MutableObjectModelBinder_WithBindRequired_NoData_AndCustomizedMessage_AddsGivenMessage()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider
                .ForType(typeof(Order10))
                .BindingDetails((Action<ModelBinding.Metadata.BindingMetadata>)(binding =>
                {
                    // A real details provider could customize message based on BindingMetadataProviderContext.
                    binding.ModelBindingMessageProvider.MissingBindRequiredValueAccessor =
                        name => $"Hurts when '{ name }' is not provided.";
                }));
            var argumentBinder = new DefaultControllerActionArgumentBinder(
                metadataProvider,
                ModelBindingTestHelper.GetObjectValidator());
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order10)
            };

            // No Data
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order10>(modelBindingResult.Model);
            Assert.Null(model.Customer);

            Assert.Equal(1, modelState.Count);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "Customer").Value;
            Assert.Null(entry.RawValue);
            Assert.Null(entry.AttemptedValue);
            var error = Assert.Single(modelState["Customer"].Errors);
            Assert.Equal("Hurts when 'Customer' is not provided.", error.ErrorMessage);
        }

        private class Order11
        {
            public Person11 Customer { get; set; }
        }

        private class Person11
        {
            public int Id { get; set; }

            [BindRequired]
            public string Name { get; set; }
        }

        [Fact]
        public async Task MutableObjectModelBinder_WithNestedRequiredProperty_WithPartialData_GetsErrors()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order11)
            };

            // No Data
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Id=123");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order11>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal(123, model.Customer.Id);
            Assert.Null(model.Customer.Name);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Id").Value;
            Assert.Equal("123", entry.RawValue);
            Assert.Equal("123", entry.AttemptedValue);

            entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Null(entry.AttemptedValue);
            var error = Assert.Single(modelState["parameter.Customer.Name"].Errors);
            Assert.Equal("A value for the 'Name' property was not provided.", error.ErrorMessage);
        }

        [Fact]
        public async Task MutableObjectModelBinder_WithNestedRequiredProperty_WithData_EmptyPrefix_GetsErrors()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order11)
            };

            // No Data
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?Customer.Id=123");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order11>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal(123, model.Customer.Id);
            Assert.Null(model.Customer.Name);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "Customer.Id").Value;
            Assert.Equal("123", entry.RawValue);
            Assert.Equal("123", entry.AttemptedValue);

            entry = Assert.Single(modelState, e => e.Key == "Customer.Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Null(entry.AttemptedValue);
            var error = Assert.Single(modelState["Customer.Name"].Errors);
            Assert.Equal("A value for the 'Name' property was not provided.", error.ErrorMessage);
        }

        [Fact]
        public async Task MutableObjectModelBinder_WithNestedRequiredProperty_WithData_CustomPrefix_GetsErrors()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order11),
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "customParameter"
                }
            };

            // No Data
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?customParameter.Customer.Id=123");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order11>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal(123, model.Customer.Id);
            Assert.Null(model.Customer.Name);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "customParameter.Customer.Id").Value;
            Assert.Equal("123", entry.RawValue);
            Assert.Equal("123", entry.AttemptedValue);

            entry = Assert.Single(modelState, e => e.Key == "customParameter.Customer.Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Null(entry.AttemptedValue);
            var error = Assert.Single(modelState["customParameter.Customer.Name"].Errors);
            Assert.Equal("A value for the 'Name' property was not provided.", error.ErrorMessage);
        }

        private class Order12
        {
            [BindRequired]
            public string ProductName { get; set; }
        }

        [Fact]
        public async Task MutableObjectModelBinder_WithRequiredProperty_NoData_GetsErrors()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order12)
            };

            // No Data
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order12>(modelBindingResult.Model);
            Assert.Null(model.ProductName);

            Assert.Equal(1, modelState.Count);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "ProductName").Value;
            Assert.Null(entry.RawValue);
            Assert.Null(entry.AttemptedValue);
            var error = Assert.Single(modelState["ProductName"].Errors);
            Assert.Equal("A value for the 'ProductName' property was not provided.", error.ErrorMessage);
        }

        [Fact]
        public async Task MutableObjectModelBinder_WithRequiredProperty_NoData_CustomPrefix_GetsErros()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order12),
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "customParameter"
                }
            };

            // No Data
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order12>(modelBindingResult.Model);
            Assert.Null(model.ProductName);

            Assert.Equal(1, modelState.Count);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "customParameter.ProductName").Value;
            Assert.Null(entry.RawValue);
            Assert.Null(entry.AttemptedValue);
            var error = Assert.Single(modelState["customParameter.ProductName"].Errors);
            Assert.Equal("A value for the 'ProductName' property was not provided.", error.ErrorMessage);
        }

        [Fact]
        public async Task MutableObjectModelBinder_WithRequiredProperty_WithData_EmptyPrefix_GetsBound()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order12),
            };

            // No Data
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?ProductName=abc");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order12>(modelBindingResult.Model);
            Assert.Equal("abc", model.ProductName);

            Assert.Equal(1, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "ProductName").Value;
            Assert.Equal("abc", entry.RawValue);
            Assert.Equal("abc", entry.AttemptedValue);
        }

        private class Order13
        {
            [BindRequired]
            public List<int> OrderIds { get; set; }
        }

        [Fact]
        public async Task MutableObjectModelBinder_WithRequiredCollectionProperty_NoData_GetsErros()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order13)
            };

            // No Data
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order13>(modelBindingResult.Model);
            Assert.Null(model.OrderIds);

            Assert.Equal(1, modelState.Count);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "OrderIds").Value;
            Assert.Null(entry.RawValue);
            Assert.Null(entry.AttemptedValue);
            var error = Assert.Single(modelState["OrderIds"].Errors);
            Assert.Equal("A value for the 'OrderIds' property was not provided.", error.ErrorMessage);
        }

        [Fact]
        public async Task MutableObjectModelBinder_WithRequiredCollectionProperty_NoData_CustomPrefix_GetsErros()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order13),
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "customParameter"
                }
            };

            // No Data
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order13>(modelBindingResult.Model);
            Assert.Null(model.OrderIds);

            Assert.Equal(1, modelState.Count);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "customParameter.OrderIds").Value;
            Assert.Null(entry.RawValue);
            Assert.Null(entry.AttemptedValue);
            var error = Assert.Single(modelState["customParameter.OrderIds"].Errors);
            Assert.Equal("A value for the 'OrderIds' property was not provided.", error.ErrorMessage);
        }

        [Fact]
        public async Task MutableObjectModelBinder_WithRequiredCollectionProperty_WithData_EmptyPrefix_GetsBound()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order13),
            };

            // No Data
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?OrderIds[0]=123");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order13>(modelBindingResult.Model);
            Assert.Equal(new[] { 123 }, model.OrderIds.ToArray());

            Assert.Equal(1, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "OrderIds[0]").Value;
            Assert.Equal("123", entry.RawValue);
            Assert.Equal("123", entry.AttemptedValue);
        }

        private class Order14
        {
            public int ProductId { get; set; }
        }

        // This covers the case where a key is present, but has an empty value. The type converter
        // will report an error.
        [Fact]
        public async Task MutableObjectModelBinder_BindsPOCO_TypeConvertedPropertyNonConvertableValue_GetsError()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order14)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter.ProductId=");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order14>(modelBindingResult.Model);
            Assert.NotNull(model);
            Assert.Equal(0, model.ProductId);

            Assert.Equal(1, modelState.Count);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.ProductId").Value;
            Assert.Equal(string.Empty, entry.AttemptedValue);
            Assert.Equal(string.Empty, entry.RawValue);

            var error = Assert.Single(entry.Errors);
            Assert.Equal("The value '' is invalid.", error.ErrorMessage);
            Assert.Null(error.Exception);
        }

        // This covers the case where a key is present, but has no value. The type converter
        // will report an error.
        [Fact]
        public async Task MutableObjectModelBinder_BindsPOCO_TypeConvertedPropertyWithNoValue_NoError()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order14)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter.ProductId");
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order14>(modelBindingResult.Model);
            Assert.NotNull(model);
            Assert.Equal(0, model.ProductId);

            Assert.Equal(0, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private static void SetJsonBodyContent(HttpRequest request, string content)
        {
            var stream = new MemoryStream(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content));
            request.Body = stream;
            request.ContentType = "application/json";
        }

        private static void SetFormFileBodyContent(HttpRequest request, string content, string name)
        {
            var fileCollection = new FormFileCollection();
            var formCollection = new FormCollection(new Dictionary<string, StringValues>(), fileCollection);
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            request.Form = formCollection;
            request.ContentType = "multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq";
            request.Headers["Content-Disposition"] = "form-data; name=" + name + "; filename=text.txt";

            fileCollection.Add(new FormFile(memoryStream, 0, memoryStream.Length)
            {
                Headers = request.Headers
            });
        }
    }
}