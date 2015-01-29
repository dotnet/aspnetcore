// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.WebUtilities;

namespace Microsoft.AspNet.Http.Core
{
    public static class BufferingHelper
    {
        internal const int DefaultBufferThreshold = 1024 * 30;

        public static string TempDirectory
        {
            get
            {
                var temp = Environment.GetEnvironmentVariable("ASPNET_TEMP");
                if (string.IsNullOrEmpty(temp))
                {
                    temp = Environment.GetEnvironmentVariable("TEMP");
                }

                if (!Directory.Exists(temp))
                {
                    // TODO: ???
                    throw new DirectoryNotFoundException(temp);
                }

                return temp;
            }
        }

        public static HttpRequest EnableRewind([NotNull] this HttpRequest request, int bufferThreshold = DefaultBufferThreshold)
        {
            var body = request.Body;
            if (!body.CanSeek)
            {
                // TODO: Register this buffer for disposal at the end of the request to ensure the temp file is deleted.
                //  Otherwise it won't get deleted until GC closes the stream.
                request.Body = new FileBufferingReadStream(body, bufferThreshold, TempDirectory);
            }
            return request;
        }
    }
}