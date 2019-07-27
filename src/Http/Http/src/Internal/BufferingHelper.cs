// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http
{
    internal static class BufferingHelper
    {
        internal const int DefaultBufferThreshold = 1024 * 30;

        public static HttpRequest EnableRewind(this HttpRequest request, int bufferThreshold = DefaultBufferThreshold, long? bufferLimit = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var body = request.Body;
            if (!body.CanSeek)
            {
                var tempPath = request.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().TempDirectoryPath;
                var fileStream = new FileBufferingStreamFactory(tempPath).CreateReadStream(body, bufferThreshold, bufferLimit);
                request.Body = fileStream;
                request.HttpContext.Response.RegisterForDispose(fileStream);
            }
            return request;
        }

        public static MultipartSection EnableRewind(this MultipartSection section, Action<IDisposable> registerForDispose,
            string tempDirectoryPath, int bufferThreshold = DefaultBufferThreshold, long? bufferLimit = null)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }
            if (registerForDispose == null)
            {
                throw new ArgumentNullException(nameof(registerForDispose));
            }
            if (tempDirectoryPath == null)
            {
                throw new ArgumentNullException(nameof(tempDirectoryPath));
            }

            var body = section.Body;
            if (!body.CanSeek)
            {
                var fileStream = new FileBufferingStreamFactory(tempDirectoryPath).CreateReadStream(body, bufferThreshold, bufferLimit);
                section.Body = fileStream;
                registerForDispose(fileStream);
            }
            return section;
        }
    }
}
