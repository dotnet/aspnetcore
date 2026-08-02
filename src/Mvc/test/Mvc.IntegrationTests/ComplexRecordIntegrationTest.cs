// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

// A clone of ComplexTypeIntegrationTestBase performed using record types
public class ComplexRecordIntegrationTest
{
    private const string AddressBodyContent = "{ \"street\" : \"" + AddressStreetContent + "\" }";
    private const string AddressStreetContent = "1 Microsoft Way";

    private static readonly byte[] ByteArrayContent = Encoding.BigEndianUnicode.GetBytes("abcd");
    private static readonly string ByteArrayEncoded = Convert.ToBase64String(ByteArrayContent);

    private record Order1(int ProductId, Person1 Customer);

    private record Person1(string Name, [FromBody] Address1 Address);

    private record Address1(string Street);

    [Fact]
    public async Task BindsNestedPOCO_WithBodyModelBinder_WithPrefix_Success()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order1)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter.Customer.Name=bill");
            SetJsonBodyContent(request, AddressBodyContent);
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    public async Task BindsNestedPOCO_WithBodyModelBinder_WithEmptyPrefix_Success()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order1)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Customer.Name=bill");
            SetJsonBodyContent(request, AddressBodyContent);
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    public async Task BindsNestedPOCO_WithBodyModelBinder_WithPrefix_NoBodyData()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order1)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter.Customer.Name=bill");
            request.ContentType = "application/json";
        });

        testContext.MvcOptions.AllowEmptyInputInBodyModelBinding = true;

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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

    [Fact]
    public async Task BindsNestedPOCO_WithBodyModelBinder_WithPrefix_NoBodyData_ValueInQuery()
    {
        // With record types, constructor parameters also appear as settable properties.
        // In this case, we will only attempt to bind the parameter and not the property.

        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order1)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter.Customer.Name=bill&paramater.Customer.Address=not-used");
            request.ContentType = "application/json";
        });

        testContext.MvcOptions.AllowEmptyInputInBodyModelBinding = true;

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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

    [Fact]
    public async Task BindsNestedPOCO_WithBodyModelBinder_WithPrefix_PartialData()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order1)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter.ProductId=10");
            SetJsonBodyContent(request, AddressBodyContent);
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Order1>(modelBindingResult.Model);
        Assert.NotNull(model.Customer);
        Assert.Equal("1 Microsoft Way", model.Customer.Address.Street);

        Assert.Equal(10, model.ProductId);

        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState).Value;
        Assert.Equal("10", entry.AttemptedValue);
        Assert.Equal("10", entry.RawValue);
    }

    [Fact]
    public async Task BindsNestedPOCO_WithBodyModelBinder_WithPrefix_NoData()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order1)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?");
            SetJsonBodyContent(request, AddressBodyContent);
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Order1>(modelBindingResult.Model);
        Assert.NotNull(model.Customer);
        Assert.Equal("1 Microsoft Way", model.Customer.Address.Street);

        Assert.Empty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    private record Order3(int ProductId, Person3 Customer);

    private record Person3(string Name, byte[] Token);

    [Fact]
    public async Task BindsNestedPOCO_WithByteArrayModelBinder_WithPrefix_Success()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order3)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString =
                new QueryString("?parameter.Customer.Name=bill&parameter.Customer.Token=" + ByteArrayEncoded);
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    public async Task BindsNestedPOCO_WithByteArrayModelBinder_WithEmptyPrefix_Success()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order3)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Customer.Name=bill&Customer.Token=" + ByteArrayEncoded);
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    public async Task BindsNestedPOCO_WithByteArrayModelBinder_WithPrefix_NoData()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order3)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter.Customer.Name=bill");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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

    private record Order4(int ProductId, Person4 Customer);

    private record Person4(string Name, IEnumerable<IFormFile> Documents);

    [Fact]
    public async Task BindsNestedPOCO_WithFormFileModelBinder_WithPrefix_Success()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order4)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter.Customer.Name=bill");
            SetFormFileBodyContent(request, "Hello, World!", "parameter.Customer.Documents");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    public async Task BindsNestedPOCO_WithFormFileModelBinder_WithEmptyPrefix_Success()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order4),
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Customer.Name=bill");
            SetFormFileBodyContent(request, "Hello, World!", "Customer.Documents");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    public async Task BindsNestedPOCO_WithFormFileModelBinder_WithPrefix_NoBodyData()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order4)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter.Customer.Name=bill");

            // Deliberately leaving out any form data.
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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

    [Fact]
    public async Task BindsNestedPOCO_WithFormFileModelBinder_WithPrefix_PartialData()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order4)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter.ProductId=10");
            SetFormFileBodyContent(request, "Hello, World!", "parameter.Customer.Documents");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Order4>(modelBindingResult.Model);
        Assert.NotNull(model.Customer);

        var document = Assert.Single(model.Customer.Documents);
        Assert.Equal("text.txt", document.FileName);
        using (var reader = new StreamReader(document.OpenReadStream()))
        {
            Assert.Equal("Hello, World!", await reader.ReadToEndAsync());
        }

        Assert.Equal(10, model.ProductId);

        Assert.Equal(2, modelState.Count);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);

        Assert.Single(modelState, e => e.Key == "parameter.Customer.Documents");
        var entry = Assert.Single(modelState, e => e.Key == "parameter.ProductId").Value;
        Assert.Equal("10", entry.AttemptedValue);
        Assert.Equal("10", entry.RawValue);
    }

    [Fact]
    public async Task BindsNestedPOCO_WithFormFileModelBinder_WithPrefix_NoData()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order4)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?");
            SetFormFileBodyContent(request, "Hello, World!", "Customer.Documents");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Order4>(modelBindingResult.Model);
        Assert.NotNull(model.Customer);

        var document = Assert.Single(model.Customer.Documents);
        Assert.Equal("text.txt", document.FileName);
        using (var reader = new StreamReader(document.OpenReadStream()))
        {
            Assert.Equal("Hello, World!", await reader.ReadToEndAsync());
        }

        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState);
        Assert.Equal("Customer.Documents", entry.Key);
    }

    private record Order5(string Name, int[] ProductIds);

    [Fact]
    public async Task BindsArrayProperty_WithPrefix_Success()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order5)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString =
                new QueryString("?parameter.Name=bill&parameter.ProductIds[0]=10&parameter.ProductIds[1]=11");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    public async Task BindsArrayProperty_EmptyPrefix_Success()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order5)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Name=bill&ProductIds[0]=10&ProductIds[1]=11");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    public async Task BindsArrayProperty_NoCollectionData()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order5)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter.Name=bill");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    public async Task BindsArrayProperty_NoData()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order5)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Order5>(modelBindingResult.Model);
        Assert.Null(model.Name);
        Assert.Null(model.ProductIds);

        Assert.Empty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    private record Order6(string Name, List<int> ProductIds);

    [Fact]
    public async Task BindsListProperty_WithPrefix_Success()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order6)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString =
                new QueryString("?parameter.Name=bill&parameter.ProductIds[0]=10&parameter.ProductIds[1]=11");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    public async Task BindsListProperty_EmptyPrefix_Success()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order6)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Name=bill&ProductIds[0]=10&ProductIds[1]=11");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    public async Task BindsListProperty_NoCollectionData()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order6)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter.Name=bill");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    public async Task BindsListProperty_NoData()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order6)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Order6>(modelBindingResult.Model);
        Assert.Null(model.Name);
        Assert.Null(model.ProductIds);

        Assert.Empty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    private record Order7(string Name, Dictionary<string, int> ProductIds);

    [Fact]
    public async Task BindsDictionaryProperty_WithPrefix_Success()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order7)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString =
                new QueryString("?parameter.Name=bill&parameter.ProductIds[0].Key=key0&parameter.ProductIds[0].Value=10");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    public async Task BindsDictionaryProperty_EmptyPrefix_Success()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order7)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Name=bill&ProductIds[0].Key=key0&ProductIds[0].Value=10");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    public async Task BindsDictionaryProperty_NoCollectionData()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order7)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter.Name=bill");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    public async Task BindsDictionaryProperty_NoData()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order7)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    private record Car1(string Name, Dictionary<string, IEnumerable<SpecDoc>> Specs);

    // Dictionary property with an Array value type
    private record Car2(string Name, Dictionary<string, SpecDoc[]> Specs);

    private record Car3(string Name, IEnumerable<KeyValuePair<string, IEnumerable<SpecDoc>>> Specs);

    private record SpecDoc(string Name);

    [Fact]
    public async Task BindsDictionaryProperty_WithIEnumerableComplexTypeValue_Success()
    {
        // Arrange
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
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    public async Task BindsDictionaryProperty_WithArrayOfComplexTypeValue_Success()
    {
        // Arrange
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
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    public async Task BindsDictionaryProperty_WithIEnumerableOfKeyValuePair_Success()
    {
        // Arrange
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
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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

    private record Order8(KeyValuePair<string, int> ProductId, string Name = default!);

    [Fact]
    public async Task BindsKeyValuePairProperty_WithPrefix_Success()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order8)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString =
                new QueryString("?parameter.Name=bill&parameter.ProductId.Key=key0&parameter.ProductId.Value=10");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    public async Task BindsKeyValuePairProperty_EmptyPrefix_Success()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order8)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Name=bill&ProductId.Key=key0&ProductId.Value=10");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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

    private record Car4(string Name, KeyValuePair<string, Dictionary<string, string>> Specs);

    [Fact]
    public async Task Foo_BindsKeyValuePairProperty_WithPrefix_Success()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "p",
            ParameterType = typeof(Car4)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
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
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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

    private record Order9(Person9 Customer);

    private record Person9([FromBody] Address1 Address);

    // If a nested POCO object has all properties bound from a greedy source, then it should be populated
    // if the top-level object is created.
    [Fact]
    public async Task BindsNestedPOCO_WithAllGreedyBoundProperties()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order9)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?");
            SetJsonBodyContent(request, AddressBodyContent);
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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

    private record Order10([BindRequired] Person10 Customer);

    private record Person10(string Name);

    [Fact]
    public async Task WithRequiredComplexProperty_NoData_GetsErrors()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order10)
        };

        // No Data
        var testContext = ModelBindingTestHelper.GetTestContext();

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
        Assert.Equal("A value for the 'Customer' parameter or property was not provided.", error.ErrorMessage);
    }

    [Fact]
    public async Task WithBindRequired_NoData_AndCustomizedMessage_AddsGivenMessage()
    {
        // Arrange
        var parameterInfo = typeof(Order10).GetConstructor(new[] { typeof(Person10) }).GetParameters()[0];
        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider
            .ForParameter(parameterInfo)
            .BindingDetails((Action<ModelBinding.Metadata.BindingMetadata>)(binding =>
            {
                // A real details provider could customize message based on BindingMetadataProviderContext.
                binding.ModelBindingMessageProvider.SetMissingBindRequiredValueAccessor(
                name => $"Hurts when '{ name }' is not provided.");
            }));

        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order10)
        };

        // No Data
        var testContext = ModelBindingTestHelper.GetTestContext(metadataProvider: metadataProvider);

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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

    private record Order11(Person11 Customer);

    private record Person11(int Id, [BindRequired] string Name);

    [Fact]
    public async Task WithNestedRequiredProperty_WithPartialData_GetsErrors()
    {
        // Arrange
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
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
        Assert.Equal("A value for the 'Name' parameter or property was not provided.", error.ErrorMessage);
    }

    [Fact]
    public async Task WithNestedRequiredProperty_WithData_EmptyPrefix_GetsErrors()
    {
        // Arrange
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
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
        Assert.Equal("A value for the 'Name' parameter or property was not provided.", error.ErrorMessage);
    }

    [Fact]
    public async Task WithNestedRequiredProperty_WithData_CustomPrefix_GetsErrors()
    {
        // Arrange
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
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
        Assert.Equal("A value for the 'Name' parameter or property was not provided.", error.ErrorMessage);
    }

    private record Order12([BindRequired] string ProductName);

    [Fact]
    public async Task WithRequiredProperty_NoData_GetsErrors()
    {
        // Arrange
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
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
        Assert.Equal("A value for the 'ProductName' parameter or property was not provided.", error.ErrorMessage);
    }

    [Fact]
    public async Task WithRequiredProperty_NoData_CustomPrefix_GetsErrors()
    {
        // Arrange
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
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
        Assert.Equal("A value for the 'ProductName' parameter or property was not provided.", error.ErrorMessage);
    }

    [Fact]
    public async Task WithRequiredProperty_WithData_EmptyPrefix_GetsBound()
    {
        // Arrange
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
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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

    private record Order13([BindRequired] List<int> OrderIds);

    [Fact]
    public async Task WithRequiredCollectionProperty_NoData_GetsErrors()
    {
        // Arrange
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
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
        Assert.Equal("A value for the 'OrderIds' parameter or property was not provided.", error.ErrorMessage);
    }

    [Fact]
    public async Task WithRequiredCollectionProperty_NoData_CustomPrefix_GetsErrors()
    {
        // Arrange
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
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
        Assert.Equal("A value for the 'OrderIds' parameter or property was not provided.", error.ErrorMessage);
    }

    [Fact]
    public async Task WithRequiredCollectionProperty_WithData_EmptyPrefix_GetsBound()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order13),
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?OrderIds[0]=123");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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

    private record Order14(int ProductId);

    // This covers the case where a key is present, but has an empty value. The type converter
    // will report an error.
    [Fact]
    public async Task BindsPOCO_TypeConvertedPropertyNonConvertibleValue_GetsError()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order14)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter.ProductId=");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
    public async Task BindsPOCO_TypeConvertedPropertyWithEmptyValue_Error()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Order14)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter.ProductId");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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

    private record Person12(Address12 Address);

    [ModelBinder(Name = "HomeAddress")]
    private record Address12(string Street);

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
        var parameter = new ParameterDescriptor
        {
            Name = "parameter-name",
            BindingInfo = bindingInfo,
            ParameterType = typeof(Person12),
        };

        var testContext = ModelBindingTestHelper.GetTestContext(
            request => request.QueryString = new QueryString("?HomeAddress.Street=someStreet"));

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
        var parameter = new ParameterDescriptor
        {
            Name = "parameter-name",
            BindingInfo = bindingInfo,
            ParameterType = typeof(Address12),
        };

        var testContext = ModelBindingTestHelper.GetTestContext(
            request => request.QueryString = new QueryString("?HomeAddress.Street=someStreet"));

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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

    private record Person13(Address13 Address);

    [Bind("Street")]
    private record Address13(int Number, string Street, string City, string State);

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
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
        var parameter = new ParameterDescriptor
        {
            Name = "parameter-name",
            BindingInfo = bindingInfo,
            ParameterType = typeof(Address13),
        };

        var testContext = ModelBindingTestHelper.GetTestContext(
            request => request.QueryString = new QueryString("?Number=23&Street=someStreet&City=Redmond&State=WA"));

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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

    private record Product(int ProductId)
    {
        public string Name { get; }

        public IList<string> Aliases { get; }
    }

    [Theory]
    [InlineData("?parameter.ProductId=10")]
    [InlineData("?parameter.ProductId=10&parameter.Name=Camera")]
    [InlineData("?parameter.ProductId=10&parameter.Name=Camera&parameter.Aliases[0]=Camera1")]
    public async Task BindsSettableProperties(string queryString)
    {
        // Arrange
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

        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Product>(modelBindingResult.Model);
        Assert.NotNull(model);
        Assert.Equal(10, model.ProductId);
        Assert.Null(model.Name);
        Assert.Null(model.Aliases);
    }

    private record Photo(string Id, KeyValuePair<string, LocationInfo> Info);

    private record LocationInfo([FromHeader] string GpsCoordinates, int Zipcode);

    [Fact]
    public async Task BindsKeyValuePairProperty_HavingFromHeaderProperty_Success()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Photo)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.Headers.Add("GpsCoordinates", "10,20");
            request.QueryString = new QueryString("?Id=1&Info.Key=location1&Info.Value.Zipcode=98052");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

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
        Assert.Equal("10,20", entry.RawValue);
    }

    private record Person5(string Name, IFormFile Photo);

    // Regression test for #4802.
    [Fact]
    public async Task ReportsFailureToCollectionModelBinder()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(IList<Person5>),
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            SetFormFileBodyContent(request, "Hello world!", "[0].Photo");

            // CollectionModelBinder binds an empty collection when value providers are all empty.
            request.QueryString = new QueryString("?a=b");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<List<Person5>>(modelBindingResult.Model);
        var person = Assert.Single(model);
        Assert.Null(person.Name);
        Assert.NotNull(person.Photo);
        using (var reader = new StreamReader(person.Photo.OpenReadStream()))
        {
            Assert.Equal("Hello world!", await reader.ReadToEndAsync());
        }

        Assert.True(modelState.IsValid);
        var state = Assert.Single(modelState);
        Assert.Equal("[0].Photo", state.Key);
        Assert.Null(state.Value.AttemptedValue);
        Assert.Empty(state.Value.Errors);
        Assert.Null(state.Value.RawValue);
    }

    private record TestModel(TestInnerModel[] InnerModels);

    private record TestInnerModel([ModelBinder(BinderType = typeof(NumberModelBinder))] decimal Rate);

    private class NumberModelBinder : IModelBinder
    {
        private readonly NumberStyles _supportedStyles = NumberStyles.Float | NumberStyles.AllowThousands;
        private readonly DecimalModelBinder _innerBinder;

        public NumberModelBinder(ILoggerFactory loggerFactory)
        {
            _innerBinder = new DecimalModelBinder(_supportedStyles, loggerFactory);
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            return _innerBinder.BindModelAsync(bindingContext);
        }
    }

    // Regression test for #4939.
    [Fact]
    public async Task ReportsFailureToCollectionModelBinder_CustomBinder()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(TestModel),
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString(
                "?parameter.InnerModels[0].Rate=1,000.00&parameter.InnerModels[1].Rate=2000");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<TestModel>(modelBindingResult.Model);
        Assert.NotNull(model.InnerModels);
        Assert.Collection(
            model.InnerModels,
            item => Assert.Equal(1000, item.Rate),
            item => Assert.Equal(2000, item.Rate));

        Assert.True(modelState.IsValid);
        Assert.Collection(
            modelState,
            kvp =>
            {
                Assert.Equal("parameter.InnerModels[0].Rate", kvp.Key);
                Assert.Equal("1,000.00", kvp.Value.AttemptedValue);
                Assert.Empty(kvp.Value.Errors);
                Assert.Equal("1,000.00", kvp.Value.RawValue);
                Assert.Equal(ModelValidationState.Valid, kvp.Value.ValidationState);
            },
            kvp =>
            {
                Assert.Equal("parameter.InnerModels[1].Rate", kvp.Key);
                Assert.Equal("2000", kvp.Value.AttemptedValue);
                Assert.Empty(kvp.Value.Errors);
                Assert.Equal("2000", kvp.Value.RawValue);
                Assert.Equal(ModelValidationState.Valid, kvp.Value.ValidationState);
            });
    }

    private record Person6(string Name, Person6 Mother, IFormFile Photo);

    // Regression test for #6616.
    [Fact]
    public async Task ReportsFailureToNearTopLevel()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Person6),
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            SetFormFileBodyContent(request, "Hello world!", "Photo");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Person6>(modelBindingResult.Model);
        Assert.Null(model.Mother);
        Assert.Null(model.Name);
        Assert.NotNull(model.Photo);
        using (var reader = new StreamReader(model.Photo.OpenReadStream()))
        {
            Assert.Equal("Hello world!", await reader.ReadToEndAsync());
        }

        Assert.True(modelState.IsValid);
        var state = Assert.Single(modelState);
        Assert.Equal("Photo", state.Key);
        Assert.Null(state.Value.AttemptedValue);
        Assert.Empty(state.Value.Errors);
        Assert.Null(state.Value.RawValue);
    }

    // Regression test for #6616.
    [Fact]
    public async Task ReportsFailureToComplexTypeModelBinder()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Person6),
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            SetFormFileBodyContent(request, "Hello world!", "Photo");
            SetFormFileBodyContent(request, "Hello Mom!", "Mother.Photo");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Person6>(modelBindingResult.Model);
        Assert.NotNull(model.Mother);
        Assert.Null(model.Mother.Mother);
        Assert.NotNull(model.Mother.Photo);
        using (var reader = new StreamReader(model.Mother.Photo.OpenReadStream()))
        {
            Assert.Equal("Hello Mom!", await reader.ReadToEndAsync());
        }

        Assert.Null(model.Name);
        Assert.NotNull(model.Photo);
        using (var reader = new StreamReader(model.Photo.OpenReadStream()))
        {
            Assert.Equal("Hello world!", await reader.ReadToEndAsync());
        }

        Assert.True(modelState.IsValid);
        Assert.Collection(
            modelState,
            kvp =>
            {
                Assert.Equal("Photo", kvp.Key);
                Assert.Null(kvp.Value.AttemptedValue);
                Assert.Empty(kvp.Value.Errors);
                Assert.Null(kvp.Value.RawValue);
            },
            kvp =>
            {
                Assert.Equal("Mother.Photo", kvp.Key);
                Assert.Null(kvp.Value.AttemptedValue);
                Assert.Empty(kvp.Value.Errors);
                Assert.Null(kvp.Value.RawValue);
            });
    }

    private record Person7(string Name, IList<Person7> Children, IFormFile Photo);

    // Regression test for #6616.
    [Fact]
    public async Task ReportsFailureToViaCollection()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Person7),
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            SetFormFileBodyContent(request, "Hello world!", "Photo");
            SetFormFileBodyContent(request, "Hello Fred!", "Children[0].Photo");
            SetFormFileBodyContent(request, "Hello Ginger!", "Children[1].Photo");

            request.QueryString = new QueryString("?Children[0].Name=Fred&Children[1].Name=Ginger");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Person7>(modelBindingResult.Model);
        Assert.NotNull(model.Children);
        Assert.Collection(
            model.Children,
            item =>
            {
                Assert.Null(item.Children);
                Assert.Equal("Fred", item.Name);
                using (var reader = new StreamReader(item.Photo.OpenReadStream()))
                {
                    Assert.Equal("Hello Fred!", reader.ReadToEnd());
                }
            },
            item =>
            {
                Assert.Null(item.Children);
                Assert.Equal("Ginger", item.Name);
                using (var reader = new StreamReader(item.Photo.OpenReadStream()))
                {
                    Assert.Equal("Hello Ginger!", reader.ReadToEnd());
                }
            });

        Assert.Null(model.Name);
        Assert.NotNull(model.Photo);
        using (var reader = new StreamReader(model.Photo.OpenReadStream()))
        {
            Assert.Equal("Hello world!", await reader.ReadToEndAsync());
        }

        Assert.True(modelState.IsValid);
    }

    private record LoopyModel([ModelBinder(typeof(SuccessfulModelBinder))] bool IsBound, LoopyModel SelfReference);

    // Regression test for #7052
    [Fact]
    public async Task ModelBindingSystem_ThrowsOn33Binders()
    {
        // Arrange
        var expectedMessage = "Model binding system exceeded " +
            $"{nameof(MvcOptions)}.{nameof(MvcOptions.MaxModelBindingRecursionDepth)} (32). Reduce the " +
            $"potential nesting of '{typeof(LoopyModel)}'. For example, this type may have a property with a " +
            "model binder that always succeeds. See the " +
            $"{nameof(MvcOptions)}.{nameof(MvcOptions.MaxModelBindingRecursionDepth)} documentation for more " +
            "information.";
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(LoopyModel),
        };

        var testContext = ModelBindingTestHelper.GetTestContext();
        var modelState = testContext.ModelState;
        var metadata = testContext.MetadataProvider.GetMetadataForType(parameter.ParameterType);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => parameterBinder.BindModelAsync(parameter, testContext));
        Assert.Equal(expectedMessage, exception.Message);
    }

    private record TwoDeepModel([ModelBinder(typeof(SuccessfulModelBinder))] bool IsBound);

    private record ThreeDeepModel([ModelBinder(typeof(SuccessfulModelBinder))] bool IsBound, TwoDeepModel Inner);

    // Ensure model binding system allows MaxModelBindingRecursionDepth binders on the stack.
    [Fact]
    public async Task ModelBindingSystem_BindsWith3Binders()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(ThreeDeepModel),
        };

        var testContext = ModelBindingTestHelper.GetTestContext(
            updateOptions: options => options.MaxModelBindingRecursionDepth = 3);

        var modelState = testContext.ModelState;
        var metadata = testContext.MetadataProvider.GetMetadataForType(parameter.ParameterType);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var result = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelState.IsValid);
        Assert.Equal(0, modelState.ErrorCount);

        Assert.True(result.IsModelSet);
        var model = Assert.IsType<ThreeDeepModel>(result.Model);
        Assert.True(model.IsBound);
        Assert.NotNull(model.Inner);
        Assert.True(model.Inner.IsBound);
    }

    private record FourDeepModel([ModelBinder(typeof(SuccessfulModelBinder))] bool IsBound, ThreeDeepModel Inner);

    // Ensure model binding system disallows one more than MaxModelBindingRecursionDepth binders on the stack.
    [Fact]
    public async Task ModelBindingSystem_ThrowsOn4Binders()
    {
        // Arrange
        var expectedMessage = $"Model binding system exceeded " +
            $"{nameof(MvcOptions)}.{nameof(MvcOptions.MaxModelBindingRecursionDepth)} (3). Reduce the " +
            $"potential nesting of '{typeof(FourDeepModel)}'. For example, this type may have a property with a " +
            $"model binder that always succeeds. See the " +
            $"{nameof(MvcOptions)}.{nameof(MvcOptions.MaxModelBindingRecursionDepth)} documentation for more " +
            $"information.";
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(FourDeepModel),
        };

        var testContext = ModelBindingTestHelper.GetTestContext(
            updateOptions: options => options.MaxModelBindingRecursionDepth = 3);

        var modelState = testContext.ModelState;
        var metadata = testContext.MetadataProvider.GetMetadataForType(parameter.ParameterType);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => parameterBinder.BindModelAsync(parameter, testContext));
        Assert.Equal(expectedMessage, exception.Message);
    }

    private record LoopyModel1([ModelBinder(typeof(SuccessfulModelBinder))] bool IsBound, LoopyModel2 Inner);

    private record LoopyModel2([ModelBinder(typeof(SuccessfulModelBinder))] bool IsBound, LoopyModel3 Inner);

    private record LoopyModel3([ModelBinder(typeof(SuccessfulModelBinder))] bool IsBound, LoopyModel1 Inner);

    [Fact]
    public async Task ModelBindingSystem_ThrowsOn33Binders_WithIndirectModelTypeLoop()
    {
        // Arrange
        var expectedMessage = $"Model binding system exceeded " +
            $"{nameof(MvcOptions)}.{nameof(MvcOptions.MaxModelBindingRecursionDepth)} (32). Reduce the " +
            $"potential nesting of '{typeof(LoopyModel1)}'. For example, this type may have a property with a " +
            $"model binder that always succeeds. See the " +
            $"{nameof(MvcOptions)}.{nameof(MvcOptions.MaxModelBindingRecursionDepth)} documentation for more " +
            $"information.";
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(LoopyModel1),
        };

        var testContext = ModelBindingTestHelper.GetTestContext();
        var modelState = testContext.ModelState;
        var metadata = testContext.MetadataProvider.GetMetadataForType(parameter.ParameterType);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => parameterBinder.BindModelAsync(parameter, testContext));
        Assert.Equal(expectedMessage, exception.Message);
    }

    private record RecordTypeWithSettableProperty1(string Name)
    {
        public int Age { get; set; }
    }

    [Fact]
    public async Task RecordTypeWithBoundParametersAndProperties_NoData()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(RecordTypeWithSettableProperty1)
        };

        // Need to have a key here so that the ComplexObjectModelBinder will recurse to bind elements.
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<RecordTypeWithSettableProperty1>(modelBindingResult.Model);
        Assert.Null(model.Name);
        Assert.Equal(0, model.Age);

        Assert.Empty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    [Fact]
    public async Task RecordTypeWithBoundParametersAndProperties_ValueForParameter()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(RecordTypeWithSettableProperty1)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?name=TestName");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<RecordTypeWithSettableProperty1>(modelBindingResult.Model);
        Assert.Equal("TestName", model.Name);
        Assert.Equal(0, model.Age);

        var entry = Assert.Single(modelState);
        Assert.Equal("Name", entry.Key);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    [Fact]
    public async Task RecordTypeWithBoundParametersAndProperties_ValueForProperty()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(RecordTypeWithSettableProperty1)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?age=28");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<RecordTypeWithSettableProperty1>(modelBindingResult.Model);
        Assert.Null(model.Name);
        Assert.Equal(28, model.Age);

        var entry = Assert.Single(modelState);
        Assert.Equal("Age", entry.Key);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    [Fact]
    public async Task RecordTypeWithBoundParametersAndProperties_ValueForParameterAndProperty()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(RecordTypeWithSettableProperty1)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Name=test&age=28");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<RecordTypeWithSettableProperty1>(modelBindingResult.Model);
        Assert.Equal("test", model.Name);
        Assert.Equal(28, model.Age);

        Assert.Equal(2, modelState.Count);
        var entry = Assert.Single(modelState, m => m.Key == "Age");
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);

        entry = Assert.Single(modelState, m => m.Key == "Name");
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    public record RecordTypeWithFilteredProperty1([BindNever] string Id, string Name);

    [Fact]
    public async Task RecordTypeWithBoundParameters_ParameterCannotBeBound()
    {
        // Annotatons on properties do not appear on properties. If an attribute is never bound, the property is also not bound.

        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(RecordTypeWithFilteredProperty1)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Id=not-bound&Name=test");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<RecordTypeWithFilteredProperty1>(modelBindingResult.Model);
        Assert.Null(model.Id);
        Assert.Equal("test", model.Name);

        var entry = Assert.Single(modelState);
        Assert.Equal("Name", entry.Key);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    [Bind(include: new[] { "Name" })]
    public record RecordTypeWithFilteredProperty2(string Id, string Name);

    [Fact]
    public async Task RecordTypeWithBoundParameters_ParameterAreFiltered()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(RecordTypeWithFilteredProperty2)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Id=not-bound&Name=test");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<RecordTypeWithFilteredProperty2>(modelBindingResult.Model);
        Assert.Null(model.Id);
        Assert.Equal("test", model.Name);

        var entry = Assert.Single(modelState);
        Assert.Equal("Name", entry.Key);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    public record RecordTypesWithDifferentMetadataOnParameterAndProperty([FromQuery] string Id, string Name)
    {
        [FromHeader]
        public string Id { get; init; } = Id;

        public string Name { get; init; } = Name;
    }

    [Fact]
    public async Task RecordTypesWithDifferentMetadataOnParameterAndProperty_MetadataOnParameterIsUsed()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(RecordTypesWithDifferentMetadataOnParameterAndProperty)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.Headers.Add("Id", "not-bound");
            request.QueryString = new QueryString("?Id=testId&Name=test");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);
        Assert.True(modelState.IsValid);

        var model = Assert.IsType<RecordTypesWithDifferentMetadataOnParameterAndProperty>(modelBindingResult.Model);
        Assert.Equal("testId", model.Id);
        Assert.Equal("test", model.Name);

        Assert.Single(modelState, e => e.Key == "Name");
        Assert.Single(modelState, e => e.Key == "Id");
    }

    [Fact]
    public async Task RecordTypesWithDifferentMetadataOnParameterAndProperty_NoDataForParameter()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(RecordTypesWithDifferentMetadataOnParameterAndProperty)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.Headers.Add("Id", "not-bound");
            request.QueryString = new QueryString("?Name=test");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<RecordTypesWithDifferentMetadataOnParameterAndProperty>(modelBindingResult.Model);
        Assert.Null(model.Id);
        Assert.Equal("test", model.Name);

        var entry = Assert.Single(modelState);
        Assert.Equal("Name", entry.Key);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    private record RecordTypeWithCollectionParameter(string Id, IList<string> Tags);

    [Fact]
    public async Task RecordTypeWithCollectionParameter_WithData_Succeeds()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(RecordTypeWithCollectionParameter)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Id=test&Tags[0]=tag1&Tags[1]=tag2");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);
        Assert.True(modelState.IsValid);

        var model = Assert.IsType<RecordTypeWithCollectionParameter>(modelBindingResult.Model);
        Assert.Equal("test", model.Id);
        Assert.Equal(new[] { "tag1", "tag2" }, model.Tags);

        Assert.Single(modelState, e => e.Key == "Id");
        Assert.Single(modelState, e => e.Key == "Tags[0]");
        Assert.Single(modelState, e => e.Key == "Tags[1]");
    }

    [Fact]
    public async Task RecordTypeCollectionParameter_NoData()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(RecordTypeWithCollectionParameter)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Id=test");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<RecordTypeWithCollectionParameter>(modelBindingResult.Model);
        Assert.Equal("test", model.Id);
        Assert.Null(model.Tags);

        var entry = Assert.Single(modelState, e => e.Key == "Id");
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    private record RecordTypesWithReadOnlyCollectionParameter(string Id, string[] Tags);

    [Fact]
    public async Task RecordTypesWithReadOnlyCollectionParameter_Data_GetsBound()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(RecordTypesWithReadOnlyCollectionParameter)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Id=test&Tags[0]=tag1&Tags[1]=tag2");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);
        Assert.True(modelState.IsValid);

        var model = Assert.IsType<RecordTypesWithReadOnlyCollectionParameter>(modelBindingResult.Model);
        Assert.Equal("test", model.Id);
        Assert.Equal(new[] { "tag1", "tag2" }, model.Tags);

        Assert.Single(modelState, e => e.Key == "Id");
        Assert.Single(modelState, e => e.Key == "Tags[0]");
        Assert.Single(modelState, e => e.Key == "Tags[1]");
    }

    private record RecordTypesWithDefaultParameterValue(string Id = "default-id", string[] Tags = null);

    [Fact]
    public async Task RecordTypesWithDefaultParameterValue_Data()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(RecordTypesWithDefaultParameterValue)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Id=test&Tags[0]=tag1&Tags[1]=tag2");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);
        Assert.True(modelState.IsValid);
        Assert.Equal(0, modelState.ErrorCount);

        var model = Assert.IsType<RecordTypesWithDefaultParameterValue>(modelBindingResult.Model);
        Assert.Equal("test", model.Id);
        Assert.Equal(new[] { "tag1", "tag2" }, model.Tags);

        Assert.Single(modelState, e => e.Key == "Id");
        Assert.Single(modelState, e => e.Key == "Tags[0]");
        Assert.Single(modelState, e => e.Key == "Tags[1]");
    }

    [Fact]
    public async Task RecordTypesWithDefaultParameterValue_NoData()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(RecordTypesWithDefaultParameterValue)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);
        Assert.True(modelState.IsValid);

        var model = Assert.IsType<RecordTypesWithDefaultParameterValue>(modelBindingResult.Model);
        Assert.Equal("default-id", model.Id);
        Assert.Null(model.Tags);

        Assert.Empty(modelState);
    }

    [Fact]
    public async Task RecordTypesWithDefaultParameterValue_PartialData()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(RecordTypesWithDefaultParameterValue)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Tags[0]=tag");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);
        Assert.True(modelState.IsValid);

        var model = Assert.IsType<RecordTypesWithDefaultParameterValue>(modelBindingResult.Model);
        Assert.Equal("default-id", model.Id);
        Assert.Equal(new[] { "tag" }, model.Tags);

        Assert.Equal(0, modelState.ErrorCount);
        var entry = Assert.Single(modelState);
        Assert.Equal("Tags[0]", entry.Key);
    }

    [Fact]
    public async Task RecordTypesWithDefaultParameterValue_PartialDataWithPrefix()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(RecordTypesWithDefaultParameterValue)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter.Tags[0]=tag");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);
        Assert.True(modelState.IsValid);

        var model = Assert.IsType<RecordTypesWithDefaultParameterValue>(modelBindingResult.Model);
        Assert.Equal("default-id", model.Id);
        Assert.Equal(new[] { "tag" }, model.Tags);

        Assert.Equal(0, modelState.ErrorCount);
        var entry = Assert.Single(modelState);
        Assert.Equal("parameter.Tags[0]", entry.Key);
    }

    private record RecordTypeWithBindRequiredParameters([BindRequired] string Name, int Age);

    [Fact]
    public async Task RecordTypeWithBindRequiredParameters_Data_Success()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(RecordTypeWithBindRequiredParameters)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Name=test&Age=7");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);
        Assert.True(modelState.IsValid);

        var model = Assert.IsType<RecordTypeWithBindRequiredParameters>(modelBindingResult.Model);
        Assert.Equal("test", model.Name);
        Assert.Equal(7, model.Age);

        Assert.Equal(0, modelState.ErrorCount);
        Assert.Equal(2, modelState.Count);

        Assert.Single(modelState, m => m.Key == "Age");
        Assert.Single(modelState, m => m.Key == "Name");
    }

    [Fact]
    public async Task RecordTypeWithBindRequiredParameters_PartialData_BindRequiredError()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(RecordTypeWithBindRequiredParameters)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?Age=7");
        });

        var modelState = testContext.ModelState;
        var metadata = GetMetadata(testContext, parameter);
        var modelBinder = GetModelBinder(testContext, parameter, metadata);
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            testContext,
            modelBinder,
            valueProvider,
            parameter,
            metadata,
            value: null);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<RecordTypeWithBindRequiredParameters>(modelBindingResult.Model);
        Assert.Null(model.Name);
        Assert.Equal(7, model.Age);

        Assert.False(modelState.IsValid);
        Assert.Equal(1, modelState.ErrorCount);

        Assert.Equal(2, modelState.Count);
        var entry = Assert.Single(modelState, m => m.Key == "Age");
        Assert.Empty(entry.Value.Errors);

        entry = Assert.Single(modelState, m => m.Key == "Name");
        var error = Assert.Single(entry.Value.Errors);
        Assert.Equal("A value for the 'Name' parameter or property was not provided.", error.ErrorMessage);
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

        FormFileCollection fileCollection;
        if (request.HasFormContentType)
        {
            // Do less work and do not overwrite previous information if called a second time.
            fileCollection = (FormFileCollection)request.Form.Files;
        }
        else
        {
            fileCollection = new FormFileCollection();
            var formCollection = new FormCollection(new Dictionary<string, StringValues>(), fileCollection);

            request.ContentType = "multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq";
            request.Form = formCollection;
        }

        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = new FormFile(memoryStream, 0, memoryStream.Length, name, fileName)
        {
            Headers = new HeaderDictionary(),

            // Do not move this up. Headers must be non-null before the ContentDisposition property is accessed.
            ContentDisposition = $"form-data; name={name}; filename={fileName}",
        };

        fileCollection.Add(file);
    }

    private ModelMetadata GetMetadata(ModelBindingTestContext context, ParameterDescriptor parameter)
    {
        return context.MetadataProvider.GetMetadataForType(parameter.ParameterType);
    }

    private IModelBinder GetModelBinder(
        ModelBindingTestContext context,
        ParameterDescriptor parameter,
        ModelMetadata metadata)
    {
        var factory = ModelBindingTestHelper.GetModelBinderFactory(
            context.MetadataProvider,
            context.HttpContext.RequestServices);
        var factoryContext = new ModelBinderFactoryContext
        {
            BindingInfo = parameter.BindingInfo,
            CacheToken = parameter,
            Metadata = metadata,
        };

        return factory.CreateBinder(factoryContext);
    }
}
