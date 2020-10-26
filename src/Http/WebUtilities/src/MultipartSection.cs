// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.WebUtilities
{
    public class MultipartSection
    {
        public string ContentType
        {
            get
            {
                StringValues values;
                if (Headers.TryGetValue(HeaderNames.ContentType, out values))
                {
                    return values;
                }
                return null;
            }
        }

        public string ContentDisposition
        {
            get
            {
                StringValues values;
                if (Headers.TryGetValue(HeaderNames.ContentDisposition, out values))
                {
                    return values;
                }
                return null;
            }
        }

        public Dictionary<string, StringValues> Headers { get; set; }

        public Stream Body { get; set; }

        /// <summary>
        /// The position where the body starts in the total multipart body.
        /// This may not be available if the total multipart body is not seekable.
        /// </summary>
        public long? BaseStreamOffset { get; set; }
    }
}
