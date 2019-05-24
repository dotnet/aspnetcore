// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public class SystemTextJsonOutputFormatterTest : JsonOutputFormatterTestBase
    {
        protected override TextOutputFormatter GetOutputFormatter()
        {
            return new SystemTextJsonOutputFormatter(new MvcOptions());
        }

        [Theory]
        [MemberData(nameof(WriteCorrectCharacterEncoding))]
        public async Task WriteToStreamAsync_UsesCorrectCharacterEncoding(
           string content,
           string encodingAsString,
           bool isDefaultEncoding)
        {
            // Arrange
            var formatter = GetOutputFormatter();
            var expectedContent = "\"" + JavaScriptEncoder.Default.Encode(content) + "\"";
            var mediaType = MediaTypeHeaderValue.Parse(string.Format("application/json; charset={0}", encodingAsString));
            var encoding = CreateOrGetSupportedEncoding(formatter, encodingAsString, isDefaultEncoding);


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
            var actualContent = encoding.GetString(body.ToArray());
            Assert.Equal(expectedContent, actualContent, StringComparer.OrdinalIgnoreCase);
        }
    }
}
