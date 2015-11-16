// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Testing;
using Microsoft.Net.Http.Headers;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNet.Mvc.Formatters
{
    public class JsonOutputFormatterTests
    {
        [Fact]
        public void Creates_SerializerSettings_ByDefault()
        {
            // Arrange
            // Act
            var jsonFormatter = new JsonOutputFormatter();

            // Assert
            Assert.NotNull(jsonFormatter.SerializerSettings);
        }

        [Fact]
        public void Constructor_UsesSerializerSettings()
        {
            // Arrange
            // Act
            var serializerSettings = new JsonSerializerSettings();
            var jsonFormatter = new JsonInputFormatter(serializerSettings);

            // Assert
            Assert.Same(serializerSettings, jsonFormatter.SerializerSettings);
        }

        [Fact]
        public async Task ChangesTo_DefaultSerializerSettings_TakesEffect()
        {
            // Arrange
            var person = new User() { Name = "John", Age = 35 };
            var expectedOutput = JsonConvert.SerializeObject(person, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            });

            var jsonFormatter = new JsonOutputFormatter();
            jsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            jsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
            var outputFormatterContext = GetOutputFormatterContext(person, typeof(User));

            // Act
            await jsonFormatter.WriteResponseBodyAsync(outputFormatterContext);

            // Assert
            var body = outputFormatterContext.HttpContext.Response.Body;

            Assert.NotNull(body);
            body.Position = 0;

            var content = new StreamReader(body, Encoding.UTF8).ReadToEnd();
            Assert.Equal(expectedOutput, content);
        }

        [Fact]
        public async Task ChangesTo_DefaultSerializerSettings_AfterSerialization_NoEffect()
        {
            // Arrange
            var person = new User() { Name = "John", Age = 35 };
            var expectedOutput = JsonConvert.SerializeObject(
                person,
                SerializerSettingsProvider.CreateSerializerSettings());

            var jsonFormatter = new JsonOutputFormatter();

            // This will create a serializer - which gets cached.
            var outputFormatterContext1 = GetOutputFormatterContext(person, typeof(User));
            await jsonFormatter.WriteResponseBodyAsync(outputFormatterContext1);

            // These changes should have no effect.
            jsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            jsonFormatter.SerializerSettings.Formatting = Formatting.Indented;

            var outputFormatterContext2 = GetOutputFormatterContext(person, typeof(User));

            // Act
            await jsonFormatter.WriteResponseBodyAsync(outputFormatterContext2);

            // Assert
            var body = outputFormatterContext2.HttpContext.Response.Body;

            Assert.NotNull(body);
            body.Position = 0;

            var content = new StreamReader(body, Encoding.UTF8).ReadToEnd();
            Assert.Equal(expectedOutput, content);
        }

        [Fact]
        public async Task ReplaceSerializerSettings_AfterSerialization_TakesEffect()
        {
            // Arrange
            var person = new User() { Name = "John", Age = 35 };
            var expectedOutput = JsonConvert.SerializeObject(person, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            });

            var jsonFormatter = new JsonOutputFormatter();

            // This will create a serializer - which gets cached.
            var outputFormatterContext1 = GetOutputFormatterContext(person, typeof(User));
            await jsonFormatter.WriteResponseBodyAsync(outputFormatterContext1);

            // This results in a new serializer being created.
            jsonFormatter.SerializerSettings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented,
            };

            var outputFormatterContext2 = GetOutputFormatterContext(person, typeof(User));

            // Act
            await jsonFormatter.WriteResponseBodyAsync(outputFormatterContext2);

            // Assert
            var body = outputFormatterContext2.HttpContext.Response.Body;

            Assert.NotNull(body);
            body.Position = 0;

            var content = new StreamReader(body, Encoding.UTF8).ReadToEnd();
            Assert.Equal(expectedOutput, content);
        }

        [Fact]
        public async Task CustomSerializerSettingsObject_TakesEffect()
        {
            // Arrange
            var person = new User() { Name = "John", Age = 35 };
            var expectedOutput = JsonConvert.SerializeObject(person, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            });

            var jsonFormatter = new JsonOutputFormatter();
            jsonFormatter.SerializerSettings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            };

            var outputFormatterContext = GetOutputFormatterContext(person, typeof(User));

            // Act
            await jsonFormatter.WriteResponseBodyAsync(outputFormatterContext);

            // Assert
            var body = outputFormatterContext.HttpContext.Response.Body;

            Assert.NotNull(body);
            body.Position = 0;

            var content = new StreamReader(body, Encoding.UTF8).ReadToEnd();
            Assert.Equal(expectedOutput, content);
        }

        [Fact]
        public async Task WriteToStreamAsync_RoundTripsJToken()
        {
            // Arrange
            var beforeMessage = "Hello World";
            var formatter = new JsonOutputFormatter();
            var before = new JValue(beforeMessage);
            var memStream = new MemoryStream();
            var outputFormatterContext = GetOutputFormatterContext(
                beforeMessage,
                typeof(string),
                "application/json; charset=utf-8",
                memStream);

            // Act
            await formatter.WriteResponseBodyAsync(outputFormatterContext);

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
                    { "This is a test 激光這兩個字是甚麼意思 string written using shift_jis", "shift_jis", false },
                    { "This is a test æøå string written using iso-8859-1", "iso-8859-1", false },
                };

                if (!TestPlatformHelper.IsMono)
                {
                    // Mono issue - https://github.com/aspnet/External/issues/28
                    data.Add("This is a test 레이저 단어 뜻 string written using iso-2022-kr", "iso-2022-kr", false);
                }

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
            var formatter = new JsonOutputFormatter();
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
                ContentType = mediaType,
            };

            // Act
            await formatter.WriteResponseBodyAsync(outputFormatterContext);

            // Assert
            var actualData = body.ToArray();
            Assert.Equal(expectedData, actualData);
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
                ContentType = mediaTypeHeaderValue,
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
            headers[HeaderNames.AcceptCharset] = contentType.Charset;
            var response = new Mock<HttpResponse>();
            response.SetupGet(f => f.Body).Returns(responseStream ?? new MemoryStream());
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Response).Returns(response.Object);
            return new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());
        }

        private sealed class User
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }
    }
}