// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    // Integration tests targeting the behavior of the MutableObjectModelBinder and related classes
    // with other model binders.
    public class ComplexTypeModelBinderIntegrationTest
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order1)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Name=bill");
                SetJsonBodyContent(request, AddressBodyContent);
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order1>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal("bill", model.Customer.Name);
            Assert.NotNull(model.Customer.Address);
            Assert.Equal(AddressStreetContent, model.Customer.Address.Street);

            Assert.Single(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsNestedPOCO_WithBodyModelBinder_WithEmptyPrefix_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order1)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?Customer.Name=bill");
                SetJsonBodyContent(request, AddressBodyContent);
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order1>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal("bill", model.Customer.Name);
            Assert.NotNull(model.Customer.Address);
            Assert.Equal(AddressStreetContent, model.Customer.Address.Street);

            Assert.Single(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "Customer.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsNestedPOCO_WithBodyModelBinder_WithPrefix_NoBodyData()
        {
            // Arrange
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order1)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Name=bill");
                request.ContentType = "application/json";
            });

            var optionsAccessor = testContext.GetService<IOptions<MvcOptions>>();
            optionsAccessor.Value.AllowEmptyInputInBodyModelBinding = true;

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(optionsAccessor.Value);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order1>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal("bill", model.Customer.Name);
            Assert.Null(model.Customer.Address);

            Assert.Single(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.Customer.Name").Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
        }

        // We don't provide enough data in this test for the 'Person' model to be created. So even though there is
        // body data in the request, it won't be used.
        [Fact]
        public async Task MutableObjectModelBinder_BindsNestedPOCO_WithBodyModelBinder_WithPrefix_PartialData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order1)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.ProductId=10");
                SetJsonBodyContent(request, AddressBodyContent);
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order1>(modelBindingResult.Model);
            Assert.Null(model.Customer);
            Assert.Equal(10, model.ProductId);

            Assert.Single(modelState);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order1)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
                SetJsonBodyContent(request, AddressBodyContent);
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order1>(modelBindingResult.Model);
            Assert.Null(model.Customer);

            Assert.Empty(modelState);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order3)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString =
                    new QueryString("?parameter.Customer.Name=bill&parameter.Customer.Token=" + ByteArrayEncoded);
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order3)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?Customer.Name=bill&Customer.Token=" + ByteArrayEncoded);
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order3)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Name=bill");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order3>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal("bill", model.Customer.Name);
            Assert.Null(model.Customer.Token);

            Assert.Single(modelState);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order4)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Name=bill");
                SetFormFileBodyContent(request, "Hello, World!", "parameter.Customer.Documents");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order4)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?Customer.Name=bill");
                SetFormFileBodyContent(request, "Hello, World!", "Customer.Documents");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order4)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Name=bill");

                // Deliberately leaving out any form data.
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order4>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);
            Assert.Equal("bill", model.Customer.Name);
            Assert.Null(model.Customer.Documents);

            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var kvp = Assert.Single(modelState);
            Assert.Equal("parameter.Customer.Name", kvp.Key);
            var entry = kvp.Value;
            Assert.Equal("bill", entry.AttemptedValue);
            Assert.Equal("bill", entry.RawValue);
        }

        // We don't provide enough data in this test for the 'Person' model to be created. So even though there are
        // form files in the request, it won't be used.
        [Fact]
        public async Task MutableObjectModelBinder_BindsNestedPOCO_WithFormFileModelBinder_WithPrefix_PartialData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order4)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.ProductId=10");
                SetFormFileBodyContent(request, "Hello, World!", "parameter.Customer.Documents");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order4>(modelBindingResult.Model);
            Assert.Null(model.Customer);
            Assert.Equal(10, model.ProductId);

            Assert.Single(modelState);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order4)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
                SetFormFileBodyContent(request, "Hello, World!", "parameter.Customer.Documents");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order4>(modelBindingResult.Model);
            Assert.Null(model.Customer);

            Assert.Empty(modelState);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order5)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString =
                    new QueryString("?parameter.Name=bill&parameter.ProductIds[0]=10&parameter.ProductIds[1]=11");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order5)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?Name=bill&ProductIds[0]=10&ProductIds[1]=11");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order5)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Name=bill");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order5>(modelBindingResult.Model);
            Assert.Equal("bill", model.Name);
            Assert.Null(model.ProductIds);

            Assert.Single(modelState);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order5)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order5>(modelBindingResult.Model);
            Assert.Null(model.Name);
            Assert.Null(model.ProductIds);

            Assert.Empty(modelState);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order6)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString =
                    new QueryString("?parameter.Name=bill&parameter.ProductIds[0]=10&parameter.ProductIds[1]=11");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order6)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?Name=bill&ProductIds[0]=10&ProductIds[1]=11");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order6)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Name=bill");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order6>(modelBindingResult.Model);
            Assert.Equal("bill", model.Name);
            Assert.Null(model.ProductIds);

            Assert.Single(modelState);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order6)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order6>(modelBindingResult.Model);
            Assert.Null(model.Name);
            Assert.Null(model.ProductIds);

            Assert.Empty(modelState);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order7)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString =
                    new QueryString("?parameter.Name=bill&parameter.ProductIds[0].Key=key0&parameter.ProductIds[0].Value=10");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order7)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?Name=bill&ProductIds[0].Key=key0&ProductIds[0].Value=10");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order7)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Name=bill");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order7>(modelBindingResult.Model);
            Assert.Equal("bill", model.Name);
            Assert.Null(model.ProductIds);

            Assert.Single(modelState);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order7)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order7>(modelBindingResult.Model);
            Assert.Null(model.Name);
            Assert.Null(model.ProductIds);

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        // Dictionary property with an IEnumerable<> value type
        private class Car1
        {
            public string Name { get; set; }

            public Dictionary<string, IEnumerable<SpecDoc>> Specs { get; set; }
        }

        // Dictionary property with an Array value type
        private class Car2
        {
            public string Name { get; set; }

            public Dictionary<string, SpecDoc[]> Specs { get; set; }
        }

        private class Car3
        {
            public string Name { get; set; }

            public IEnumerable<KeyValuePair<string, IEnumerable<SpecDoc>>> Specs { get; set; }
        }

        private class SpecDoc
        {
            public string Name { get; set; }
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsDictionaryProperty_WithIEnumerableComplexTypeValue_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "p",
                ParameterType = typeof(Car1)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                var queryString = "?p.Name=Accord"
                        + "&p.Specs[0].Key=camera_specs"
                        + "&p.Specs[0].Value[0].Name=camera_spec1.txt"
                        + "&p.Specs[0].Value[1].Name=camera_spec2.txt"
                        + "&p.Specs[1].Key=tyre_specs"
                        + "&p.Specs[1].Value[0].Name=tyre_spec1.txt"
                        + "&p.Specs[1].Value[1].Name=tyre_spec2.txt";
                request.QueryString = new QueryString(queryString);
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Car1>(modelBindingResult.Model);
            Assert.Equal("Accord", model.Name);

            Assert.Collection(
                model.Specs,
                (e) =>
                {
                    Assert.Equal("camera_specs", e.Key);
                    Assert.Collection(
                        e.Value,
                        (s) =>
                        {
                            Assert.Equal("camera_spec1.txt", s.Name);
                        },
                        (s) =>
                        {
                            Assert.Equal("camera_spec2.txt", s.Name);
                        });
                },
                (e) =>
                {
                    Assert.Equal("tyre_specs", e.Key);
                    Assert.Collection(
                        e.Value,
                        (s) =>
                        {
                            Assert.Equal("tyre_spec1.txt", s.Name);
                        },
                        (s) =>
                        {
                            Assert.Equal("tyre_spec2.txt", s.Name);
                        });
                });

            Assert.Equal(7, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "p.Name").Value;
            Assert.Equal("Accord", entry.AttemptedValue);
            Assert.Equal("Accord", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs[0].Key").Value;
            Assert.Equal("camera_specs", entry.AttemptedValue);
            Assert.Equal("camera_specs", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs[0].Value[0].Name").Value;
            Assert.Equal("camera_spec1.txt", entry.AttemptedValue);
            Assert.Equal("camera_spec1.txt", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs[0].Value[1].Name").Value;
            Assert.Equal("camera_spec2.txt", entry.AttemptedValue);
            Assert.Equal("camera_spec2.txt", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs[1].Key").Value;
            Assert.Equal("tyre_specs", entry.AttemptedValue);
            Assert.Equal("tyre_specs", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs[1].Value[0].Name").Value;
            Assert.Equal("tyre_spec1.txt", entry.AttemptedValue);
            Assert.Equal("tyre_spec1.txt", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs[1].Value[1].Name").Value;
            Assert.Equal("tyre_spec2.txt", entry.AttemptedValue);
            Assert.Equal("tyre_spec2.txt", entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsDictionaryProperty_WithArrayOfComplexTypeValue_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "p",
                ParameterType = typeof(Car2)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                var queryString = "?p.Name=Accord"
                        + "&p.Specs[0].Key=camera_specs"
                        + "&p.Specs[0].Value[0].Name=camera_spec1.txt"
                        + "&p.Specs[0].Value[1].Name=camera_spec2.txt"
                        + "&p.Specs[1].Key=tyre_specs"
                        + "&p.Specs[1].Value[0].Name=tyre_spec1.txt"
                        + "&p.Specs[1].Value[1].Name=tyre_spec2.txt";
                request.QueryString = new QueryString(queryString);
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Car2>(modelBindingResult.Model);
            Assert.Equal("Accord", model.Name);

            Assert.Collection(
                model.Specs,
                (e) =>
                {
                    Assert.Equal("camera_specs", e.Key);
                    Assert.Collection(
                        e.Value,
                        (s) =>
                        {
                            Assert.Equal("camera_spec1.txt", s.Name);
                        },
                        (s) =>
                        {
                            Assert.Equal("camera_spec2.txt", s.Name);
                        });
                },
                (e) =>
                {
                    Assert.Equal("tyre_specs", e.Key);
                    Assert.Collection(
                        e.Value,
                        (s) =>
                        {
                            Assert.Equal("tyre_spec1.txt", s.Name);
                        },
                        (s) =>
                        {
                            Assert.Equal("tyre_spec2.txt", s.Name);
                        });
                });

            Assert.Equal(7, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "p.Name").Value;
            Assert.Equal("Accord", entry.AttemptedValue);
            Assert.Equal("Accord", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs[0].Key").Value;
            Assert.Equal("camera_specs", entry.AttemptedValue);
            Assert.Equal("camera_specs", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs[0].Value[0].Name").Value;
            Assert.Equal("camera_spec1.txt", entry.AttemptedValue);
            Assert.Equal("camera_spec1.txt", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs[0].Value[1].Name").Value;
            Assert.Equal("camera_spec2.txt", entry.AttemptedValue);
            Assert.Equal("camera_spec2.txt", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs[1].Key").Value;
            Assert.Equal("tyre_specs", entry.AttemptedValue);
            Assert.Equal("tyre_specs", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs[1].Value[0].Name").Value;
            Assert.Equal("tyre_spec1.txt", entry.AttemptedValue);
            Assert.Equal("tyre_spec1.txt", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs[1].Value[1].Name").Value;
            Assert.Equal("tyre_spec2.txt", entry.AttemptedValue);
            Assert.Equal("tyre_spec2.txt", entry.RawValue);
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsDictionaryProperty_WithIEnumerableOfKeyValuePair_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "p",
                ParameterType = typeof(Car3)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                var queryString = "?p.Name=Accord"
                        + "&p.Specs[0].Key=camera_specs"
                        + "&p.Specs[0].Value[0].Name=camera_spec1.txt"
                        + "&p.Specs[0].Value[1].Name=camera_spec2.txt"
                        + "&p.Specs[1].Key=tyre_specs"
                        + "&p.Specs[1].Value[0].Name=tyre_spec1.txt"
                        + "&p.Specs[1].Value[1].Name=tyre_spec2.txt";
                request.QueryString = new QueryString(queryString);
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Car3>(modelBindingResult.Model);
            Assert.Equal("Accord", model.Name);

            Assert.Collection(
                model.Specs,
                (e) =>
                {
                    Assert.Equal("camera_specs", e.Key);
                    Assert.Collection(
                        e.Value,
                        (s) =>
                        {
                            Assert.Equal("camera_spec1.txt", s.Name);
                        },
                        (s) =>
                        {
                            Assert.Equal("camera_spec2.txt", s.Name);
                        });
                },
                (e) =>
                {
                    Assert.Equal("tyre_specs", e.Key);
                    Assert.Collection(
                        e.Value,
                        (s) =>
                        {
                            Assert.Equal("tyre_spec1.txt", s.Name);
                        },
                        (s) =>
                        {
                            Assert.Equal("tyre_spec2.txt", s.Name);
                        });
                });

            Assert.Equal(7, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "p.Name").Value;
            Assert.Equal("Accord", entry.AttemptedValue);
            Assert.Equal("Accord", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs[0].Key").Value;
            Assert.Equal("camera_specs", entry.AttemptedValue);
            Assert.Equal("camera_specs", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs[0].Value[0].Name").Value;
            Assert.Equal("camera_spec1.txt", entry.AttemptedValue);
            Assert.Equal("camera_spec1.txt", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs[0].Value[1].Name").Value;
            Assert.Equal("camera_spec2.txt", entry.AttemptedValue);
            Assert.Equal("camera_spec2.txt", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs[1].Key").Value;
            Assert.Equal("tyre_specs", entry.AttemptedValue);
            Assert.Equal("tyre_specs", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs[1].Value[0].Name").Value;
            Assert.Equal("tyre_spec1.txt", entry.AttemptedValue);
            Assert.Equal("tyre_spec1.txt", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs[1].Value[1].Name").Value;
            Assert.Equal("tyre_spec2.txt", entry.AttemptedValue);
            Assert.Equal("tyre_spec2.txt", entry.RawValue);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order8)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString =
                    new QueryString("?parameter.Name=bill&parameter.ProductId.Key=key0&parameter.ProductId.Value=10");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order8)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?Name=bill&ProductId.Key=key0&ProductId.Value=10");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order8)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Name=bill");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order8>(modelBindingResult.Model);
            Assert.Equal("bill", model.Name);
            Assert.Equal(default(KeyValuePair<string, int>), model.ProductId);

            Assert.Single(modelState);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order8)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order8>(modelBindingResult.Model);
            Assert.Null(model.Name);
            Assert.Equal(default(KeyValuePair<string, int>), model.ProductId);

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class Car4
        {
            public string Name { get; set; }

            public KeyValuePair<string, Dictionary<string, string>> Specs { get; set; }
        }

        [Fact]
        public async Task Foo_MutableObjectModelBinder_BindsKeyValuePairProperty_WithPrefix_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "p",
                ParameterType = typeof(Car4)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                var queryString = "?p.Name=Accord"
                                + "&p.Specs.Key=camera_specs"
                                + "&p.Specs.Value[0].Key=spec1"
                                + "&p.Specs.Value[0].Value=spec1.txt"
                                + "&p.Specs.Value[1].Key=spec2"
                                + "&p.Specs.Value[1].Value=spec2.txt";

                request.QueryString = new QueryString(queryString);
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Car4>(modelBindingResult.Model);
            Assert.Equal("Accord", model.Name);

            Assert.Collection(
                model.Specs.Value,
                (e) =>
                {
                    Assert.Equal("spec1", e.Key);
                    Assert.Equal("spec1.txt", e.Value);
                },
                (e) =>
                {
                    Assert.Equal("spec2", e.Key);
                    Assert.Equal("spec2.txt", e.Value);
                });

            Assert.Equal(6, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "p.Name").Value;
            Assert.Equal("Accord", entry.AttemptedValue);
            Assert.Equal("Accord", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs.Key").Value;
            Assert.Equal("camera_specs", entry.AttemptedValue);
            Assert.Equal("camera_specs", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs.Value[0].Key").Value;
            Assert.Equal("spec1", entry.AttemptedValue);
            Assert.Equal("spec1", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs.Value[0].Value").Value;
            Assert.Equal("spec1.txt", entry.AttemptedValue);
            Assert.Equal("spec1.txt", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs.Value[1].Key").Value;
            Assert.Equal("spec2", entry.AttemptedValue);
            Assert.Equal("spec2", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "p.Specs.Value[1].Value").Value;
            Assert.Equal("spec2.txt", entry.AttemptedValue);
            Assert.Equal("spec2.txt", entry.RawValue);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order9)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
                SetJsonBodyContent(request, AddressBodyContent);
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order9>(modelBindingResult.Model);
            Assert.NotNull(model.Customer);

            Assert.NotNull(model.Customer.Address);
            Assert.Equal(AddressStreetContent, model.Customer.Address.Street);

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order10)
            };

            // No Data
            var testContext = ModelBindingTestHelper.GetTestContext();

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order10>(modelBindingResult.Model);
            Assert.Null(model.Customer);

            Assert.Single(modelState);
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
                .ForProperty(typeof(Order10), nameof(Order10.Customer))
                .BindingDetails((Action<ModelBinding.Metadata.BindingMetadata>)(binding =>
                {
                    // A real details provider could customize message based on BindingMetadataProviderContext.
                    binding.ModelBindingMessageProvider.SetMissingBindRequiredValueAccessor(
                        name => $"Hurts when '{ name }' is not provided.");
                }));

            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(metadataProvider);
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order10)
            };

            // No Data
            var testContext = ModelBindingTestHelper.GetTestContext();

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order10>(modelBindingResult.Model);
            Assert.Null(model.Customer);

            Assert.Single(modelState);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order11)
            };

            // No Data
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.Customer.Id=123");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order11)
            };

            // No Data
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?Customer.Id=123");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
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
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?customParameter.Customer.Id=123");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order12)
            };

            // No Data
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order12>(modelBindingResult.Model);
            Assert.Null(model.ProductName);

            Assert.Single(modelState);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
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
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order12>(modelBindingResult.Model);
            Assert.Null(model.ProductName);

            Assert.Single(modelState);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order12),
            };

            // No Data
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?ProductName=abc");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order12>(modelBindingResult.Model);
            Assert.Equal("abc", model.ProductName);

            Assert.Single(modelState);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order13)
            };

            // No Data
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order13>(modelBindingResult.Model);
            Assert.Null(model.OrderIds);

            Assert.Single(modelState);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
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
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order13>(modelBindingResult.Model);
            Assert.Null(model.OrderIds);

            Assert.Single(modelState);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order13),
            };

            // No Data
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?OrderIds[0]=123");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order13>(modelBindingResult.Model);
            Assert.Equal(new[] { 123 }, model.OrderIds.ToArray());

            Assert.Single(modelState);
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
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order14)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.ProductId=");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order14>(modelBindingResult.Model);
            Assert.NotNull(model);
            Assert.Equal(0, model.ProductId);

            Assert.Single(modelState);
            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "parameter.ProductId").Value;
            Assert.Equal(string.Empty, entry.AttemptedValue);
            Assert.Equal(string.Empty, entry.RawValue);

            var error = Assert.Single(entry.Errors);
            Assert.Equal("The value '' is invalid.", error.ErrorMessage);
            Assert.Null(error.Exception);
        }

        // This covers the case where a key is present, but has no value. The model binder will
        // report and error because it's a value type (non-nullable).
        [Fact]
        [ReplaceCulture]
        public async Task MutableObjectModelBinder_BindsPOCO_TypeConvertedPropertyWithEmptyValue_Error()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Order14)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter.ProductId");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Order14>(modelBindingResult.Model);
            Assert.NotNull(model);
            Assert.Equal(0, model.ProductId);

            var entry = Assert.Single(modelState);
            Assert.Equal("parameter.ProductId", entry.Key);
            Assert.Equal(string.Empty, entry.Value.AttemptedValue);

            var error = Assert.Single(entry.Value.Errors);
            Assert.Equal("The value '' is invalid.", error.ErrorMessage, StringComparer.Ordinal);
            Assert.Null(error.Exception);

            Assert.Equal(1, modelState.ErrorCount);
            Assert.False(modelState.IsValid);
        }

        private class Person12
        {
            public Address12 Address { get; set; }
        }

        [ModelBinder(Name = "HomeAddress")]
        private class Address12
        {
            public string Street { get; set; }
        }

        // Make sure the metadata is honored when a [ModelBinder] attribute is associated with a class somewhere in the
        // type hierarchy of an action parameter. This should behave identically to such an attribute on a property in
        // the type hierarchy.
        [Theory]
        [MemberData(
            nameof(BinderTypeBasedModelBinderIntegrationTest.NullAndEmptyBindingInfo),
            MemberType = typeof(BinderTypeBasedModelBinderIntegrationTest))]
        public async Task ModelNameOnPropertyType_WithData_Succeeds(BindingInfo bindingInfo)
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "parameter-name",
                BindingInfo = bindingInfo,
                ParameterType = typeof(Person12),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request => request.QueryString = new QueryString("?HomeAddress.Street=someStreet"));
            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var person = Assert.IsType<Person12>(modelBindingResult.Model);
            Assert.NotNull(person.Address);
            Assert.Equal("someStreet", person.Address.Street, StringComparer.Ordinal);

            Assert.True(modelState.IsValid);
            var kvp = Assert.Single(modelState);
            Assert.Equal("HomeAddress.Street", kvp.Key);
            var entry = kvp.Value;
            Assert.NotNull(entry);
            Assert.Empty(entry.Errors);
            Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        }

        // Make sure the metadata is honored when a [ModelBinder] attribute is associated with an action parameter's
        // type. This should behave identically to such an attribute on an action parameter.
        [Theory]
        [MemberData(
            nameof(BinderTypeBasedModelBinderIntegrationTest.NullAndEmptyBindingInfo),
            MemberType = typeof(BinderTypeBasedModelBinderIntegrationTest))]
        public async Task ModelNameOnParameterType_WithData_Succeeds(BindingInfo bindingInfo)
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "parameter-name",
                BindingInfo = bindingInfo,
                ParameterType = typeof(Address12),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request => request.QueryString = new QueryString("?HomeAddress.Street=someStreet"));
            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var address = Assert.IsType<Address12>(modelBindingResult.Model);
            Assert.Equal("someStreet", address.Street, StringComparer.Ordinal);

            Assert.True(modelState.IsValid);
            var kvp = Assert.Single(modelState);
            Assert.Equal("HomeAddress.Street", kvp.Key);
            var entry = kvp.Value;
            Assert.NotNull(entry);
            Assert.Empty(entry.Errors);
            Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        }

        private class Person13
        {
            public Address13 Address { get; set; }
        }

        [Bind("Street")]
        private class Address13
        {
            public int Number { get; set; }

            public string Street { get; set; }

            public string City { get; set; }

            public string State { get; set; }
        }

        // Make sure the metadata is honored when a [Bind] attribute is associated with a class somewhere in the type
        // hierarchy of an action parameter. This should behave identically to such an attribute on a property in the
        // type hierarchy. (Test is similar to ModelNameOnPropertyType_WithData_Succeeds() but covers implementing
        // IPropertyFilterProvider, not IModelNameProvider.)
        [Theory]
        [MemberData(
            nameof(BinderTypeBasedModelBinderIntegrationTest.NullAndEmptyBindingInfo),
            MemberType = typeof(BinderTypeBasedModelBinderIntegrationTest))]
        public async Task BindAttributeOnPropertyType_WithData_Succeeds(BindingInfo bindingInfo)
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "parameter-name",
                BindingInfo = bindingInfo,
                ParameterType = typeof(Person13),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request => request.QueryString = new QueryString(
                    "?Address.Number=23&Address.Street=someStreet&Address.City=Redmond&Address.State=WA"));
            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var person = Assert.IsType<Person13>(modelBindingResult.Model);
            Assert.NotNull(person.Address);
            Assert.Null(person.Address.City);
            Assert.Equal(0, person.Address.Number);
            Assert.Null(person.Address.State);
            Assert.Equal("someStreet", person.Address.Street, StringComparer.Ordinal);

            Assert.True(modelState.IsValid);
            var kvp = Assert.Single(modelState);
            Assert.Equal("Address.Street", kvp.Key);
            var entry = kvp.Value;
            Assert.NotNull(entry);
            Assert.Empty(entry.Errors);
            Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        }

        // Make sure the metadata is honored when a [Bind] attribute is associated with an action parameter's type.
        // This should behave identically to such an attribute on an action parameter. (Test is similar
        // to ModelNameOnParameterType_WithData_Succeeds() but covers implementing IPropertyFilterProvider, not
        // IModelNameProvider.)
        [Theory]
        [MemberData(
            nameof(BinderTypeBasedModelBinderIntegrationTest.NullAndEmptyBindingInfo),
            MemberType = typeof(BinderTypeBasedModelBinderIntegrationTest))]
        public async Task BindAttributeOnParameterType_WithData_Succeeds(BindingInfo bindingInfo)
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "parameter-name",
                BindingInfo = bindingInfo,
                ParameterType = typeof(Address13),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request => request.QueryString = new QueryString("?Number=23&Street=someStreet&City=Redmond&State=WA"));
            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var address = Assert.IsType<Address13>(modelBindingResult.Model);
            Assert.Null(address.City);
            Assert.Equal(0, address.Number);
            Assert.Null(address.State);
            Assert.Equal("someStreet", address.Street, StringComparer.Ordinal);

            Assert.True(modelState.IsValid);
            var kvp = Assert.Single(modelState);
            Assert.Equal("Street", kvp.Key);
            var entry = kvp.Value;
            Assert.NotNull(entry);
            Assert.Empty(entry.Errors);
            Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        }

        private class Product
        {
            public int ProductId { get; set; }

            public string Name { get; }

            public IList<string> Aliases { get; }
        }

        [Theory]
        [InlineData("?parameter.ProductId=10")]
        [InlineData("?parameter.ProductId=10&parameter.Name=Camera")]
        [InlineData("?parameter.ProductId=10&parameter.Name=Camera&parameter.Aliases[0]=Camera1")]
        public async Task ComplexTypeModelBinder_BindsSettableProperties(string queryString)
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Product)
            };

            // Need to have a key here so that the ComplexTypeModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString(queryString);
                SetJsonBodyContent(request, AddressBodyContent);
            });
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Product>(modelBindingResult.Model);
            Assert.NotNull(model);
            Assert.Equal(10, model.ProductId);
            Assert.Null(model.Name);
            Assert.Null(model.Aliases);
        }

        private class Photo
        {
            public string Id { get; set; }

            public KeyValuePair<string, LocationInfo> Info { get; set; }
        }

        private class LocationInfo
        {
            [FromHeader]
            public string GpsCoordinates { get; set; }

            public int Zipcode { get; set; }
        }

        [Fact]
        public async Task MutableObjectModelBinder_BindsKeyValuePairProperty_HavingFromHeaderProperty_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Photo)
            };

            // Need to have a key here so that the MutableObjectModelBinder will recurse to bind elements.
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.Headers.Add("GpsCoordinates", "10,20");
                request.QueryString = new QueryString("?Id=1&Info.Key=location1&Info.Value.Zipcode=98052");
            });

            var modelState = testContext.ModelState;
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(testContext, valueProvider, parameter);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var model = Assert.IsType<Photo>(modelBindingResult.Model);
            Assert.Equal("1", model.Id);
            Assert.Equal("location1", model.Info.Key);
            Assert.NotNull(model.Info.Value);
            Assert.Equal("10,20", model.Info.Value.GpsCoordinates);
            Assert.Equal(98052, model.Info.Value.Zipcode);

            // ModelState
            Assert.Equal(4, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, e => e.Key == "Id").Value;
            Assert.Equal("1", entry.AttemptedValue);
            Assert.Equal("1", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "Info.Key").Value;
            Assert.Equal("location1", entry.AttemptedValue);
            Assert.Equal("location1", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "Info.Value.Zipcode").Value;
            Assert.Equal("98052", entry.AttemptedValue);
            Assert.Equal("98052", entry.RawValue);

            entry = Assert.Single(modelState, e => e.Key == "Info.Value.GpsCoordinates").Value;
            Assert.Equal("10,20", entry.AttemptedValue);
            Assert.Equal(new[] { "10", "20" }, entry.RawValue);
        }

        private static void SetJsonBodyContent(HttpRequest request, string content)
        {
            var stream = new MemoryStream(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content));
            request.Body = stream;
            request.ContentType = "application/json";
        }

        private static void SetFormFileBodyContent(HttpRequest request, string content, string name)
        {
            const string fileName = "text.txt";
            var fileCollection = new FormFileCollection();
            var formCollection = new FormCollection(new Dictionary<string, StringValues>(), fileCollection);
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            request.Form = formCollection;
            request.ContentType = "multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq";
            request.Headers["Content-Disposition"] = $"form-data; name={name}; filename={fileName}";

            fileCollection.Add(new FormFile(memoryStream, 0, memoryStream.Length, name, fileName)
            {
                Headers = request.Headers
            });
        }
    }
}