// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;

namespace Microsoft.AspNetCore.Http.Abstractions.Tests;

public class HttpValidationProblemDetailsJsonConverterTest
{
    private static JsonSerializerOptions JsonSerializerOptions => new JsonOptions().SerializerOptions;

    [Fact]
    public void Write_Works()
    {
        var problemDetails = new HttpValidationProblemDetails();

        problemDetails.Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5";
        problemDetails.Title = "Not found";
        problemDetails.Status = 404;
        problemDetails.Detail = "Product not found";
        problemDetails.Instance = "http://example.com/products/14";
        problemDetails.Extensions["traceId"] = "|37dd3dd5-4a9619f953c40a16.";
        problemDetails.Errors.Add("key0", new[] { "error0" });
        problemDetails.Errors.Add("key1", new[] { "error1", "error2" });

        var ms = new MemoryStream();
        var writer = new Utf8JsonWriter(ms);
        JsonSerializer.Serialize(writer, problemDetails, JsonSerializerOptions);
        writer.Flush();

        ms.Seek(0, SeekOrigin.Begin);
        var document = JsonDocument.Parse(ms);
        Assert.Equal(problemDetails.Type, document.RootElement.GetProperty("type").GetString());
        Assert.Equal(problemDetails.Title, document.RootElement.GetProperty("title").GetString());
        Assert.Equal(problemDetails.Status, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(problemDetails.Detail, document.RootElement.GetProperty("detail").GetString());
        Assert.Equal(problemDetails.Instance, document.RootElement.GetProperty("instance").GetString());
        Assert.Equal((string)problemDetails.Extensions["traceId"]!, document.RootElement.GetProperty("traceId").GetString());
        var errorsElement = document.RootElement.GetProperty("errors");
        Assert.Equal("error0", errorsElement.GetProperty("key0")[0].GetString());
        Assert.Equal("error1", errorsElement.GetProperty("key1")[0].GetString());
        Assert.Equal("error2", errorsElement.GetProperty("key1")[1].GetString());
    }

    [Fact]
    public void Read_Works()
    {
        // Arrange
        var type = "https://tools.ietf.org/html/rfc9110#section-15.5.5";
        var title = "Not found";
        var status = 404;
        var detail = "Product not found";
        var instance = "http://example.com/products/14";
        var traceId = "|37dd3dd5-4a9619f953c40a16.";
        var json = $"{{\"type\":\"{type}\",\"title\":\"{title}\",\"status\":{status},\"detail\":\"{detail}\", \"instance\":\"{instance}\",\"traceId\":\"{traceId}\"," +
            "\"errors\":{\"key0\":[\"error0\"],\"key1\":[\"error1\",\"error2\"]}}";
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();

        // Act
        var problemDetails = JsonSerializer.Deserialize<HttpValidationProblemDetails>(ref reader, JsonSerializerOptions);

        Assert.NotNull(problemDetails);
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
                Assert.Equal(traceId, kvp.Value?.ToString());
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
        var type = "https://tools.ietf.org/html/rfc9110#section-15.5.5";
        var title = "Not found";
        var status = 404;
        var traceId = "|37dd3dd5-4a9619f953c40a16.";
        var json = $"{{\"type\":\"{type}\",\"title\":\"{title}\",\"status\":{status},\"traceId\":\"{traceId}\"," +
            "\"errors\":{\"key0\":[\"error0\"],\"key1\":[\"error1\",\"error2\"]}}";
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();

        // Act
        var problemDetails = JsonSerializer.Deserialize<HttpValidationProblemDetails>(ref reader, JsonSerializerOptions);

        Assert.NotNull(problemDetails);
        Assert.Equal(type, problemDetails.Type);
        Assert.Equal(title, problemDetails.Title);
        Assert.Equal(status, problemDetails.Status);
        Assert.Collection(
            problemDetails.Extensions,
            kvp =>
            {
                Assert.Equal("traceId", kvp.Key);
                Assert.Equal(traceId, kvp.Value?.ToString());
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
        var type = "https://tools.ietf.org/html/rfc9110#section-15.5.5";
        var title = "Not found";
        var status = 404;
        var traceId = "|37dd3dd5-4a9619f953c40a16.";
        var json = $"{{\"type\":\"{type}\",\"title\":\"{title}\",\"status\":{status},\"traceId\":\"{traceId}\"," +
            "\"errors\":{\"key0\":[\"error0\"],\"key1\":[\"error1\",\"error2\"]}}";

        // Act
        var problemDetails = JsonSerializer.Deserialize<HttpValidationProblemDetails>(json, JsonSerializerOptions);

        Assert.NotNull(problemDetails);
        Assert.Equal(type, problemDetails!.Type);
        Assert.Equal(title, problemDetails.Title);
        Assert.Equal(status, problemDetails.Status);
        Assert.Collection(
            problemDetails.Extensions,
            kvp =>
            {
                Assert.Equal("traceId", kvp.Key);
                Assert.Equal(traceId, kvp.Value?.ToString());
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
    public void WriteUsingJsonSerializerOptionsWithDifferentPoliciesWorks()
    {
        var errors = new Dictionary<string, string[]>()
        {
            { "Property", new[] { "error0" } },
            { "TwoWords", new[] { "error1" } },
            { "TopLevelProperty.PropertyName", new[] { "error2" } }
        };
        
        var problemDetails = new HttpValidationProblemDetails(errors)
        {
            Title = "Validation error",
            Status = 400
        };

        // Test CamelCase
        {
            using MemoryStream stream = new();
            using Utf8JsonWriter writer = new(stream);

            var options = new JsonOptions().SerializerOptions;
            options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;

            JsonSerializer.Serialize(writer, problemDetails, options);

            writer.Flush();
            var json = Encoding.UTF8.GetString(stream.ToArray());

            var expectedJSON = $"{{\"title\":\"{problemDetails.Title}\",\"status\":{problemDetails.Status}," +
                "\"errors\":{\"property\":[\"error0\"],\"twoWords\":[\"error1\"],\"topLevelProperty.PropertyName\":[\"error2\"]}}";
            Assert.NotNull(json);
            Assert.Equal(expectedJSON, json);
        }

        // Test KebabCase
        {
            using MemoryStream stream = new();
            using Utf8JsonWriter writer = new(stream);

            var options = new JsonOptions().SerializerOptions;
            options.DictionaryKeyPolicy = new KebabCaseDictionaryNamingPolicy();

            JsonSerializer.Serialize(writer, problemDetails, options);

            writer.Flush();
            var json = Encoding.UTF8.GetString(stream.ToArray());

            var expectedJSON = $"{{\"title\":\"{problemDetails.Title}\",\"status\":{problemDetails.Status}," +
                "\"errors\":{\"property\":[\"error0\"],\"two-words\":[\"error1\"],\"top-level-property.property-name\":[\"error2\"]}}";
            Assert.NotNull(json);
            Assert.Equal(expectedJSON, json);
        }
    }

    private class KebabCaseDictionaryNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var parts = name.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]))
                    continue;

                var result = string.Empty;
                for (var j = 0; j < parts[i].Length; j++)
                {
                    if (j > 0 && char.IsUpper(parts[i][j]))
                        result += "-";
                    result += char.ToLowerInvariant(parts[i][j]);
                }
                parts[i] = result;
            }

            return string.Join(".", parts);
        }
    }
}
