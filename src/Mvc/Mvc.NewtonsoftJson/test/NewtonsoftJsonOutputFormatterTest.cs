// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.Mvc.Formatters;

public class NewtonsoftJsonOutputFormatterTest : JsonOutputFormatterTestBase
{
    protected override TextOutputFormatter GetOutputFormatter()
    {
        return new NewtonsoftJsonOutputFormatter(new JsonSerializerSettings(), ArrayPool<char>.Shared, new MvcOptions(), new MvcNewtonsoftJsonOptions());
    }

    [Fact]
    public void Creates_SerializerSettings_ByDefault()
    {
        // Arrange & Act
        var jsonFormatter = new TestableJsonOutputFormatter(new JsonSerializerSettings());

        // Assert
        Assert.NotNull(jsonFormatter.SerializerSettings);
    }

    [Fact]
    public void Constructor_UsesSerializerSettings()
    {
        // Arrange
        // Act
        var serializerSettings = new JsonSerializerSettings();
        var jsonFormatter = new TestableJsonOutputFormatter(serializerSettings);

        // Assert
        Assert.Same(serializerSettings, jsonFormatter.SerializerSettings);
    }

    [Fact]
    public async Task MvcJsonOptionsAreUsedToSetBufferThresholdFromServices()
    {
        // Arrange
        var person = new User() { FullName = "John", age = 35 };
        Stream writeStream = null;
        var outputFormatterContext = GetOutputFormatterContext(person, typeof(User), writerFactory: (stream, encoding) =>
        {
            writeStream = stream;
            return StreamWriter.Null;
        });

        var services = new ServiceCollection()
                .AddOptions()
                .Configure<MvcNewtonsoftJsonOptions>(o =>
                {
                    o.OutputFormatterMemoryBufferThreshold = 1;
                })
                .BuildServiceProvider();

        outputFormatterContext.HttpContext.RequestServices = services;

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
        };
        var expectedOutput = JsonConvert.SerializeObject(person, settings);
#pragma warning disable CS0618 // Type or member is obsolete
        var jsonFormatter = new NewtonsoftJsonOutputFormatter(settings, ArrayPool<char>.Shared, new MvcOptions());
#pragma warning restore CS0618 // Type or member is obsolete

        // Act
        await jsonFormatter.WriteResponseBodyAsync(outputFormatterContext, Encoding.UTF8);

        // Assert
        Assert.IsType<FileBufferingWriteStream>(writeStream);

        Assert.Equal(1, ((FileBufferingWriteStream)writeStream).MemoryThreshold);
    }

    [Fact]
    public async Task MvcJsonOptionsAreUsedToSetBufferThreshold()
    {
        // Arrange
        var person = new User() { FullName = "John", age = 35 };
        Stream writeStream = null;
        var outputFormatterContext = GetOutputFormatterContext(person, typeof(User), writerFactory: (stream, encoding) =>
        {
            writeStream = stream;
            return StreamWriter.Null;
        });

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
        };
        var expectedOutput = JsonConvert.SerializeObject(person, settings);
        var jsonFormatter = new NewtonsoftJsonOutputFormatter(settings, ArrayPool<char>.Shared, new MvcOptions(), new MvcNewtonsoftJsonOptions()
        {
            OutputFormatterMemoryBufferThreshold = 2
        });

        // Act
        await jsonFormatter.WriteResponseBodyAsync(outputFormatterContext, Encoding.UTF8);

        // Assert
        Assert.IsType<FileBufferingWriteStream>(writeStream);

        Assert.Equal(2, ((FileBufferingWriteStream)writeStream).MemoryThreshold);
    }

    [Fact]
    public async Task ChangesTo_SerializerSettings_AffectSerialization()
    {
        // Arrange
        var person = new User() { FullName = "John", age = 35 };
        var outputFormatterContext = GetOutputFormatterContext(person, typeof(User));

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
        };
        var expectedOutput = JsonConvert.SerializeObject(person, settings);
        var jsonFormatter = new NewtonsoftJsonOutputFormatter(settings, ArrayPool<char>.Shared, new MvcOptions(), new MvcNewtonsoftJsonOptions());

        // Act
        await jsonFormatter.WriteResponseBodyAsync(outputFormatterContext, Encoding.UTF8);

        // Assert
        var body = outputFormatterContext.HttpContext.Response.Body;

        Assert.NotNull(body);
        body.Position = 0;

        var content = new StreamReader(body, Encoding.UTF8).ReadToEnd();
        Assert.Equal(expectedOutput, content);
    }

    [Fact]
    public async Task ChangesTo_SerializerSettings_AfterSerialization_DoNotAffectSerialization()
    {
        // Arrange
        var person = new User() { FullName = "John", age = 35 };
        var expectedOutput = JsonConvert.SerializeObject(person, new JsonSerializerSettings());

        var jsonFormatter = new TestableJsonOutputFormatter(new JsonSerializerSettings());

        // This will create a serializer - which gets cached.
        var outputFormatterContext1 = GetOutputFormatterContext(person, typeof(User));
        await jsonFormatter.WriteResponseBodyAsync(outputFormatterContext1, Encoding.UTF8);

        // These changes should have no effect.
        jsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        jsonFormatter.SerializerSettings.Formatting = Formatting.Indented;

        var outputFormatterContext2 = GetOutputFormatterContext(person, typeof(User));

        // Act
        await jsonFormatter.WriteResponseBodyAsync(outputFormatterContext2, Encoding.UTF8);

        // Assert
        var body = outputFormatterContext2.HttpContext.Response.Body;

        Assert.NotNull(body);
        body.Position = 0;

        var content = new StreamReader(body, Encoding.UTF8).ReadToEnd();
        Assert.Equal(expectedOutput, content);
    }

    public static TheoryData<NamingStrategy, string> NamingStrategy_AffectsSerializationData
    {
        get
        {
            return new TheoryData<NamingStrategy, string>
                {
                    { new CamelCaseNamingStrategy(), "{\"fullName\":\"John\",\"age\":35}" },
                    { new DefaultNamingStrategy(), "{\"FullName\":\"John\",\"age\":35}" },
                    { new SnakeCaseNamingStrategy(), "{\"full_name\":\"John\",\"age\":35}" },
                };
        }
    }

    [Theory]
    [MemberData(nameof(NamingStrategy_AffectsSerializationData))]
    public async Task NamingStrategy_AffectsSerialization(NamingStrategy strategy, string expected)
    {
        // Arrange
        var user = new User { FullName = "John", age = 35 };
        var context = GetOutputFormatterContext(user, typeof(User));

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = strategy,
            },
        };
        var formatter = new TestableJsonOutputFormatter(settings);

        // Act
        await formatter.WriteResponseBodyAsync(context, Encoding.UTF8);

        // Assert
        var body = context.HttpContext.Response.Body;

        Assert.NotNull(body);
        body.Position = 0;

        var content = new StreamReader(body, Encoding.UTF8).ReadToEnd();
        Assert.Equal(expected, content);
    }

    public static TheoryData<NamingStrategy> NamingStrategy_DoesNotAffectSerializationData
    {
        get
        {
            return new TheoryData<NamingStrategy>
                {
                    { new CamelCaseNamingStrategy() },
                    { new DefaultNamingStrategy() },
                    { new SnakeCaseNamingStrategy() },
                };
        }
    }

    [Theory]
    [MemberData(nameof(NamingStrategy_DoesNotAffectSerializationData))]
    public async Task NamingStrategy_DoesNotAffectDictionarySerialization(NamingStrategy strategy)
    {
        // Arrange
        var dictionary = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "id", 12 },
                { "Id", 12 },
                { "fullName", 12 },
                { "full-name", 12 },
                { "FullName", 12 },
                { "full_Name", 12 },
            };
        var expected = "{\"id\":12,\"Id\":12,\"fullName\":12,\"full-name\":12,\"FullName\":12,\"full_Name\":12}";
        var context = GetOutputFormatterContext(dictionary, typeof(Dictionary<string, int>));

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = strategy,
            },
        };
        var formatter = new TestableJsonOutputFormatter(settings);

        // Act
        await formatter.WriteResponseBodyAsync(context, Encoding.UTF8);

        // Assert
        var body = context.HttpContext.Response.Body;

        Assert.NotNull(body);
        body.Position = 0;

        var content = new StreamReader(body, Encoding.UTF8).ReadToEnd();
        Assert.Equal(expected, content);
    }

    [Theory]
    [MemberData(nameof(NamingStrategy_DoesNotAffectSerializationData))]
    public async Task NamingStrategy_DoesNotAffectSerialization_WithJsonProperty(NamingStrategy strategy)
    {
        // Arrange
        var user = new UserWithJsonProperty
        {
            Name = "Joe",
            AnotherName = "Joe",
            ThirdName = "Joe",
        };
        var expected = "{\"ThisIsTheFullName\":\"Joe\",\"another_name\":\"Joe\",\"ThisIsTheThirdName\":\"Joe\"}";
        var context = GetOutputFormatterContext(user, typeof(UserWithJsonProperty));

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = strategy,
            },
        };
        var formatter = new TestableJsonOutputFormatter(settings);

        // Act
        await formatter.WriteResponseBodyAsync(context, Encoding.UTF8);

        // Assert
        var body = context.HttpContext.Response.Body;

        Assert.NotNull(body);
        body.Position = 0;

        var content = new StreamReader(body, Encoding.UTF8).ReadToEnd();
        Assert.Equal(expected, content);
    }

    [Theory]
    [MemberData(nameof(NamingStrategy_DoesNotAffectSerializationData))]
    public async Task NamingStrategy_DoesNotAffectSerialization_WithJsonObject(NamingStrategy strategy)
    {
        // Arrange
        var user = new UserWithJsonObject
        {
            age = 35,
            FullName = "John",
        };
        var expected = "{\"age\":35,\"full_name\":\"John\"}";
        var context = GetOutputFormatterContext(user, typeof(UserWithJsonProperty));

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = strategy,
            },
        };
        var formatter = new TestableJsonOutputFormatter(settings);

        // Act
        await formatter.WriteResponseBodyAsync(context, Encoding.UTF8);

        // Assert
        var body = context.HttpContext.Response.Body;

        Assert.NotNull(body);
        body.Position = 0;

        var content = new StreamReader(body, Encoding.UTF8).ReadToEnd();
        Assert.Equal(expected, content);
    }

    [Fact]
    public async Task WriteToStreamAsync_RoundTripsJToken()
    {
        // Arrange
        var beforeMessage = "Hello World";
        var formatter = new NewtonsoftJsonOutputFormatter(new JsonSerializerSettings(), ArrayPool<char>.Shared, new MvcOptions(), new MvcNewtonsoftJsonOptions());
        var memStream = new MemoryStream();
        var outputFormatterContext = GetOutputFormatterContext(
            beforeMessage,
            typeof(string),
            "application/json; charset=utf-8",
            memStream);

        // Act
        await formatter.WriteResponseBodyAsync(outputFormatterContext, Encoding.UTF8);

        // Assert
        memStream.Position = 0;
        var after = JToken.Load(new JsonTextReader(new StreamReader(memStream)));
        var afterMessage = after.ToObject<string>();

        Assert.Equal(beforeMessage, afterMessage);
    }

    [Fact]
    public async Task WriteToStreamAsync_LargePayload_DoesNotPerformSynchronousWrites()
    {
        // Arrange
        var model = Enumerable.Range(0, 1000).Select(p => new User { FullName = new string('a', 5000) });

        var stream = new Mock<Stream> { CallBase = true };
        stream.Setup(v => v.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        stream.Setup(v => v.FlushAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        stream.SetupGet(s => s.CanWrite).Returns(true);

        var formatter = new NewtonsoftJsonOutputFormatter(new JsonSerializerSettings(), ArrayPool<char>.Shared, new MvcOptions(), new MvcNewtonsoftJsonOptions());
        var outputFormatterContext = GetOutputFormatterContext(
            model,
            typeof(string),
            "application/json; charset=utf-8",
            stream.Object);

        // Act
        await formatter.WriteResponseBodyAsync(outputFormatterContext, Encoding.UTF8);

        // Assert
        stream.Verify(v => v.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce());

        stream.Verify(v => v.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never());
        stream.Verify(v => v.Flush(), Times.Never());
        Assert.NotNull(outputFormatterContext.HttpContext.Response.ContentLength);
    }

    [Fact]
    public async Task SerializingWithPreserveReferenceHandling()
    {
        // Arrange
        var expected = "{\"$id\":\"1\",\"fullName\":\"John\",\"age\":35}";
        var user = new User { FullName = "John", age = 35 };

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
            },
            PreserveReferencesHandling = PreserveReferencesHandling.All,
        };
        var formatter = new TestableJsonOutputFormatter(settings);

        for (var i = 0; i < 3; i++)
        {
            // Act
            var context = GetOutputFormatterContext(user, typeof(User));
            await formatter.WriteResponseBodyAsync(context, Encoding.UTF8);

            // Assert
            var body = context.HttpContext.Response.Body;

            Assert.NotNull(body);
            body.Position = 0;

            var content = new StreamReader(body, Encoding.UTF8).ReadToEnd();
            Assert.Equal(expected, content);
        }
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
        actionContext.HttpContext.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();

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
        Assert.Empty(body.ToArray());
        Assert.False(iterated);

        async IAsyncEnumerable<int> AsyncEnumerableClosedConnection([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cts.Cancel();
            // MvcOptions.MaxIAsyncEnumerableBufferLimit is 8192. Pick some value larger than that.
            foreach (var i in Enumerable.Range(0, 9000))
            {
                cancellationToken.ThrowIfCancellationRequested();
                iterated = true;
                yield return i;
            }
        }
    }

    [Fact]
    public async Task WriteResponseBodyAsync_AsyncEnumerableThrowsCustomOCE()
    {
        // Arrange
        var formatter = GetOutputFormatter();
        var mediaType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");

        var body = new MemoryStream();
        var actionContext = GetActionContext(mediaType, body);
        var cts = new CancellationTokenSource();
        actionContext.HttpContext.RequestAborted = cts.Token;
        actionContext.HttpContext.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();

        var asyncEnumerable = AsyncEnumerableThrows();
        var outputFormatterContext = new OutputFormatterWriteContext(
            actionContext.HttpContext,
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            asyncEnumerable.GetType(),
            asyncEnumerable)
        {
            ContentType = new StringSegment(mediaType.ToString()),
        };

        // Act
        await Assert.ThrowsAsync<OperationCanceledException>(() => formatter.WriteResponseBodyAsync(outputFormatterContext, Encoding.GetEncoding("utf-8")));

        async IAsyncEnumerable<int> AsyncEnumerableThrows([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return 1;
            throw new OperationCanceledException();
        }
    }

    [Fact]
    public async Task WriteResponseBodyAsync_AsyncEnumerableThrowsConnectionAbortedOCE()
    {
        // Arrange
        var formatter = GetOutputFormatter();
        var mediaType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");

        var body = new MemoryStream();
        var actionContext = GetActionContext(mediaType, body);
        var cts = new CancellationTokenSource();
        actionContext.HttpContext.RequestAborted = cts.Token;
        actionContext.HttpContext.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();

        var asyncEnumerable = AsyncEnumerableThrows();
        var outputFormatterContext = new OutputFormatterWriteContext(
            actionContext.HttpContext,
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            asyncEnumerable.GetType(),
            asyncEnumerable)
        {
            ContentType = new StringSegment(mediaType.ToString()),
        };

        // Act
        // Act
        await formatter.WriteResponseBodyAsync(outputFormatterContext, Encoding.GetEncoding("utf-8"));

        // Assert
        Assert.Empty(body.ToArray());

        async IAsyncEnumerable<int> AsyncEnumerableThrows([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cts.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            yield return 1;
        }
    }

    private class TestableJsonOutputFormatter : NewtonsoftJsonOutputFormatter
    {
        public TestableJsonOutputFormatter(JsonSerializerSettings serializerSettings)
            : base(serializerSettings, ArrayPool<char>.Shared, new MvcOptions(), new MvcNewtonsoftJsonOptions())
        {
        }

        public new JsonSerializerSettings SerializerSettings => base.SerializerSettings;
    }

    private sealed class User
    {
        public string FullName { get; set; }

        public int age { get; set; }
    }

    private class UserWithJsonProperty
    {
        [JsonProperty("ThisIsTheFullName")]
        public string Name { get; set; }

        [JsonProperty(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
        public string AnotherName { get; set; }

        // NamingStrategyType should be ignored with an explicit name.
        [JsonProperty("ThisIsTheThirdName", NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
        public string ThirdName { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    private class UserWithJsonObject
    {
        public int age { get; set; }

        public string FullName { get; set; }
    }
}
