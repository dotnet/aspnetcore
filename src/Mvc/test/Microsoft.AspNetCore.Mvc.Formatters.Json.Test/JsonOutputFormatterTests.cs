// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public class JsonOutputFormatterTests
    {
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
            var jsonFormatter = new JsonOutputFormatter(settings, ArrayPool<char>.Shared);

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
            var formatter = new JsonOutputFormatter(new JsonSerializerSettings(), ArrayPool<char>.Shared);
            var before = new JValue(beforeMessage);
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

        public static TheoryData<string, string, bool> WriteCorrectCharacterEncoding
        {
            get
            {
                var data = new TheoryData<string, string, bool>
                {
                    { "This is a test 激光這兩個字是甚麼意思 string written using utf-8", "utf-8", true },
                    { "This is a test 激光這兩個字是甚麼意思 string written using utf-16", "utf-16", true },
                    { "This is a test 激光這兩個字是甚麼意思 string written using utf-32", "utf-32", false },
                    { "This is a test æøå string written using iso-8859-1", "iso-8859-1", false },
                };

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(WriteCorrectCharacterEncoding))]
        public async Task WriteToStreamAsync_UsesCorrectCharacterEncoding(
            string content,
            string encodingAsString,
            bool isDefaultEncoding)
        {
            // Arrange
            var formatter = new JsonOutputFormatter(new JsonSerializerSettings(), ArrayPool<char>.Shared);
            var formattedContent = "\"" + content + "\"";
            var mediaType = MediaTypeHeaderValue.Parse(string.Format("application/json; charset={0}", encodingAsString));
            var encoding = CreateOrGetSupportedEncoding(formatter, encodingAsString, isDefaultEncoding);
            var expectedData = encoding.GetBytes(formattedContent);


            var body = new MemoryStream();
            var actionContext = GetActionContext(mediaType, body);

            var outputFormatterContext = new OutputFormatterWriteContext(
                actionContext.HttpContext,
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                typeof(string),
                content)
            {
                ContentType = new StringSegment(mediaType.ToString()),
            };

            // Act
            await formatter.WriteResponseBodyAsync(outputFormatterContext, Encoding.GetEncoding(encodingAsString));

            // Assert
            var actualData = body.ToArray();
            Assert.Equal(expectedData, actualData);
        }

        [Fact]
        public async Task ErrorDuringSerialization_DoesNotCloseTheBrackets()
        {
            // Arrange
            var expectedOutput = "{\"name\":\"Robert\"";
            var outputFormatterContext = GetOutputFormatterContext(
                new ModelWithSerializationError(),
                typeof(ModelWithSerializationError));
            var serializerSettings = JsonSerializerSettingsProvider.CreateSerializerSettings();
            var jsonFormatter = new JsonOutputFormatter(serializerSettings, ArrayPool<char>.Shared);

            // Act
            try
            {
                await jsonFormatter.WriteResponseBodyAsync(outputFormatterContext, Encoding.UTF8);
            }
            catch (JsonSerializationException serializerException)
            {
                var expectedException = Assert.IsType<NotImplementedException>(serializerException.InnerException);
                Assert.Equal("Property Age has not been implemented", expectedException.Message);
            }

            // Assert
            var body = outputFormatterContext.HttpContext.Response.Body;

            Assert.NotNull(body);
            body.Position = 0;

            var content = new StreamReader(body, Encoding.UTF8).ReadToEnd();
            Assert.Equal(expectedOutput, content);
        }

        [Theory]
        [InlineData("application/json", false, "application/json")]
        [InlineData("application/json", true, "application/json")]
        [InlineData("application/xml", false, null)]
        [InlineData("application/xml", true, null)]
        [InlineData("application/*", false, "application/json")]
        [InlineData("text/*", false, "text/json")]
        [InlineData("custom/*", false, null)]
        [InlineData("application/json;v=2", false, null)]
        [InlineData("application/json;v=2", true, null)]
        [InlineData("application/some.entity+json", false, null)]
        [InlineData("application/some.entity+json", true, "application/some.entity+json")]
        [InlineData("application/some.entity+json;v=2", true, "application/some.entity+json;v=2")]
        [InlineData("application/some.entity+xml", true, null)]
        public void CanWriteResult_ReturnsExpectedValueForMediaType(
            string mediaType,
            bool isServerDefined,
            string expectedResult)
        {
            // Arrange
            var formatter = new JsonOutputFormatter(new JsonSerializerSettings(), ArrayPool<char>.Shared);
            
            var body = new MemoryStream();
            var actionContext = GetActionContext(MediaTypeHeaderValue.Parse(mediaType), body);
            var outputFormatterContext = new OutputFormatterWriteContext(
                actionContext.HttpContext,
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                typeof(string),
                new object())
            {
                ContentType = new StringSegment(mediaType),
                ContentTypeIsServerDefined = isServerDefined,
            };

            // Act
            var actualCanWriteValue = formatter.CanWriteResult(outputFormatterContext);

            // Assert
            var expectedContentType = expectedResult ?? mediaType;
            Assert.Equal(expectedResult != null, actualCanWriteValue);
            Assert.Equal(new StringSegment(expectedContentType), outputFormatterContext.ContentType);
        }

        private static Encoding CreateOrGetSupportedEncoding(
            JsonOutputFormatter formatter,
            string encodingAsString,
            bool isDefaultEncoding)
        {
            Encoding encoding = null;
            if (isDefaultEncoding)
            {
                encoding = formatter.SupportedEncodings
                               .First((e) => e.WebName.Equals(encodingAsString, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                encoding = Encoding.GetEncoding(encodingAsString);
                formatter.SupportedEncodings.Add(encoding);
            }

            return encoding;
        }

        private static ILogger GetLogger()
        {
            return NullLogger.Instance;
        }

        private static OutputFormatterWriteContext GetOutputFormatterContext(
            object outputValue,
            Type outputType,
            string contentType = "application/xml; charset=utf-8",
            MemoryStream responseStream = null)
        {
            var mediaTypeHeaderValue = MediaTypeHeaderValue.Parse(contentType);

            var actionContext = GetActionContext(mediaTypeHeaderValue, responseStream);
            return new OutputFormatterWriteContext(
                actionContext.HttpContext,
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                outputType,
                outputValue)
            {
                ContentType = new StringSegment(contentType),
            };
        }

        private static ActionContext GetActionContext(
            MediaTypeHeaderValue contentType,
            MemoryStream responseStream = null)
        {
            var request = new Mock<HttpRequest>();
            var headers = new HeaderDictionary();
            request.Setup(r => r.ContentType).Returns(contentType.ToString());
            request.SetupGet(r => r.Headers).Returns(headers);
            headers[HeaderNames.AcceptCharset] = contentType.Charset.ToString();
            var response = new Mock<HttpResponse>();
            response.SetupGet(f => f.Body).Returns(responseStream ?? new MemoryStream());
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Response).Returns(response.Object);
            return new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());
        }

        private class TestableJsonOutputFormatter : JsonOutputFormatter
        {
            public TestableJsonOutputFormatter(JsonSerializerSettings serializerSettings)
                : base(serializerSettings, ArrayPool<char>.Shared)
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

        private class ModelWithSerializationError
        {
            public string Name { get; } = "Robert";
            public int Age
            {
                get
                {
                    throw new NotImplementedException($"Property {nameof(Age)} has not been implemented");
                }
            }
        }
    }
}