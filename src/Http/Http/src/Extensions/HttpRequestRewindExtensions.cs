// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Extension methods for enabling buffering in an <see cref="HttpRequest"/>.
/// </summary>
public static class HttpRequestRewindExtensions
{
    /// <summary>
    /// Ensure the <paramref name="request"/> <see cref="HttpRequest.Body"/> can be read multiple times. Normally
    /// buffers request bodies in memory; writes requests larger than 30K bytes to disk.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/> to prepare.</param>
    /// <remarks>
    /// Temporary files for larger requests are written to the location named in the <c>ASPNETCORE_TEMP</c>
    /// environment variable, if any. If that environment variable is not defined, these files are written to the
    /// current user's temporary folder. Files are automatically deleted at the end of their associated requests.
    /// </remarks>
    public static void EnableBuffering(this HttpRequest request)
    {
        BufferingHelper.EnableRewind(request);
    }

    /// <summary>
    /// Ensure the <paramref name="request"/> <see cref="HttpRequest.Body"/> can be read multiple times. Normally
    /// buffers request bodies in memory; writes requests larger than <paramref name="bufferThreshold"/> bytes to
    /// disk.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/> to prepare.</param>
    /// <param name="bufferThreshold">
    /// The maximum size in bytes of the in-memory <see cref="System.Buffers.ArrayPool{Byte}"/> used to buffer the
    /// stream. Larger request bodies are written to disk.
    /// </param>
    /// <remarks>
    /// Temporary files for larger requests are written to the location named in the <c>ASPNETCORE_TEMP</c>
    /// environment variable, if any. If that environment variable is not defined, these files are written to the
    /// current user's temporary folder. Files are automatically deleted at the end of their associated requests.
    /// </remarks>
    public static void EnableBuffering(this HttpRequest request, int bufferThreshold)
    {
        BufferingHelper.EnableRewind(request, bufferThreshold);
    }

    /// <summary>
    /// Ensure the <paramref name="request"/> <see cref="HttpRequest.Body"/> can be read multiple times. Normally
    /// buffers request bodies in memory; writes requests larger than 30K bytes to disk.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/> to prepare.</param>
    /// <param name="bufferLimit">
    /// The maximum size in bytes of the request body. An attempt to read beyond this limit will cause an
    /// <see cref="System.IO.IOException"/>.
    /// </param>
    /// <remarks>
    /// Temporary files for larger requests are written to the location named in the <c>ASPNETCORE_TEMP</c>
    /// environment variable, if any. If that environment variable is not defined, these files are written to the
    /// current user's temporary folder. Files are automatically deleted at the end of their associated requests.
    /// </remarks>
    public static void EnableBuffering(this HttpRequest request, long bufferLimit)
    {
        BufferingHelper.EnableRewind(request, bufferLimit: bufferLimit);
    }

    /// <summary>
    /// Ensure the <paramref name="request"/> <see cref="HttpRequest.Body"/> can be read multiple times. Normally
    /// buffers request bodies in memory; writes requests larger than <paramref name="bufferThreshold"/> bytes to
    /// disk.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/> to prepare.</param>
    /// <param name="bufferThreshold">
    /// The maximum size in bytes of the in-memory <see cref="System.Buffers.ArrayPool{Byte}"/> used to buffer the
    /// stream. Larger request bodies are written to disk.
    /// </param>
    /// <param name="bufferLimit">
    /// The maximum size in bytes of the request body. An attempt to read beyond this limit will cause an
    /// <see cref="System.IO.IOException"/>.
    /// </param>
    /// <remarks>
    /// Temporary files for larger requests are written to the location named in the <c>ASPNETCORE_TEMP</c>
    /// environment variable, if any. If that environment variable is not defined, these files are written to the
    /// current user's temporary folder. Files are automatically deleted at the end of their associated requests.
    /// </remarks>
    public static void EnableBuffering(this HttpRequest request, int bufferThreshold, long bufferLimit)
    {
        BufferingHelper.EnableRewind(request, bufferThreshold, bufferLimit);
    }
}
