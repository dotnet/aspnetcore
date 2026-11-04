// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.JSInterop.Implementation;

/// <summary>
/// Implements functionality for <see cref="IJSStreamReference"/>.
/// </summary>
public sealed class JSStreamReference : JSObjectReference, IJSStreamReference
{
    private readonly JSRuntime _jsRuntime;

    /// <inheritdoc />
    public long Length { get; }

    /// <summary>
    /// Initializes a new <see cref="JSStreamReference"/> instance.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="JSRuntime"/> used for invoking JS interop calls.</param>
    /// <param name="id">The unique identifier.</param>
    /// <param name="totalLength">The length of the data stream coming from JS represented by this data reference.</param>
    internal JSStreamReference(JSRuntime jsRuntime, long id, long totalLength) : base(jsRuntime, id)
    {
        if (totalLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalLength), totalLength, "Length must be a positive value.");
        }

        _jsRuntime = jsRuntime;
        Length = totalLength;
    }

    /// <inheritdoc />
    async ValueTask<Stream> IJSStreamReference.OpenReadStreamAsync(long maxAllowedSize, CancellationToken cancellationToken)
    {
        if (Length > maxAllowedSize)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAllowedSize), $"The incoming data stream of length {Length} exceeds the maximum allowed length {maxAllowedSize}.");
        }

        return await _jsRuntime.ReadJSDataAsStreamAsync(this, Length, cancellationToken);
    }
}
