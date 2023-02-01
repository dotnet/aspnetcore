// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Http.HttpResults;

public partial class HttpResultsHelperTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WriteResultAsJsonAsync_Works_ForValueTypes(bool useJsonContext)
    {
        // Arrange
        var value = new TodoStruct()
        {
            Id = 1,
            IsComplete = true,
            Name = "Write even more tests!",
        };
        var responseBodyStream = new MemoryStream();
        var httpContext = CreateHttpContext(responseBodyStream);
        var serializerOptions = new JsonOptions().SerializerOptions;

        if (useJsonContext)
        {
            serializerOptions.AddContext<TestJsonContext>();
        }

        // Act
        await HttpResultsHelper.WriteResultAsJsonAsync(httpContext, NullLogger.Instance, value, jsonSerializerOptions: serializerOptions);

        // Assert
        var body = JsonSerializer.Deserialize<TodoStruct>(responseBodyStream.ToArray(), serializerOptions);

        Assert.Equal("Write even more tests!", body!.Name);
        Assert.True(body!.IsComplete);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WriteResultAsJsonAsync_Works_ForReferenceTypes(bool useJsonContext)
    {
        // Arrange
        var value = new Todo()
        {
            Id = 1,
            IsComplete = true,
            Name = "Write even more tests!",
        };
        var responseBodyStream = new MemoryStream();
        var httpContext = CreateHttpContext(responseBodyStream);
        var serializerOptions = new JsonOptions().SerializerOptions;

        if (useJsonContext)
        {
            serializerOptions.AddContext<TestJsonContext>();
        }

        // Act
        await HttpResultsHelper.WriteResultAsJsonAsync(httpContext, NullLogger.Instance, value, jsonSerializerOptions: serializerOptions);

        // Assert
        var body = JsonSerializer.Deserialize<Todo>(responseBodyStream.ToArray(), serializerOptions);

        Assert.NotNull(body);
        Assert.Equal("Write even more tests!", body!.Name);
        Assert.True(body!.IsComplete);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WriteResultAsJsonAsync_Works_ForChildTypes(bool useJsonContext)
    {
        // Arrange
        var value = new TodoChild()
        {
            Id = 1,
            IsComplete = true,
            Name = "Write even more tests!",
            Child = "With type hierarchies!"
        };
        var responseBodyStream = new MemoryStream();
        var httpContext = CreateHttpContext(responseBodyStream);
        var serializerOptions = new JsonOptions().SerializerOptions;

        if (useJsonContext)
        {
            serializerOptions.AddContext<TestJsonContext>();
        }

        // Act
        await HttpResultsHelper.WriteResultAsJsonAsync(httpContext, NullLogger.Instance, value, jsonSerializerOptions: serializerOptions);

        // Assert
        var body = JsonSerializer.Deserialize<TodoChild>(responseBodyStream.ToArray(), serializerOptions);

        Assert.NotNull(body);
        Assert.Equal("Write even more tests!", body!.Name);
        Assert.True(body!.IsComplete);
        Assert.Equal("With type hierarchies!", body!.Child);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WriteResultAsJsonAsync_Works_UsingBaseType_ForChildTypes(bool useJsonContext)
    {
        // Arrange
        var value = new TodoChild()
        {
            Id = 1,
            IsComplete = true,
            Name = "Write even more tests!",
            Child = "With type hierarchies!"
        };
        var responseBodyStream = new MemoryStream();
        var httpContext = CreateHttpContext(responseBodyStream);
        var serializerOptions = new JsonOptions().SerializerOptions;

        if (useJsonContext)
        {
            serializerOptions.AddContext<TestJsonContext>();
        }

        // Act
        await HttpResultsHelper.WriteResultAsJsonAsync<Todo>(httpContext, NullLogger.Instance, value, jsonSerializerOptions: serializerOptions);

        // Assert
        var body = JsonSerializer.Deserialize<TodoChild>(responseBodyStream.ToArray(), serializerOptions);

        Assert.NotNull(body);
        Assert.Equal("Write even more tests!", body!.Name);
        Assert.True(body!.IsComplete);
        Assert.Equal("With type hierarchies!", body!.Child);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WriteResultAsJsonAsync_Works_UsingBaseType_ForChildTypes_WithJsonPolymorphism(bool useJsonContext)
    {
        // Arrange
        var value = new TodoJsonChild()
        {
            Id = 1,
            IsComplete = true,
            Name = "Write even more tests!",
            Child = "With type hierarchies!"
        };
        var responseBodyStream = new MemoryStream();
        var httpContext = CreateHttpContext(responseBodyStream);
        var serializerOptions = new JsonOptions().SerializerOptions;

        if (useJsonContext)
        {
            serializerOptions.AddContext<TestJsonContext>();
        }

        // Act
        await HttpResultsHelper.WriteResultAsJsonAsync<JsonTodo>(httpContext, NullLogger.Instance, value, jsonSerializerOptions: serializerOptions);

        // Assert
        var body = JsonSerializer.Deserialize<TodoJsonChild>(responseBodyStream.ToArray(), serializerOptions);

        Assert.NotNull(body);
        Assert.Equal("Write even more tests!", body!.Name);
        Assert.True(body!.IsComplete);
        Assert.Equal("With type hierarchies!", body!.Child);
    }

    private static DefaultHttpContext CreateHttpContext(Stream stream)
        => new()
        {
            RequestServices = CreateServices(),
            Response =
            {
                Body = stream,
            },
        };

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        return services.BuildServiceProvider();
    }

    [JsonSerializable(typeof(Todo))]
    [JsonSerializable(typeof(TodoChild))]
    [JsonSerializable(typeof(JsonTodo))]
    [JsonSerializable(typeof(TodoStruct))]
    private partial class TestJsonContext : JsonSerializerContext
    { }

    private class Todo
    {
        public int Id { get; set; }
        public string Name { get; set; } = "Todo";
        public bool IsComplete { get; set; }
    }

    private struct TodoStruct
    {
        public TodoStruct()
        {
        }

        public int Id { get; set; }
        public string Name { get; set; } = "Todo";
        public bool IsComplete { get; set; }
    }

    private class TodoChild : Todo
    {
        public string Child { get; set; }
    }

    [JsonDerivedType(typeof(TodoJsonChild))]
    private class JsonTodo : Todo
    {
    }

    private class TodoJsonChild : JsonTodo
    {
        public string Child { get; set; }
    }
}
