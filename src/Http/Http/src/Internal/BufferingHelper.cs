// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Http;

internal static class BufferingHelper
{
    internal const int DefaultBufferThreshold = 1024 * 30;

    public static HttpRequest EnableRewind(this HttpRequest request, int bufferThreshold = DefaultBufferThreshold, long? bufferLimit = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var body = request.Body;
        if (!body.CanSeek)
        {
            var fileStream = new FileBufferingReadStream(body, bufferThreshold, bufferLimit, AspNetCoreTempDirectory.TempDirectoryFactory);
            request.Body = fileStream;
            request.HttpContext.Response.RegisterForDispose(fileStream);
        }
        return request;
    }

    public static MultipartSection EnableRewind(this MultipartSection section, Action<IDisposable> registerForDispose,
        int bufferThreshold = DefaultBufferThreshold, long? bufferLimit = null)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(registerForDispose);

        var body = section.Body;
        if (!body.CanSeek)
        {
            var fileStream = new FileBufferingReadStream(body, bufferThreshold, bufferLimit, AspNetCoreTempDirectory.TempDirectoryFactory);
            section.Body = fileStream;
            registerForDispose(fileStream);
        }
        return section;
    }
}
