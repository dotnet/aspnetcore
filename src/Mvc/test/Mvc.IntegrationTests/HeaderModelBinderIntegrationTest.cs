// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

public class HeaderModelBinderIntegrationTest
{
    private class Person
    {
        public Address Address { get; set; }
    }

    private class Address
    {
        [FromHeader(Name = "Header")]
        [BindRequired]
        public string Street { get; set; }
    }

    [Fact]
    public async Task BindPropertyFromHeader_NoData_UsesFullPathAsKeyForModelStateErrors()
    {
        // Arrange
        var expected = "A value for the 'Header' parameter or property was not provided.";
        var parameter = new ParameterDescriptor()
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo()
            {
                BinderModelName = "CustomParameter",
            },
            ParameterType = typeof(Person)
        };

        // Do not add any headers.
        var testContext = GetModelBindingTestContext();
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert

        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);

        // Model
        var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
        Assert.NotNull(boundPerson);

        // ModelState
        Assert.False(modelState.IsValid);
        var key = Assert.Single(modelState.Keys);
        Assert.Equal("CustomParameter.Address.Header", key);
        var error = Assert.Single(modelState[key].Errors);
        Assert.Equal(expected, error.ErrorMessage);
    }

    [Fact]
    public async Task BindPropertyFromHeader_WithPrefix_GetsBound()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo()
            {
                BinderModelName = "prefix",
            },
            ParameterType = typeof(Person)
        };

        var testContext = GetModelBindingTestContext(
            request => request.Headers.Add("Header", new[] { "someValue" }));
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert

        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);

        // Model
        var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
        Assert.NotNull(boundPerson);
        Assert.NotNull(boundPerson.Address);
        Assert.Equal("someValue", boundPerson.Address.Street);

        // ModelState
        Assert.True(modelState.IsValid);
        var entry = Assert.Single(modelState);
        Assert.Equal("prefix.Address.Header", entry.Key);
        Assert.Empty(entry.Value.Errors);
        Assert.Equal(ModelValidationState.Valid, entry.Value.ValidationState);
        Assert.Equal("someValue", entry.Value.AttemptedValue);
        Assert.Equal("someValue", entry.Value.RawValue);
    }

    // The scenario is interesting as we to bind the top level model we fallback to empty prefix,
    // and hence the model state keys have an empty prefix.
    [Fact]
    public async Task BindPropertyFromHeader_WithData_WithEmptyPrefix_GetsBound()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo(),
            ParameterType = typeof(Person)
        };

        var testContext = GetModelBindingTestContext(
            request => request.Headers.Add("Header", new[] { "someValue" }));
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert

        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);

        // Model
        var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
        Assert.NotNull(boundPerson);
        Assert.NotNull(boundPerson.Address);
        Assert.Equal("someValue", boundPerson.Address.Street);

        // ModelState
        Assert.True(modelState.IsValid);
        var entry = Assert.Single(modelState);
        Assert.Equal("Address.Header", entry.Key);
        Assert.Empty(entry.Value.Errors);
        Assert.Equal(ModelValidationState.Valid, entry.Value.ValidationState);
        Assert.Equal("someValue", entry.Value.AttemptedValue);
        Assert.Equal("someValue", entry.Value.RawValue);
    }

    private class ListContainer1
    {
        [FromHeader(Name = "Header")]
        public List<string> ListProperty { get; set; }
    }

    [Fact]
    public async Task BindCollectionPropertyFromHeader_WithData_IsBound()
    {
        // Arrange
        var parameter = new ParameterDescriptor
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo(),
            ParameterType = typeof(ListContainer1),
        };

        var testContext = GetModelBindingTestContext(
            request => request.Headers.Add("Header", new[] { "someValue" }));
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
        var modelState = testContext.ModelState;

        // Act
        var result = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(result.IsModelSet);

        // Model
        var boundContainer = Assert.IsType<ListContainer1>(result.Model);
        Assert.NotNull(boundContainer);
        Assert.NotNull(boundContainer.ListProperty);
        var entry = Assert.Single(boundContainer.ListProperty);
        Assert.Equal("someValue", entry);

        // ModelState
        Assert.True(modelState.IsValid);
        var kvp = Assert.Single(modelState);
        Assert.Equal("Header", kvp.Key);
        var modelStateEntry = kvp.Value;
        Assert.NotNull(modelStateEntry);
        Assert.Empty(modelStateEntry.Errors);
        Assert.Equal(ModelValidationState.Valid, modelStateEntry.ValidationState);
        Assert.Equal("someValue", modelStateEntry.AttemptedValue);
        Assert.Equal("someValue", modelStateEntry.RawValue);
    }

    private class ListContainer2
    {
        [FromHeader(Name = "Header")]
        public List<string> ListProperty { get; } = new List<string> { "One", "Two", "Three" };
    }

    [Fact]
    public async Task BindReadOnlyCollectionPropertyFromHeader_WithData_IsBound()
    {
        // Arrange
        var parameter = new ParameterDescriptor
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo(),
            ParameterType = typeof(ListContainer2),
        };

        var testContext = GetModelBindingTestContext(
            request => request.Headers.Add("Header", new[] { "someValue" }));
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
        var modelState = testContext.ModelState;

        // Act
        var result = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(result.IsModelSet);

        // Model
        var boundContainer = Assert.IsType<ListContainer2>(result.Model);
        Assert.NotNull(boundContainer);
        Assert.NotNull(boundContainer.ListProperty);
        var entry = Assert.Single(boundContainer.ListProperty);
        Assert.Equal("someValue", entry);

        // ModelState
        Assert.True(modelState.IsValid);
        var kvp = Assert.Single(modelState);
        Assert.Equal("Header", kvp.Key);
        var modelStateEntry = kvp.Value;
        Assert.NotNull(modelStateEntry);
        Assert.Empty(modelStateEntry.Errors);
        Assert.Equal(ModelValidationState.Valid, modelStateEntry.ValidationState);
        Assert.Equal("someValue", modelStateEntry.AttemptedValue);
        Assert.Equal("someValue", modelStateEntry.RawValue);
    }

    [Theory]
    [InlineData(typeof(string[]), "value1, value2, value3")]
    [InlineData(typeof(string), "value")]
    public async Task BindParameterFromHeader_WithData_WithPrefix_ModelGetsBound(Type modelType, string value)
    {
        // Arrange
        string expectedAttemptedValue;
        object expectedRawValue;
        if (modelType == typeof(string))
        {
            expectedAttemptedValue = value;
            expectedRawValue = value;
        }
        else
        {
            expectedAttemptedValue = value.Replace(" ", "");
            expectedRawValue = value.Split(',').Select(v => v.Trim()).ToArray();
        }

        var parameter = new ParameterDescriptor
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo
            {
                BinderModelName = "CustomParameter",
                BindingSource = BindingSource.Header
            },
            ParameterType = modelType
        };

        void action(HttpRequest r) => r.Headers.Add("CustomParameter", new[] { expectedAttemptedValue });
        var testContext = GetModelBindingTestContext(action);
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);

        // Do not add any headers.
        var httpContext = testContext.HttpContext;
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert

        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);

        // Model
        Assert.NotNull(modelBindingResult.Model);
        Assert.IsType(modelType, modelBindingResult.Model);

        // ModelState
        Assert.True(modelState.IsValid);
        var entry = Assert.Single(modelState);
        Assert.Equal("CustomParameter", entry.Key);
        Assert.Empty(entry.Value.Errors);
        Assert.Equal(ModelValidationState.Valid, entry.Value.ValidationState);
        Assert.Equal(expectedAttemptedValue, entry.Value.AttemptedValue);
        Assert.Equal(expectedRawValue, entry.Value.RawValue);
    }

    [Fact]
    public async Task BindPropertyFromHeader_WithPrefix_GetsBound_ForSimpleTypes()
    {
        // Arrange
        var parameter = new ParameterDescriptor()
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo()
            {
                BinderModelName = "prefix",
            },
            ParameterType = typeof(Product)
        };

        var testContext = GetModelBindingTestContext(
            request =>
            {
                request.Headers.Add("NoCommaString", "someValue");
                request.Headers.Add("OneCommaSeparatedString", "one, two, three");
                request.Headers.Add("IntProperty", "10");
                request.Headers.Add("NullableIntProperty", "300");
                request.Headers.Add("ArrayOfString", "first, second");
                request.Headers.Add("EnumerableOfDouble", "10.51, 45.44");
                request.Headers.Add("ListOfEnum", "Sedan, Coupe");
                request.Headers.Add("ListOfOrderWithTypeConverter", "10");
            });
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert

        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);

        // Model
        var product = Assert.IsType<Product>(modelBindingResult.Model);
        Assert.NotNull(product);
        Assert.NotNull(product.Manufacturer);
        Assert.Equal("someValue", product.Manufacturer.NoCommaString);
        Assert.Equal("one, two, three", product.Manufacturer.OneCommaSeparatedStringProperty);
        Assert.Equal(10, product.Manufacturer.IntProperty);
        Assert.Equal(300, product.Manufacturer.NullableIntProperty);
        Assert.Null(product.Manufacturer.NullableLongProperty);
        Assert.Equal(new[] { "first", "second" }, product.Manufacturer.ArrayOfString);
        Assert.Equal(new double[] { 10.51, 45.44 }, product.Manufacturer.EnumerableOfDoubleProperty);
        Assert.Equal(new CarType[] { CarType.Sedan, CarType.Coupe }, product.Manufacturer.ListOfEnum);
        var orderWithTypeConverter = Assert.Single(product.Manufacturer.ListOfOrderWithTypeConverterProperty);
        Assert.Equal(10, orderWithTypeConverter.Id);

        // ModelState
        Assert.True(modelState.IsValid);
        Assert.Collection(
            modelState.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("prefix.Manufacturer.ArrayOfString", kvp.Key);
                var entry = kvp.Value;
                Assert.Empty(entry.Errors);
                Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
                Assert.Equal("first,second", entry.AttemptedValue);
                Assert.Equal(new[] { "first", "second" }, entry.RawValue);
            },
            kvp =>
            {
                Assert.Equal("prefix.Manufacturer.EnumerableOfDouble", kvp.Key);
                var entry = kvp.Value;
                Assert.Empty(entry.Errors);
                Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
                Assert.Equal("10.51,45.44", entry.AttemptedValue);
                Assert.Equal(new[] { "10.51", "45.44" }, entry.RawValue);
            },
            kvp =>
            {
                Assert.Equal("prefix.Manufacturer.IntProperty", kvp.Key);
                var entry = kvp.Value;
                Assert.Empty(entry.Errors);
                Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
                Assert.Equal("10", entry.AttemptedValue);
                Assert.Equal("10", entry.RawValue);
            },
            kvp =>
            {
                Assert.Equal("prefix.Manufacturer.ListOfEnum", kvp.Key);
                var entry = kvp.Value;
                Assert.Empty(entry.Errors);
                Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
                Assert.Equal("Sedan,Coupe", entry.AttemptedValue);
                Assert.Equal(new[] { "Sedan", "Coupe" }, entry.RawValue);
            },
            kvp =>
            {
                Assert.Equal("prefix.Manufacturer.ListOfOrderWithTypeConverter", kvp.Key);
                var entry = kvp.Value;
                Assert.Empty(entry.Errors);
                Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
                Assert.Equal("10", entry.AttemptedValue);
                Assert.Equal("10", entry.RawValue);
            },
            kvp =>
            {
                Assert.Equal("prefix.Manufacturer.NoCommaString", kvp.Key);
                var entry = kvp.Value;
                Assert.Empty(entry.Errors);
                Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
                Assert.Equal("someValue", entry.AttemptedValue);
                Assert.Equal("someValue", entry.RawValue);
            },
            kvp =>
            {
                Assert.Equal("prefix.Manufacturer.NullableIntProperty", kvp.Key);
                var entry = kvp.Value;
                Assert.Empty(entry.Errors);
                Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
                Assert.Equal("300", entry.AttemptedValue);
                Assert.Equal("300", entry.RawValue);
            },
            kvp =>
            {
                Assert.Equal("prefix.Manufacturer.OneCommaSeparatedString", kvp.Key);
                var entry = kvp.Value;
                Assert.Empty(entry.Errors);
                Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
                Assert.Equal("one, two, three", entry.AttemptedValue);
                Assert.Equal("one, two, three", entry.RawValue);
            });
    }

    private ModelBindingTestContext GetModelBindingTestContext(
        Action<HttpRequest> updateRequest = null,
        Action<MvcOptions> updateOptions = null)
    {
        return ModelBindingTestHelper.GetTestContext(updateRequest, updateOptions);
    }

    private class Product
    {
        public Manufacturer Manufacturer { get; set; }
    }

    private class Manufacturer
    {
        [FromHeader]
        public string NoCommaString { get; set; }

        [FromHeader(Name = "OneCommaSeparatedString")]
        public string OneCommaSeparatedStringProperty { get; set; }

        [FromHeader]
        public int IntProperty { get; set; }

        [FromHeader]
        public int? NullableIntProperty { get; set; }

        [FromHeader]
        public long? NullableLongProperty { get; set; }

        [FromHeader]
        public string[] ArrayOfString { get; set; }

        [FromHeader(Name = "EnumerableOfDouble")]
        public IEnumerable<double> EnumerableOfDoubleProperty { get; set; }

        [FromHeader]
        public List<CarType> ListOfEnum { get; set; }

        [FromHeader(Name = "ListOfOrderWithTypeConverter")]
        public List<OrderWithTypeConverter> ListOfOrderWithTypeConverterProperty { get; set; }
    }

    private enum CarType
    {
        Coupe,
        Sedan
    }

    [TypeConverter(typeof(CanConvertFromStringConverter))]
    private class OrderWithTypeConverter : IEquatable<OrderWithTypeConverter>
    {
        public int Id { get; set; }

        public int ItemCount { get; set; }

        public bool Equals(OrderWithTypeConverter other)
        {
            return Id == other.Id;
        }
    }

    private class CanConvertFromStringConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var id = value.ToString();
            return new OrderWithTypeConverter() { Id = int.Parse(id, CultureInfo.InvariantCulture) };
        }
    }
}
