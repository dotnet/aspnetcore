// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Helper type that allows copying a file to a Stream.
/// <para>
/// This type is part of ASP.NET Core's infrastructure and should not used by application code.
/// </para>
/// </summary>
public static class SendFileFallback
{
    /// <summary>
    /// Copies the segment of the file to the destination stream.
    /// </summary>
    /// <param name="destination">The stream to write the file segment to.</param>
    /// <param name="filePath">The full disk path to the file.</param>
    /// <param name="offset">The offset in the file to start at.</param>
    /// <param name="count">The number of bytes to send, or null to send the remainder of the file.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to abort the transmission.</param>
    /// <returns></returns>
    public static async Task SendFileAsync(Stream destination, string filePath, long offset, long? count, CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(filePath);
        if (offset < 0 || offset > fileInfo.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, string.Empty);
        }
        if (count.HasValue &&
            (count.Value < 0 || count.Value > fileInfo.Length - offset))
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, string.Empty);
        }

        cancellationToken.ThrowIfCancellationRequested();

        const int bufferSize = 1024 * 16;

        var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: bufferSize,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        using (fileStream)
        {
            fileStream.Seek(offset, SeekOrigin.Begin);
            await StreamCopyOperationInternal.CopyToAsync(fileStream, destination, count, bufferSize, cancellationToken);
        }
    }
}
