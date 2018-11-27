// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace FormatterWebSite
{
    public class StreamController : Controller
    {
        [HttpGet]
        public Stream SimpleMemoryStream()
        {
            return CreateDefaultStream();
        }

        [HttpGet]
        public Stream MemoryStreamWithContentType()
        {
            Response.ContentType = "text/html";
            return CreateDefaultStream();
        }

        [HttpGet]
        [Produces("text/plain")]
        public Stream MemoryStreamWithContentTypeFromProduces()
        {
            return CreateDefaultStream();
        }

        [HttpGet]
        [Produces("text/html", "text/plain")]
        public Stream MemoryStreamWithContentTypeFromProducesWithMultipleValues()
        {
            return CreateDefaultStream();
        }

        [HttpGet]
        [Produces("text/plain")]
        public Stream MemoryStreamOverridesProducesContentTypeWithResponseContentType()
        {
            // Produces will set a ContentType on the implicit ObjectResult and
            // ContentType on response are overriden by content types from ObjectResult.
            Response.ContentType = "text/html";

            return CreateDefaultStream();
        }

        private static Stream CreateDefaultStream()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write("Sample text from a stream");
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }
    }
}
