// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class FormPostResponseGenerator
    {
        private const string FormPostHeaderFormat = @"<!doctype html>
<html>
<head>
  <title>Please wait while you're being redirected to the identity provider</title>
</head>
<body>
  <form name=""form"" method=""post"" action=""{0}"">";

        private const string FormPostParameterFormat = @"    <input type=""hidden"" name=""{0}"" value=""{1}"" />";

        private const string FormPostFooterFormat =
@"    <noscript>Click here to finish the process: <input type=""submit"" /></noscript>
  </form>
  <script>document.form.submit();</script>
</body>
</html>";

        private readonly HtmlEncoder _encoder;

        public FormPostResponseGenerator(HtmlEncoder encoder)
        {
            _encoder = encoder;
        }

        public async Task GenerateResponseAsync(
            HttpContext context,
            string redirectUri,
            IEnumerable<KeyValuePair<string, string>> parameters)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
                {
                    writer.WriteLine(FormPostHeaderFormat, redirectUri);
                    foreach (var parameter in parameters)
                    {
                        writer.WriteLine(FormPostParameterFormat, _encoder.Encode(parameter.Key), _encoder.Encode(parameter.Value));
                    }
                    writer.Write(FormPostFooterFormat);
                };
                stream.Seek(0, SeekOrigin.Begin);

                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.ContentLength = stream.Length;
                await stream.CopyToAsync(context.Response.Body);
            }
        }
    }
}
