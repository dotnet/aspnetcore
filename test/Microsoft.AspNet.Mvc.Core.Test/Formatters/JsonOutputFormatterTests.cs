// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core.Collections;
using Microsoft.Net.Http.Headers;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test.Formatters
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
            Assert.NotNull(outputFormatterContext.ActionContext.HttpContext.Response.Body);
            outputFormatterContext.ActionContext.HttpContext.Response.Body.Position = 0;
            Assert.Equal(expectedOutput,
                new StreamReader(outputFormatterContext.ActionContext.HttpContext.Response.Body, Encoding.UTF8)
                        .ReadToEnd());
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
            Assert.NotNull(outputFormatterContext.ActionContext.HttpContext.Response.Body);
            outputFormatterContext.ActionContext.HttpContext.Response.Body.Position = 0;

            var streamReader = new StreamReader(outputFormatterContext.ActionContext.HttpContext.Response.Body, Encoding.UTF8);
            Assert.Equal(expectedOutput, streamReader.ReadToEnd());
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
                return new TheoryData<string, string, bool>
                {
                    { "This is a test 激光這兩個字是甚麼意思 string written using utf-8", "utf-8", true },
                    { "This is a test 激光這兩個字是甚麼意思 string written using utf-16", "utf-16", true },
                    { "This is a test 激光這兩個字是甚麼意思 string written using utf-32", "utf-32", false },
                    { "This is a test 激光這兩個字是甚麼意思 string written using shift_jis", "shift_jis", false },
                    { "This is a test æøå string written using iso-8859-1", "iso-8859-1", false },
                    { "This is a test 레이저 단어 뜻 string written using iso-2022-kr", "iso-2022-kr", false },
                };
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
            var mediaType = string.Format("application/json; charset={0}", encodingAsString);
            var encoding = CreateOrGetSupportedEncoding(formatter, encodingAsString, isDefaultEncoding);
            var preamble = encoding.GetPreamble();
            var data = encoding.GetBytes(formattedContent);
            var expectedData = new byte[preamble.Length + data.Length];
            Buffer.BlockCopy(preamble, 0, expectedData, 0, preamble.Length);
            Buffer.BlockCopy(data, 0, expectedData, preamble.Length, data.Length);

            var memStream = new MemoryStream();
            var outputFormatterContext = new OutputFormatterContext
            {
                Object = content,
                DeclaredType = typeof(string),
                ActionContext = GetActionContext(MediaTypeHeaderValue.Parse(mediaType), memStream),
                SelectedEncoding = encoding
            };

            // Act
            await formatter.WriteResponseBodyAsync(outputFormatterContext);

            // Assert
            var actualData = memStream.ToArray();
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

        private static OutputFormatterContext GetOutputFormatterContext(
            object outputValue,
            Type outputType,
            string contentType = "application/xml; charset=utf-8",
            MemoryStream responseStream = null)
        {
            var mediaTypeHeaderValue = MediaTypeHeaderValue.Parse(contentType);

            return new OutputFormatterContext
            {
                Object = outputValue,
                DeclaredType = outputType,
                ActionContext = GetActionContext(mediaTypeHeaderValue, responseStream),
                SelectedEncoding = Encoding.GetEncoding(mediaTypeHeaderValue.Charset)
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
            return new ActionContext(httpContext.Object, routeData: null, actionDescriptor: null);
        }

        private sealed class User
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }
    }
}