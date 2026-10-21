// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.JSInterop;

/// <summary>
/// Represents the reference to a .NET stream sent to JavaScript.
/// </summary>
public sealed class DotNetStreamReference : IDisposable
{
    /// <summary>
    /// Create a reference to a .NET stream sent to JavaScript.
    /// </summary>
    /// <param name="stream">The stream being sent to JavaScript.</param>
    /// <param name="leaveOpen">A flag that indicates whether the stream should be left open after transmission.</param>
    public DotNetStreamReference(Stream stream, bool leaveOpen = false)
    {
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        LeaveOpen = leaveOpen;
    }

    /// <summary>
    /// The stream being sent to JavaScript.
    /// </summary>
    public Stream Stream { get; }

    /// <summary>
    /// A flag that indicates whether the stream should be left open after transmission.
    /// </summary>
    public bool LeaveOpen { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!LeaveOpen)
        {
            Stream.Dispose();
        }
    }
}
