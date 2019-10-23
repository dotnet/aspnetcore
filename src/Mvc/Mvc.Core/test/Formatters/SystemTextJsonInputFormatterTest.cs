// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
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

        protected override TextInputFormatter GetInputFormatter()
        {
            return new SystemTextJsonInputFormatter(new JsonOptions(), LoggerFactory.CreateLogger<SystemTextJsonInputFormatter>());
        }

        internal override string ReadAsync_AddsModelValidationErrorsToModelState_Expected => "$.Age";

        internal override string JsonFormatter_EscapedKeys_Expected => "$[0]['It\"s a key']";

        internal override string JsonFormatter_EscapedKeys_Bracket_Expected => "$[0]['It[s a key']";

        internal override string ReadAsync_ArrayOfObjects_HasCorrectKey_Expected => "$[2].Age";

        internal override string ReadAsync_InvalidArray_AddsOverflowErrorsToModelState_Expected => "$[2]";

        internal override string ReadAsync_InvalidComplexArray_AddsOverflowErrorsToModelState_Expected => "$[1].Small";

        internal override string ReadAsync_ComplexPoco_Expected => "$.Person.Numbers[2]";

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
                return short.Parse(reader.GetString());
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
                return DateTime.Parse(reader.GetString());
            }

            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }
    }
}
