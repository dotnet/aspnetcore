// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.WebUtilities;

/// <summary>
/// A <see cref="Stream"/> that buffers content to be written to disk. Use <see cref="DrainBufferAsync(Stream, CancellationToken)" />
/// to write buffered content to a target <see cref="Stream" />.
/// </summary>
public sealed class FileBufferingWriteStream : Stream
{
    private const int DefaultMemoryThreshold = 32 * 1024; // 32k

    private readonly int _memoryThreshold;
    private readonly long? _bufferLimit;
    private readonly Func<string> _tempFileDirectoryAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="FileBufferingWriteStream"/>.
    /// </summary>
    /// <param name="memoryThreshold">
    /// The maximum amount of memory in bytes to allocate before switching to a file on disk.
    /// Defaults to 32kb.
    /// </param>
    /// <param name="bufferLimit">
    /// The maximum amount of bytes that the <see cref="FileBufferingWriteStream"/> is allowed to buffer.
    /// </param>
    /// <param name="tempFileDirectoryAccessor">Provides the location of the directory to write buffered contents to.
    /// When unspecified, uses the value specified by the environment variable <c>ASPNETCORE_TEMP</c> if available, otherwise
    /// uses the value returned by <see cref="Path.GetTempPath"/>.
    /// </param>
    public FileBufferingWriteStream(
        int memoryThreshold = DefaultMemoryThreshold,
        long? bufferLimit = null,
        Func<string>? tempFileDirectoryAccessor = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(memoryThreshold);

        if (bufferLimit != null && bufferLimit < memoryThreshold)
        {
            // We would expect a limit at least as much as memoryThreshold
            throw new ArgumentOutOfRangeException(nameof(bufferLimit), $"{nameof(bufferLimit)} must be larger than {nameof(memoryThreshold)}.");
        }

        _memoryThreshold = memoryThreshold;
        _bufferLimit = bufferLimit;
        _tempFileDirectoryAccessor = tempFileDirectoryAccessor ?? AspNetCoreTempDirectory.TempDirectoryFactory;
        PagedByteBuffer = new PagedByteBuffer(ArrayPool<byte>.Shared);
    }

    /// <summary>
    /// The maximum amount of memory in bytes to allocate before switching to a file on disk.
    /// </summary>
    /// <remarks>
    /// Defaults to 32kb.
    /// </remarks>
    public int MemoryThreshold => _memoryThreshold;

    /// <inheritdoc />
    public override bool CanRead => false;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => true;

    /// <inheritdoc />
    public override long Length => PagedByteBuffer.Length + (FileStream?.Length ?? 0);

    /// <inheritdoc />
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    internal PagedByteBuffer PagedByteBuffer { get; }

    internal FileStream? FileStream { get; private set; }

    internal bool Disposed { get; private set; }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    /// <inheritdoc/>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        ThrowIfDisposed();

        if (_bufferLimit.HasValue && _bufferLimit - Length < count)
        {
            Dispose();
            throw new IOException("Buffer limit exceeded.");
        }

        // Allow buffering in memory if we're below the memory threshold once the current buffer is written.
        var allowMemoryBuffer = (_memoryThreshold - count) >= PagedByteBuffer.Length;
        if (allowMemoryBuffer)
        {
            // Buffer content in the MemoryStream if it has capacity.
            PagedByteBuffer.Add(buffer, offset, count);
            Debug.Assert(PagedByteBuffer.Length <= _memoryThreshold);
        }
        else
        {
            // If the MemoryStream is incapable of accommodating the content to be written
            // spool to disk.
            EnsureFileStream();

            // Spool memory content to disk.
            PagedByteBuffer.MoveTo(FileStream);

            FileStream.Write(buffer, offset, count);
        }
    }

    /// <inheritdoc />
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
    }

    /// <inheritdoc />
    [SuppressMessage("ApiDesign", "RS0027:Public API with optional parameter(s) should have the most parameters amongst its public overloads", Justification = "This is a method overload.")]
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_bufferLimit.HasValue && _bufferLimit - Length < buffer.Length)
        {
            Dispose();
            throw new IOException("Buffer limit exceeded.");
        }

        // Allow buffering in memory if we're below the memory threshold once the current buffer is written.
        var allowMemoryBuffer = (_memoryThreshold - buffer.Length) >= PagedByteBuffer.Length;
        if (allowMemoryBuffer)
        {
            // Buffer content in the MemoryStream if it has capacity.
            PagedByteBuffer.Add(buffer);
            Debug.Assert(PagedByteBuffer.Length <= _memoryThreshold);
        }
        else
        {
            // If the MemoryStream is incapable of accommodating the content to be written
            // spool to disk.
            EnsureFileStream();

            // Spool memory content to disk.
            await PagedByteBuffer.MoveToAsync(FileStream, cancellationToken);
            await FileStream.WriteAsync(buffer, cancellationToken);
        }
    }

    /// <inheritdoc />
    public override void Flush()
    {
        // Do nothing.
    }

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <summary>
    /// Drains buffered content to <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">The <see cref="Stream" /> to drain buffered contents to.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken" />.</param>
    /// <returns>A <see cref="Task" /> that represents the asynchronous drain operation.</returns>
    public async Task DrainBufferAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        // When not null, FileStream always has "older" spooled content. The PagedByteBuffer always has "newer"
        // unspooled content. Copy the FileStream content first when available.
        if (FileStream != null)
        {
            // We make a new stream for async reads from disk and async writes to the destination
            await using var readStream = new FileStream(FileStream.Name, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite, bufferSize: 1, useAsync: true);

            await readStream.CopyToAsync(destination, cancellationToken);

            // This is created with delete on close
            await FileStream.DisposeAsync();
            FileStream = null;
        }

        await PagedByteBuffer.MoveToAsync(destination, cancellationToken);
    }

    /// <summary>
    /// Drains buffered content to <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">The <see cref="PipeWriter" /> to drain buffered contents to.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken" />.</param>
    /// <returns>A <see cref="Task" /> that represents the asynchronous drain operation.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public async Task DrainBufferAsync(PipeWriter destination, CancellationToken cancellationToken = default)
    {
        // When not null, FileStream always has "older" spooled content. The PagedByteBuffer always has "newer"
        // unspooled content. Copy the FileStream content first when available.
        if (FileStream != null)
        {
            // We make a new stream for async reads from disk and async writes to the destination
            await using var readStream = new FileStream(FileStream.Name, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite, bufferSize: 1, useAsync: true);

            await readStream.CopyToAsync(destination, cancellationToken);

            // This is created with delete on close
            await FileStream.DisposeAsync();
            FileStream = null;
        }

        await PagedByteBuffer.MoveToAsync(destination, cancellationToken);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            Disposed = true;

            PagedByteBuffer.Dispose();
            FileStream?.Dispose();
        }
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (!Disposed)
        {
            Disposed = true;

            PagedByteBuffer.Dispose();
            await (FileStream?.DisposeAsync() ?? default);
        }
    }

    [MemberNotNull(nameof(FileStream))]
    private void EnsureFileStream()
    {
        if (FileStream == null)
        {
            var tempFileDirectory = _tempFileDirectoryAccessor();
            var tempFileName = Path.Combine(tempFileDirectory, "ASPNETCORE_" + Guid.NewGuid() + ".tmp");

            // Create a temp file with the correct Unix file mode before moving it to the assigned tempFileName in the _tempFileDirectory.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var tempTempFileName = Path.GetTempFileName();
                File.Move(tempTempFileName, tempFileName);
            }

            FileStream = new FileStream(
                tempFileName,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Delete | FileShare.ReadWrite,
                bufferSize: 1,
                FileOptions.SequentialScan | FileOptions.DeleteOnClose);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Disposed, this);
    }
}
