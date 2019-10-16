// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class ValidationProblemDetailsJsonConverterTest
    {
        private static JsonSerializerOptions JsonSerializerOptions => new JsonOptions().JsonSerializerOptions;

        [Fact]
        public void Read_Works()
        {
            // Arrange
            var type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
            var title = "Not found";
            var status = 404;
            var detail = "Product not found";
            var instance = "http://example.com/products/14";
            var traceId = "|37dd3dd5-4a9619f953c40a16.";
            var json = $"{{\"type\":\"{type}\",\"title\":\"{title}\",\"status\":{status},\"detail\":\"{detail}\", \"instance\":\"{instance}\",\"traceId\":\"{traceId}\"," +
                "\"errors\":{\"key0\":[\"error0\"],\"key1\":[\"error1\",\"error2\"]}}";
            var converter = new ValidationProblemDetailsJsonConverter();
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            reader.Read();

            // Act
            var problemDetails = converter.Read(ref reader, typeof(ValidationProblemDetails), JsonSerializerOptions);

            Assert.Equal(type, problemDetails.Type);
            Assert.Equal(title, problemDetails.Title);
            Assert.Equal(status, problemDetails.Status);
            Assert.Equal(instance, problemDetails.Instance);
            Assert.Equal(detail, problemDetails.Detail);
            Assert.Collection(
                problemDetails.Extensions,
                kvp =>
                {
                    Assert.Equal("traceId", kvp.Key);
                    Assert.Equal(traceId, kvp.Value.ToString());
                });
            Assert.Collection(
                problemDetails.Errors.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    Assert.Equal("key0", kvp.Key);
                    Assert.Equal(new[] { "error0" }, kvp.Value);
                },
                kvp =>
                {
                    Assert.Equal("key1", kvp.Key);
                    Assert.Equal(new[] { "error1", "error2" }, kvp.Value);
                });
        }

        [Fact]
        public void Read_WithSomeMissingValues_Works()
        {
            // Arrange
            var type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
            var title = "Not found";
            var status = 404;
            var traceId = "|37dd3dd5-4a9619f953c40a16.";
            var json = $"{{\"type\":\"{type}\",\"title\":\"{title}\",\"status\":{status},\"traceId\":\"{traceId}\"," +
                "\"errors\":{\"key0\":[\"error0\"],\"key1\":[\"error1\",\"error2\"]}}";
            var converter = new ValidationProblemDetailsJsonConverter();
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            reader.Read();

            // Act
            var problemDetails = converter.Read(ref reader, typeof(ValidationProblemDetails), JsonSerializerOptions);

            Assert.Equal(type, problemDetails.Type);
            Assert.Equal(title, problemDetails.Title);
            Assert.Equal(status, problemDetails.Status);
            Assert.Collection(
                problemDetails.Extensions,
                kvp =>
                {
                    Assert.Equal("traceId", kvp.Key);
                    Assert.Equal(traceId, kvp.Value.ToString());
                });
            Assert.Collection(
                problemDetails.Errors.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    Assert.Equal("key0", kvp.Key);
                    Assert.Equal(new[] { "error0" }, kvp.Value);
                },
                kvp =>
                {
                    Assert.Equal("key1", kvp.Key);
                    Assert.Equal(new[] { "error1", "error2" }, kvp.Value);
                });
        }

        [Fact]
        public void ReadUsingJsonSerializerWorks()
        {
            // Arrange
            var type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
            var title = "Not found";
            var status = 404;
            var traceId = "|37dd3dd5-4a9619f953c40a16.";
            var json = $"{{\"type\":\"{type}\",\"title\":\"{title}\",\"status\":{status},\"traceId\":\"{traceId}\"," +
                "\"errors\":{\"key0\":[\"error0\"],\"key1\":[\"error1\",\"error2\"]}}";

            // Act
            var problemDetails = JsonSerializer.Deserialize<ValidationProblemDetails>(json, JsonSerializerOptions);

            Assert.Equal(type, problemDetails.Type);
            Assert.Equal(title, problemDetails.Title);
            Assert.Equal(status, problemDetails.Status);
            Assert.Collection(
                problemDetails.Extensions,
                kvp =>
                {
                    Assert.Equal("traceId", kvp.Key);
                    Assert.Equal(traceId, kvp.Value.ToString());
                });
            Assert.Collection(
                problemDetails.Errors.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    Assert.Equal("key0", kvp.Key);
                    Assert.Equal(new[] { "error0" }, kvp.Value);
                },
                kvp =>
                {
                    Assert.Equal("key1", kvp.Key);
                    Assert.Equal(new[] { "error1", "error2" }, kvp.Value);
                });
        }

        [Fact]
        public void Write_Works()
        {
            // Arrange
            var traceId = "|37dd3dd5-4a9619f953c40a16.";
            var value = new ProblemDetails
            {
                Title = "Not found",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Status = 404,
                Detail = "Product not found",
                Instance = "http://example.com/products/14",
                Extensions =
                {
                    { "traceId", traceId },
                    { "some-data", new[] { "value1", "value2" } }
                }
            };
            var expected = $"{{\"type\":\"{JsonEncodedText.Encode(value.Type)}\",\"title\":\"{value.Title}\",\"status\":{value.Status},\"detail\":\"{value.Detail}\",\"instance\":\"{JsonEncodedText.Encode(value.Instance)}\",\"traceId\":\"{traceId}\",\"some-data\":[\"value1\",\"value2\"]}}";
            var converter = new ProblemDetailsJsonConverter();
            var stream = new MemoryStream();

            // Act
            using (var writer = new Utf8JsonWriter(stream))
            {
                converter.Write(writer, value, JsonSerializerOptions);
            }

            // Assert
            var actual = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Write_WithSomeMissingContent_Works()
        {
            // Arrange
            var value = new ProblemDetails
            {
                Title = "Not found",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Status = 404,
            };
            var expected = $"{{\"type\":\"{JsonEncodedText.Encode(value.Type)}\",\"title\":\"{value.Title}\",\"status\":{value.Status}}}";
            var converter = new ProblemDetailsJsonConverter();
            var stream = new MemoryStream();

            // Act
            using (var writer = new Utf8JsonWriter(stream))
            {
                converter.Write(writer, value, JsonSerializerOptions);
            }

            // Assert
            var actual = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, actual);
        }
    }
}
