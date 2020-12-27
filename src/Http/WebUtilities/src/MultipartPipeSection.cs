// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.WebUtilities
{

    public class MultipartPipeSection
    {
        public string? ContentType
        {
            get
            {
                if (Headers != null && Headers.TryGetValue(HeaderNames.ContentType, out var values))
                {
                    return values;
                }
                return null;
            }
        }

        public string? ContentDisposition
        {
            get
            {
                if (Headers != null && Headers.TryGetValue(HeaderNames.ContentDisposition, out var values))
                {
                    return values;
                }
                return null;
            }
        }

        public Dictionary<string, StringValues>? Headers { get; set; }

        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        public PipeReader BodyReader { get; set; } = default!;

        /// <summary>
        /// The position where the body starts in the total multipart body.
        /// This may not be available if the total multipart body is not seekable.
        /// </summary>
        public long? BaseStreamOffset { get; set; }
    }
}
