// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
                var httpBufferingOptions = request.HttpContext.RequestServices.GetRequiredService<IOptions<HttpBufferingOptions>>();
                var factory = new HttpFileBufferingStreamFactory(httpBufferingOptions);
                var fileStream = factory.CreateReadStream(body, bufferThreshold, bufferLimit);
                request.Body = fileStream;
                request.HttpContext.Response.RegisterForDispose(fileStream);
            }
            return request;
        }

        public static MultipartSection EnableRewind(this MultipartSection section, Action<IDisposable> registerForDispose,
            int bufferThreshold = DefaultBufferThreshold, long? bufferLimit = null, IOptions<HttpBufferingOptions> httpBufferingOptions = null)
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
                httpBufferingOptions ??= Options.Create(new HttpBufferingOptions());
                var factory = new HttpFileBufferingStreamFactory(httpBufferingOptions);
                var fileStream = factory.CreateReadStream(body, bufferThreshold, bufferLimit);
                section.Body = fileStream;
                registerForDispose(fileStream);
            }
            return section;
        }
    }
}
