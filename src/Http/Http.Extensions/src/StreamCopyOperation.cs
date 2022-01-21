// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Extensions;

// FYI: In most cases the source will be a FileStream and the destination will be to the network.
/// <summary>
/// Provides APIs to copy a range of bytes from a source <see cref="Stream"/> to a destination <see cref="Stream"/>.
/// </summary>
public static class StreamCopyOperation
{
    /// <summary>Asynchronously reads the given number of bytes from the source stream and writes them to another stream.</summary>
    /// <returns>A task that represents the asynchronous copy operation.</returns>
    /// <param name="source">The stream from which the contents will be copied.</param>
    /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
    /// <param name="count">The count of bytes to be copied.</param>
    /// <param name="cancel">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
    public static Task CopyToAsync(Stream source, Stream destination, long? count, CancellationToken cancel)
        => StreamCopyOperationInternal.CopyToAsync(source, destination, count, cancel);

    /// <summary>Asynchronously reads the given number of bytes from the source stream and writes them to another stream, using a specified buffer size.</summary>
    /// <returns>A task that represents the asynchronous copy operation.</returns>
    /// <param name="source">The stream from which the contents will be copied.</param>
    /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
    /// <param name="count">The count of bytes to be copied.</param>
    /// <param name="bufferSize">The size, in bytes, of the buffer. This value must be greater than zero. The default size is 4096.</param>
    /// <param name="cancel">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
    public static Task CopyToAsync(Stream source, Stream destination, long? count, int bufferSize, CancellationToken cancel)
        => StreamCopyOperationInternal.CopyToAsync(source, destination, count, bufferSize, cancel);
}
