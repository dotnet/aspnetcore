// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Always writes a string value to the response, regardless of requested content type.
    /// </summary>
    public class TextPlainFormatter : OutputFormatter
    {
        public TextPlainFormatter()
        {
            SupportedEncodings.Add(Encodings.UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(Encodings.UTF16EncodingLittleEndian);
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/plain"));
        }

        public override bool CanWriteResult(OutputFormatterContext context, MediaTypeHeaderValue contentType)
        {
            // Ignore the passed in content type, if the object is string 
            // always return it as a text/plain format.
            if (context.DeclaredType == typeof(string))
            {
                return true;
            }

            if (context.Object is string)
            {
                return true;
            }

            return false;
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterContext context)
        {
            var valueAsString = (string)context.Object;
            if (valueAsString == null)
            {
                // if the value is null don't write anything.
                return;
            }

            var response = context.ActionContext.HttpContext.Response;

            using (var delegatingStream = new DelegatingStream(response.Body))
            using (var writer = new StreamWriter(delegatingStream, context.SelectedEncoding, 1024, leaveOpen: true))
            {
                await writer.WriteAsync(valueAsString);
            }
        }
    }
}