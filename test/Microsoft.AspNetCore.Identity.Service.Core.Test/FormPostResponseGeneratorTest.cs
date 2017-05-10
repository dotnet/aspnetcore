// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class FormPostResponseGeneratorTest
    {
        [Fact]
        public async Task GenerateResponse_EncodesParameters_OnAnAutoPostedForm()
        {
            // Arrange
            var expectedBody = @"<!doctype html>
<html>
<head>
  <title>Please wait while you're being redirected to the identity provider</title>
</head>
<body>
  <form name=""form"" method=""post"" action=""http://www.example.com/callback"">
    <input type=""hidden"" name=""state"" value=""&lt;&gt;&amp;"" />
    <input type=""hidden"" name=""code"" value=""serializedcode"" />
    <noscript>Click here to finish the process: <input type=""submit"" /></noscript>
  </form>
  <script>document.form.submit();</script>
</body>
</html>";
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();
            var generator = new FormPostResponseGenerator(HtmlEncoder.Default);
            var redirectUri = "http://www.example.com/callback";
            var response = new Dictionary<string, string>
            {
                ["state"] = "<>&",
                ["code"] = "serializedcode"
            };

            // Act
            await generator.GenerateResponseAsync(httpContext, redirectUri, response);

            // Assert
            Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
            Assert.Equal("text/html; charset=utf-8", httpContext.Response.ContentType);
            var body = httpContext.Response.Body;
            body.Seek(0, SeekOrigin.Begin);

            var bodyText = await new StreamReader(body).ReadToEndAsync();
            Assert.Equal(expectedBody, bodyText);
        }
    }
}
