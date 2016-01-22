// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.WebUtilities;

namespace Microsoft.AspNet.Http.Internal
{
    public static class BufferingHelper
    {
        internal const int DefaultBufferThreshold = 1024 * 30;

        private readonly static Func<string> _getTempDirectory = () => TempDirectory;

        private static string _tempDirectory;

        public static string TempDirectory
        {
            get
            {
                if (_tempDirectory == null)
                {
                    // Look for folders in the following order.
                    var temp = Environment.GetEnvironmentVariable("ASPNET_TEMP") ??     // ASPNET_TEMP - User set temporary location.
                               Path.GetTempPath();                                      // Fall back.

                    if (!Directory.Exists(temp))
                    {
                        // TODO: ???
                        throw new DirectoryNotFoundException(temp);
                    }

                    _tempDirectory = temp;
                }

                return _tempDirectory;
            }
        }

        public static HttpRequest EnableRewind(this HttpRequest request, int bufferThreshold = DefaultBufferThreshold)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var body = request.Body;
            if (!body.CanSeek)
            {
                var fileStream = new FileBufferingReadStream(body, bufferThreshold, _getTempDirectory);
                request.Body = fileStream;
                request.HttpContext.Response.RegisterForDispose(fileStream);
            }
            return request;
        }
    }
}