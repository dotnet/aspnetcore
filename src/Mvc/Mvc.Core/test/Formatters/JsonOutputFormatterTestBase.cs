// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public abstract class JsonOutputFormatterTestBase
    {
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
            var formatter = GetOutputFormatter();

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

        [Fact]
        public async Task ErrorDuringSerialization_DoesNotCloseTheBrackets()
        {
            // Arrange
            var outputFormatterContext = GetOutputFormatterContext(
                new ModelWithSerializationError(),
                typeof(ModelWithSerializationError));
            var jsonFormatter = GetOutputFormatter();

            // Act
            await Record.ExceptionAsync(() => jsonFormatter.WriteResponseBodyAsync(outputFormatterContext, Encoding.UTF8));

            // Assert
            var body = outputFormatterContext.HttpContext.Response.Body;

            Assert.NotNull(body);
            body.Position = 0;

            var content = new StreamReader(body, Encoding.UTF8).ReadToEnd();
            Assert.DoesNotContain("}", content);
        }

        protected static ActionContext GetActionContext(
            MediaTypeHeaderValue contentType,
            MemoryStream responseStream = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = contentType.ToString();
            httpContext.Request.Headers[HeaderNames.AcceptCharset] = contentType.Charset.ToString();


            httpContext.Response.Body = responseStream ?? new MemoryStream();
            return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        }

        protected static OutputFormatterWriteContext GetOutputFormatterContext(
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

        protected static Encoding CreateOrGetSupportedEncoding(
            TextOutputFormatter formatter,
            string encodingAsString,
            bool isDefaultEncoding)
        {
            Encoding encoding = null;
            if (isDefaultEncoding)
            {
                encoding = formatter
                    .SupportedEncodings
                    .First((e) => e.WebName.Equals(encodingAsString, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                encoding = Encoding.GetEncoding(encodingAsString);
                formatter.SupportedEncodings.Add(encoding);
            }

            return encoding;
        }

        protected abstract TextOutputFormatter GetOutputFormatter();

        protected sealed class ModelWithSerializationError
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
