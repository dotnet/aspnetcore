// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Internal;
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
                var factory = request.HttpContext.RequestServices.GetRequiredService<IFileBufferingStreamFactory>();
                var fileStream = factory.CreateReadStream(body, bufferThreshold, bufferLimit);
                request.Body = fileStream;
                request.HttpContext.Response.RegisterForDispose(fileStream);
            }
            return request;
        }

        [Obsolete("This method is obsolete. Use `EnableRewind` instead.")]
        public static MultipartSection EnableRewind(this MultipartSection section, Action<IDisposable> registerForDispose,
            int bufferThreshold = DefaultBufferThreshold, long? bufferLimit = null)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }
            if (registerForDispose == null)
            {
                throw new ArgumentNullException(nameof(registerForDispose));
            }

            var body = section.Body;
            if (!body.CanSeek)
            {
                var fileStream = new FileBufferingReadStream(body, bufferThreshold, bufferLimit, AspNetCoreTempDirectory.TempDirectoryFactory);
                section.Body = fileStream;
                registerForDispose(fileStream);
            }
            return section;
        }

        public static MultipartSection EnableRewind(this MultipartSection section, Action<IDisposable> registerForDispose,
            IFileBufferingStreamFactory fileBufferingStreamFactory, int bufferThreshold = DefaultBufferThreshold, long? bufferLimit = null)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }
            if (registerForDispose == null)
            {
                throw new ArgumentNullException(nameof(registerForDispose));
            }

            if (fileBufferingStreamFactory == null)
            {
                throw new ArgumentNullException(nameof(fileBufferingStreamFactory));
            }

            var body = section.Body;
            if (!body.CanSeek)
            {
                var fileStream = fileBufferingStreamFactory.CreateReadStream(body, bufferThreshold, bufferLimit);
                section.Body = fileStream;
                registerForDispose(fileStream);
            }
            return section;
        }
    }
}
