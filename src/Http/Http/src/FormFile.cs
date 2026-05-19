// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Default implementation of <see cref="IFormFile"/>.
/// </summary>
public class FormFile : IFormFile
{
    // Stream.CopyTo method uses 80KB as the default buffer size.
    private const int DefaultBufferSize = 80 * 1024;

    private readonly Stream _baseStream;
    private readonly long _baseStreamOffset;

    /// <summary>
    /// Initializes a new instance of <see cref="FormFile"/>.
    /// </summary>
    /// <param name="baseStream">The <see cref="Stream"/> containing the form file.</param>
    /// <param name="baseStreamOffset">The offset at which the form file begins.</param>
    /// <param name="length">The length of the form file.</param>
    /// <param name="name">The name of the form file from the <c>Content-Disposition</c> header.</param>
    /// <param name="fileName">The file name from the <c>Content-Disposition</c> header.</param>
    public FormFile(Stream baseStream, long baseStreamOffset, long length, string name, string fileName)
    {
        _baseStream = baseStream;
        _baseStreamOffset = baseStreamOffset;
        Length = length;
        Name = name;
        FileName = fileName;
    }

    /// <summary>
    /// Gets the raw <c>Content-Disposition</c> header of the uploaded file.
    /// </summary>
    public string ContentDisposition
    {
        get { return Headers.ContentDisposition.ToString(); }
        set { Headers.ContentDisposition = value; }
    }

    /// <summary>
    /// Gets the raw <c>Content-Type</c> header of the uploaded file.
    /// </summary>
    public string ContentType
    {
        get { return Headers.ContentType.ToString(); }
        set { Headers.ContentType = value; }
    }

    /// <summary>
    /// Gets the header dictionary of the uploaded file.
    /// </summary>
    public IHeaderDictionary Headers { get; set; } = default!;

    /// <summary>
    /// Gets the file length in bytes.
    /// </summary>
    public long Length { get; }

    /// <summary>
    /// Gets the name from the <c>Content-Disposition</c> header.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the file name from the <c>Content-Disposition</c> header.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Opens the request stream for reading the uploaded file.
    /// </summary>
    public Stream OpenReadStream()
    {
        return new ReferenceReadStream(_baseStream, _baseStreamOffset, Length);
    }

    /// <summary>
    /// Copies the contents of the uploaded file to the <paramref name="target"/> stream.
    /// </summary>
    /// <param name="target">The stream to copy the file contents to.</param>
    public void CopyTo(Stream target)
    {
        ArgumentNullException.ThrowIfNull(target);

        using (var readStream = OpenReadStream())
        {
            readStream.CopyTo(target, DefaultBufferSize);
        }
    }

    /// <summary>
    /// Asynchronously copies the contents of the uploaded file to the <paramref name="target"/> stream.
    /// </summary>
    /// <param name="target">The stream to copy the file contents to.</param>
    /// <param name="cancellationToken"></param>
    public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default(CancellationToken))
    {
        ArgumentNullException.ThrowIfNull(target);

        using (var readStream = OpenReadStream())
        {
            await readStream.CopyToAsync(target, DefaultBufferSize, cancellationToken);
        }
    }
}
