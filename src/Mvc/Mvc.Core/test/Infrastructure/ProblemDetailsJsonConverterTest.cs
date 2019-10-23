// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class ProblemDetailsJsonConverterTest
    {
        private static JsonSerializerOptions JsonSerializerOptions => new JsonOptions().JsonSerializerOptions;

        [Fact]
        public void Read_ThrowsIfJsonIsIncomplete()
        {
            // Arrange
            var json = "{";
            var converter = new ProblemDetailsJsonConverter();

            // Act & Assert
            var ex = Record.Exception(() =>
            {
                var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
                converter.Read(ref reader, typeof(ProblemDetails), JsonSerializerOptions);
            });
            Assert.IsAssignableFrom<JsonException>(ex);
        }

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
            var json = $"{{\"type\":\"{type}\",\"title\":\"{title}\",\"status\":{status},\"detail\":\"{detail}\", \"instance\":\"{instance}\",\"traceId\":\"{traceId}\"}}";
            var converter = new ProblemDetailsJsonConverter();
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            reader.Read();

            // Act
            var problemDetails = converter.Read(ref reader, typeof(ProblemDetails), JsonSerializerOptions);

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
        }

        [Fact]
        public void Read_UsingJsonSerializerWorks()
        {
            // Arrange
            var type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
            var title = "Not found";
            var status = 404;
            var detail = "Product not found";
            var instance = "http://example.com/products/14";
            var traceId = "|37dd3dd5-4a9619f953c40a16.";
            var json = $"{{\"type\":\"{type}\",\"title\":\"{title}\",\"status\":{status},\"detail\":\"{detail}\", \"instance\":\"{instance}\",\"traceId\":\"{traceId}\"}}";

            // Act
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(json, JsonSerializerOptions);

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
        }

        [Fact]
        public void Read_WithSomeMissingValues_Works()
        {
            // Arrange
            var type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
            var title = "Not found";
            var status = 404;
            var traceId = "|37dd3dd5-4a9619f953c40a16.";
            var json = $"{{\"type\":\"{type}\",\"title\":\"{title}\",\"status\":{status},\"traceId\":\"{traceId}\"}}";
            var converter = new ProblemDetailsJsonConverter();
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            reader.Read();

            // Act
            var problemDetails = converter.Read(ref reader, typeof(ProblemDetails), JsonSerializerOptions);

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
