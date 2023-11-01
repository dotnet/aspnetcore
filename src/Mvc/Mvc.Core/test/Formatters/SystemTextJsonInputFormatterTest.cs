// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Formatters;

public class SystemTextJsonInputFormatterTest : JsonInputFormatterTestBase
{
    [Fact]
    public override Task ReadAsync_AddsModelValidationErrorsToModelState()
    {
        return base.ReadAsync_AddsModelValidationErrorsToModelState();
    }

    [Fact]
    public override Task ReadAsync_InvalidArray_AddsOverflowErrorsToModelState()
    {
        return base.ReadAsync_InvalidArray_AddsOverflowErrorsToModelState();
    }

    [Fact]
    public override Task ReadAsync_InvalidComplexArray_AddsOverflowErrorsToModelState()
    {
        return base.ReadAsync_InvalidComplexArray_AddsOverflowErrorsToModelState();
    }

    [Fact]
    public override Task ReadAsync_UsesTryAddModelValidationErrorsToModelState()
    {
        return base.ReadAsync_UsesTryAddModelValidationErrorsToModelState();
    }

    [Fact(Skip = "https://github.com/dotnet/corefx/issues/38492")]
    public override Task ReadAsync_RequiredAttribute()
    {
        // System.Text.Json does not yet support an equivalent of Required.
        throw new NotImplementedException();
    }

    [Fact]
    public override Task JsonFormatter_EscapedKeys()
    {
        return base.JsonFormatter_EscapedKeys();
    }

    [Fact]
    public override Task JsonFormatter_EscapedKeys_Bracket()
    {
        return base.JsonFormatter_EscapedKeys_Bracket();
    }

    [Fact]
    public override Task JsonFormatter_EscapedKeys_SingleQuote()
    {
        return base.JsonFormatter_EscapedKeys_SingleQuote();
    }

    [Fact]
    public async Task ReadAsync_SingleError()
    {
        // Arrange
        var formatter = GetInputFormatter();

        var content = "[5, 'seven', 3, notnum ]";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(List<int>), httpContext);

        // Act
        await formatter.ReadAsync(formatterContext);

        Assert.Collection(
            formatterContext.ModelState.OrderBy(k => k),
            kvp =>
            {
                Assert.Equal("$[1]", kvp.Key);
                var error = Assert.Single(kvp.Value.Errors);
                Assert.StartsWith("''' is an invalid start of a value", error.ErrorMessage);
            });
    }

    [Fact]
    public async Task ReadAsync_DoesNotThrowFormatException()
    {
        // Arrange
        var formatter = GetInputFormatter();

        var contentBytes = Encoding.UTF8.GetBytes("{\"dateValue\":\"not-a-date\"}");
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(TypeWithBadConverters), httpContext);

        // Act
        await formatter.ReadAsync(formatterContext);

        Assert.False(formatterContext.ModelState.IsValid);
        var kvp = Assert.Single(formatterContext.ModelState);
        Assert.Empty(kvp.Key);
        var error = Assert.Single(kvp.Value.Errors);
        Assert.Equal("The supplied value is invalid.", error.ErrorMessage);
    }

    [Fact]
    public async Task ReadAsync_DoesNotThrowOverflowException()
    {
        // Arrange
        var formatter = GetInputFormatter();

        var contentBytes = Encoding.UTF8.GetBytes("{\"shortValue\":\"32768\"}");
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(TypeWithBadConverters), httpContext);

        // Act
        await formatter.ReadAsync(formatterContext);

        Assert.False(formatterContext.ModelState.IsValid);
        var kvp = Assert.Single(formatterContext.ModelState);
        Assert.Empty(kvp.Key);
        var error = Assert.Single(kvp.Value.Errors);
        Assert.Equal("The supplied value is invalid.", error.ErrorMessage);
    }

    [Theory]
    [InlineData("{", "$", "Expected depth to be zero at the end of the JSON payload. There is an open JSON object or array that should be closed. Path: $ | LineNumber: 0 | BytePositionInLine: 1.")]
    [InlineData("{\"a\":{\"b\"}}", "$.a", "'}' is invalid after a property name. Expected a ':'. Path: $.a | LineNumber: 0 | BytePositionInLine: 9.")]
    [InlineData("{\"age\":\"x\"}", "$.age", "The JSON value could not be converted to System.Decimal. Path: $.age | LineNumber: 0 | BytePositionInLine: 10.")]
    [InlineData("{\"login\":1}", "$.login", "The JSON value could not be converted to Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonInputFormatterTest+UserLogin. Path: $.login | LineNumber: 0 | BytePositionInLine: 10.")]
    public async Task ReadAsync_WithAllowInputFormatterExceptionMessages_RegistersJsonInputExceptionsAsInputFormatterException(
                string content,
                string modelStateKey,
                string expectedMessage)
    {
        // Arrange
        var formatter = GetInputFormatter(allowInputFormatterExceptionMessages: true);

        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(User), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.True(result.HasError);
        Assert.True(!formatterContext.ModelState.IsValid);
        Assert.True(formatterContext.ModelState.ContainsKey(modelStateKey));

        var modelError = formatterContext.ModelState[modelStateKey].Errors.Single();
        Assert.Equal(expectedMessage, modelError.ErrorMessage);
    }

    [Fact]
    public async Task ReadAsync_DoNotAllowInputFormatterExceptionMessages_DoesNotWrapJsonInputExceptions()
    {
        // Arrange
        var formatter = GetInputFormatter(allowInputFormatterExceptionMessages: false);
        var contentBytes = Encoding.UTF8.GetBytes("{");
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(User), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.True(result.HasError);
        Assert.True(!formatterContext.ModelState.IsValid);
        Assert.True(formatterContext.ModelState.ContainsKey("$"));

        var modelError = formatterContext.ModelState["$"].Errors.Single();
        Assert.IsNotType<InputFormatterException>(modelError.Exception);
        Assert.Empty(modelError.ErrorMessage);
    }

    protected override TextInputFormatter GetInputFormatter(bool allowInputFormatterExceptionMessages = true)
    {
        return new SystemTextJsonInputFormatter(
            new JsonOptions
            {
                AllowInputFormatterExceptionMessages = allowInputFormatterExceptionMessages
            },
            LoggerFactory.CreateLogger<SystemTextJsonInputFormatter>());
    }

    internal override string ReadAsync_AddsModelValidationErrorsToModelState_Expected => "$.Age";

    internal override string JsonFormatter_EscapedKeys_Expected => "$[0]['It\"s a key']";

    internal override string JsonFormatter_EscapedKeys_Bracket_Expected => "$[0]['It[s a key']";

    internal override string JsonFormatter_EscapedKeys_SingleQuote_Expected => "$[0]['It's a key']";

    internal override string ReadAsync_ArrayOfObjects_HasCorrectKey_Expected => "$[2].Age";

    internal override string ReadAsync_InvalidArray_AddsOverflowErrorsToModelState_Expected => "$[2]";

    internal override string ReadAsync_InvalidComplexArray_AddsOverflowErrorsToModelState_Expected => "$[1].Small";

    internal override string ReadAsync_ComplexPoco_Expected => "$.Person.Numbers[2]";

    internal override string ReadAsync_NestedParseError_Expected => "$.b.c.d";

    private class TypeWithBadConverters
    {
        [JsonConverter(typeof(DateTimeConverter))]
        public DateTime DateValue { get; set; }

        [JsonConverter(typeof(ShortConverter))]
        public short ShortValue { get; set; }
    }

    private class ShortConverter : JsonConverter<short>
    {
        public override short Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return short.Parse(reader.GetString(), CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, short value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    private class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString(), CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class User
    {
        public string Name { get; set; }

        public decimal Age { get; set; }

        public byte Small { get; set; }

        public UserLogin Login { get; set; }
    }

    private sealed class UserLogin
    {
        public string UserName { get; set; }

        public string Password { get; set; }
    }
}
