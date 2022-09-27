// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Represents the data of a file selected from an <see cref="InputFile"/> component.
/// <para>
/// Note: Metadata is provided by the client and is untrusted.
/// </para>
/// </summary>
public interface IBrowserFile
{
    /// <summary>
    /// Gets the name of the file as specified by the browser.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the last modified date as specified by the browser.
    /// </summary>
    DateTimeOffset LastModified { get; }

    /// <summary>
    /// Gets the size of the file in bytes as specified by the browser.
    /// </summary>
    long Size { get; }

    /// <summary>
    /// Gets the MIME type of the file as specified by the browser.
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// Opens the stream for reading the uploaded file.
    /// </summary>
    /// <param name="maxAllowedSize">
    /// The maximum number of bytes that can be supplied by the Stream. Defaults to 500 KB.
    /// <para>
    /// Calling <see cref="OpenReadStream(long, CancellationToken)"/>
    /// will throw if the file's size, as specified by <see cref="Size"/> is larger than
    /// <paramref name="maxAllowedSize"/>. By default, if the user supplies a file larger than 500 KB, this method will throw an exception.
    /// </para>
    /// <para>
    /// It is valuable to choose a size limit that corresponds to your use case. If you allow excessively large files, this
    /// may result in excessive consumption of memory or disk/database space, depending on what your code does
    /// with the supplied <see cref="Stream"/>.
    /// </para>
    /// <para>
    /// For Blazor Server in particular, beware of reading the entire stream into a memory buffer unless you have
    /// passed a suitably low size limit, since you will be consuming that memory on the server.
    /// </para>
    /// </param>
    /// <param name="cancellationToken">A cancellation token to signal the cancellation of streaming file data.</param>
    /// <exception cref="IOException">Thrown if the file's length exceeds the <paramref name="maxAllowedSize"/> value.</exception>
    Stream OpenReadStream(long maxAllowedSize = 500 * 1024, CancellationToken cancellationToken = default);
}
