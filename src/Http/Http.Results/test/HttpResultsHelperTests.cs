// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
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
        var httpContext = CreateHttpContext(responseBodyStream, useJsonContext);

        // Act
        await HttpResultsHelper.WriteResultAsJsonAsync(httpContext, NullLogger.Instance, value);

        // Assert
        var body = JsonSerializer.Deserialize<TodoStruct>(responseBodyStream.ToArray(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

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
        var httpContext = CreateHttpContext(responseBodyStream, useJsonContext);

        // Act
        await HttpResultsHelper.WriteResultAsJsonAsync(httpContext, NullLogger.Instance, value);

        // Assert
        var body = JsonSerializer.Deserialize<Todo>(responseBodyStream.ToArray(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

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
        var httpContext = CreateHttpContext(responseBodyStream, useJsonContext);

        // Act
        await HttpResultsHelper.WriteResultAsJsonAsync(httpContext, NullLogger.Instance, value);

        // Assert
        var body = JsonSerializer.Deserialize<TodoChild>(responseBodyStream.ToArray(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(body);
        Assert.Equal("Write even more tests!", body!.Name);
        Assert.True(body!.IsComplete);
        Assert.Equal("With type hierarchies!", body!.Child);
    }

    private static DefaultHttpContext CreateHttpContext(Stream stream, bool useJsonContext = false)
        => new()
        {
            RequestServices = CreateServices(useJsonContext),
            Response =
            {
                Body = stream,
            },
        };

    private static IServiceProvider CreateServices(bool useJsonContext = false)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();

        if (useJsonContext)
        {
            services.ConfigureHttpJsonOptions(o => o.SerializerOptions.AddContext<TestJsonContext>());
        }

        return services.BuildServiceProvider();
    }

    [JsonSerializable(typeof(Todo))]
    [JsonSerializable(typeof(TodoChild))]
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
}
