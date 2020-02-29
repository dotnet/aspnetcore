// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public class NewtonsoftJsonOutputFormatterTest : JsonOutputFormatterTestBase
    {
        protected override TextOutputFormatter GetOutputFormatter()
        {
            return new NewtonsoftJsonOutputFormatter(new JsonSerializerSettings(), ArrayPool<char>.Shared, new MvcOptions());
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
            var jsonFormatter = new NewtonsoftJsonOutputFormatter(settings, ArrayPool<char>.Shared, new MvcOptions());

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
            var formatter = new NewtonsoftJsonOutputFormatter(new JsonSerializerSettings(), ArrayPool<char>.Shared, new MvcOptions());
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
            stream.SetupGet(s => s.CanWrite).Returns(true);

            var formatter = new NewtonsoftJsonOutputFormatter(new JsonSerializerSettings(), ArrayPool<char>.Shared, new MvcOptions());
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
        }

        private class TestableJsonOutputFormatter : NewtonsoftJsonOutputFormatter
        {
            public TestableJsonOutputFormatter(JsonSerializerSettings serializerSettings)
                : base(serializerSettings, ArrayPool<char>.Shared, new MvcOptions())
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
}