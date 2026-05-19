// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Formatters;

public partial class SystemTextJsonOutputFormatterTest : JsonOutputFormatterTestBase
{
    protected override TextOutputFormatter GetOutputFormatter()
    {
        return SystemTextJsonOutputFormatter.CreateFormatter(new JsonOptions());
    }

    [Fact]
    public async Task WriteResponseBodyAsync_AllowsConfiguringPreserveReferenceHandling()
    {
        // Arrange
        var jsonOptions = new JsonOptions();
        jsonOptions.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;

        var formatter = SystemTextJsonOutputFormatter.CreateFormatter(jsonOptions);
        var expectedContent = "{\"$id\":\"1\",\"name\":\"Person\",\"child\":{\"$id\":\"2\",\"name\":\"Child\",\"child\":null,\"parent\":{\"$ref\":\"1\"}},\"parent\":null}";
        var person = new Person
        {
            Name = "Person",
            Child = new Person { Name = "Child", },
        };
        person.Child.Parent = person;

        var mediaType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
        var encoding = CreateOrGetSupportedEncoding(formatter, "utf-8", isDefaultEncoding: true);

        var body = new MemoryStream();
        var actionContext = GetActionContext(mediaType, body);

        var outputFormatterContext = new OutputFormatterWriteContext(
            actionContext.HttpContext,
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            typeof(Person),
            person)
        {
            ContentType = new StringSegment(mediaType.ToString()),
        };

        // Act
        await formatter.WriteResponseBodyAsync(outputFormatterContext, Encoding.GetEncoding("utf-8"));

        // Assert
        var actualContent = encoding.GetString(body.ToArray());
        Assert.Equal(expectedContent, actualContent);
    }

    [Fact]
    public async Task WriteResponseBodyAsync_WithNonUtf8Encoding_FormattingErrorsAreThrown()
    {
        // Arrange
        var formatter = GetOutputFormatter();
        var mediaType = MediaTypeHeaderValue.Parse("application/json; charset=utf-16");
        var encoding = CreateOrGetSupportedEncoding(formatter, "utf-16", isDefaultEncoding: true);

        var body = new MemoryStream();
        var actionContext = GetActionContext(mediaType, body);

        var outputFormatterContext = new OutputFormatterWriteContext(
            actionContext.HttpContext,
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            typeof(Person),
            new ThrowingFormatterModel())
        {
            ContentType = new StringSegment(mediaType.ToString()),
        };

        // Act & Assert
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => formatter.WriteResponseBodyAsync(outputFormatterContext, Encoding.GetEncoding("utf-16")));
    }

    [Fact]
    public async Task WriteResponseBodyAsync_ForLargeAsyncEnumerable()
    {
        // Arrange
        var expected = new MemoryStream();
        await JsonSerializer.SerializeAsync(expected, LargeAsync(), JsonSerializerOptions.Web);
        var formatter = GetOutputFormatter();
        var mediaType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
        var encoding = CreateOrGetSupportedEncoding(formatter, "utf-8", isDefaultEncoding: true);

        var body = new MemoryStream();
        var actionContext = GetActionContext(mediaType, body);

        var asyncEnumerable = LargeAsync();
        var outputFormatterContext = new OutputFormatterWriteContext(
            actionContext.HttpContext,
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            asyncEnumerable.GetType(),
            asyncEnumerable)
        {
            ContentType = new StringSegment(mediaType.ToString()),
        };

        // Act
        await formatter.WriteResponseBodyAsync(outputFormatterContext, Encoding.GetEncoding("utf-8"));

        // Assert
        Assert.Equal(expected.ToArray(), body.ToArray());
    }

    [Fact]
    public async Task WriteResponseBodyAsync_AsyncEnumerableConnectionCloses()
    {
        // Arrange
        var formatter = GetOutputFormatter();
        var mediaType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");

        var body = new MemoryStream();
        var actionContext = GetActionContext(mediaType, body);
        var cts = new CancellationTokenSource();
        actionContext.HttpContext.RequestAborted = cts.Token;

        var asyncEnumerable = AsyncEnumerableClosedConnection();
        var outputFormatterContext = new OutputFormatterWriteContext(
            actionContext.HttpContext,
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            asyncEnumerable.GetType(),
            asyncEnumerable)
        {
            ContentType = new StringSegment(mediaType.ToString()),
        };
        var iterated = false;

        // Act
        await formatter.WriteResponseBodyAsync(outputFormatterContext, Encoding.GetEncoding("utf-8"));

        // Assert
        // System.Text.Json might write the '[' before cancellation is observed
        Assert.InRange(body.ToArray().Length, 0, 1);
        Assert.False(iterated);

        async IAsyncEnumerable<int> AsyncEnumerableClosedConnection([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cts.Cancel();
            // MvcOptions.MaxIAsyncEnumerableBufferLimit is 8192. Pick some value larger than that.
            foreach (var i in Enumerable.Range(0, 9000))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
                iterated = true;
                yield return i;
            }
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WriteResponseBodyAsync_UsesJsonPolymorphismOptions(bool useJsonContext)
    {
        // Arrange
        var jsonOptions = new JsonOptions();

        if (useJsonContext)
        {
            jsonOptions.JsonSerializerOptions.TypeInfoResolver = TestJsonContext.Default;
        }

        var formatter = SystemTextJsonOutputFormatter.CreateFormatter(jsonOptions);
        var expectedContent = "{\"$type\":\"JsonPersonExtended\",\"age\":99,\"name\":\"Person\",\"child\":null,\"parent\":null}";
        JsonPerson person = new JsonPersonExtended()
        {
            Name = "Person",
            Age = 99,
        };

        var mediaType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
        var encoding = CreateOrGetSupportedEncoding(formatter, "utf-8", isDefaultEncoding: true);

        var body = new MemoryStream();
        var actionContext = GetActionContext(mediaType, body);

        var outputFormatterContext = new OutputFormatterWriteContext(
            actionContext.HttpContext,
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            typeof(JsonPerson),
            person)
        {
            ContentType = new StringSegment(mediaType.ToString()),
        };

        // Act
        await formatter.WriteResponseBodyAsync(outputFormatterContext, Encoding.GetEncoding("utf-8"));

        // Assert
        var actualContent = encoding.GetString(body.ToArray());
        Assert.Equal(expectedContent, actualContent);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WriteResponseBodyAsync_UsesJsonPolymorphismOptions_WithUnspeakableTypes(bool useJsonContext)
    {
        // Arrange
        var jsonOptions = new JsonOptions();

        if (useJsonContext)
        {
            jsonOptions.JsonSerializerOptions.TypeInfoResolver = TestJsonContext.Default;
        }

        var formatter = SystemTextJsonOutputFormatter.CreateFormatter(jsonOptions);
        var expectedContent = """[{"name":"One","child":null,"parent":null},{"$type":"JsonPersonExtended","age":99,"name":"Two","child":null,"parent":null}]""";
        var people = GetPeopleAsync();

        var mediaType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
        var encoding = CreateOrGetSupportedEncoding(formatter, "utf-8", isDefaultEncoding: true);

        var body = new MemoryStream();
        var actionContext = GetActionContext(mediaType, body);

        var outputFormatterContext = new OutputFormatterWriteContext(
            actionContext.HttpContext,
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            typeof(IAsyncEnumerable<JsonPerson>),
            people)
        {
            ContentType = new StringSegment(mediaType.ToString()),
        };

        // Act
        await formatter.WriteResponseBodyAsync(outputFormatterContext, Encoding.GetEncoding("utf-8"));

        // Assert
        var actualContent = encoding.GetString(body.ToArray());
        Assert.Equal(expectedContent, actualContent);
    }

    private static async IAsyncEnumerable<JsonPerson> GetPeopleAsync()
    {
        yield return new JsonPerson() { Name = "One" };

        // ensure this is async
        await Task.Yield();

        yield return new JsonPersonExtended() { Name = "Two", Age = 99 };
    }

    [Fact]
    public void WriteResponseBodyAsync_Works_WhenTypeResolverIsNull()
    {
        // Arrange
        var jsonOptions = new JsonOptions();
        jsonOptions.JsonSerializerOptions.TypeInfoResolver = null;

        var exception = Assert.Throws<InvalidOperationException>(() => SystemTextJsonOutputFormatter.CreateFormatter(jsonOptions));
        Assert.Equal("JsonSerializerOptions instance must specify a TypeInfoResolver setting before being marked as read-only.", exception.Message);
    }

    [ConditionalTheory]
    [RemoteExecutionSupported]
    [InlineData(false)]
    [InlineData(true)]
    public void STJOutputFormatter_UsesEmptyResolver_WhenJsonIsReflectionEnabledByDefaultFalse(bool isReflectionEnabledByDefault)
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", isReflectionEnabledByDefault.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            // Arrange
            var jsonOptions = new JsonOptions();

            // Assert
            var stjOutputFormatter = SystemTextJsonOutputFormatter.CreateFormatter(jsonOptions);
            Assert.IsAssignableFrom<IJsonTypeInfoResolver>(stjOutputFormatter.SerializerOptions.TypeInfoResolver);
            // Use default resolver if reflection is enabled instead of empty one
            if (JsonSerializer.IsReflectionEnabledByDefault)
            {
                Assert.IsType<DefaultJsonTypeInfoResolver>(stjOutputFormatter.SerializerOptions.TypeInfoResolver);
            }
        }, options);
    }

    private class Person
    {
        public string Name { get; set; }

        public Person Child { get; set; }

        public Person Parent { get; set; }
    }

    [JsonPolymorphic]
    [JsonDerivedType(typeof(JsonPersonExtended), nameof(JsonPersonExtended))]
    private class JsonPerson : Person
    { }

    private class JsonPersonExtended : JsonPerson
    {
        public int Age { get; set; }
    }

    [JsonSerializable(typeof(JsonPerson))]
    [JsonSerializable(typeof(IAsyncEnumerable<JsonPerson>))]
    private partial class TestJsonContext : JsonSerializerContext
    { }

    [JsonConverter(typeof(ThrowingFormatterPersonConverter))]
    private class ThrowingFormatterModel
    { }

    private class ThrowingFormatterPersonConverter : JsonConverter<ThrowingFormatterModel>
    {
        public override ThrowingFormatterModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, ThrowingFormatterModel value, JsonSerializerOptions options)
        {
            throw new TimeZoneNotFoundException();
        }
    }

    private static async IAsyncEnumerable<int> LargeAsync()
    {
        await Task.Yield();
        // MvcOptions.MaxIAsyncEnumerableBufferLimit is 8192. Pick some value larger than that.
        foreach (var i in Enumerable.Range(0, 9000))
        {
            yield return i;
        }
    }
}
