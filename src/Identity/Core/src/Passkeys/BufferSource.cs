// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents a base64url-encoded byte buffer for use in passkey operations.
/// </summary>
/// <remarks>
/// This type is named after the JavaScript <c>BufferSource</c> type.
/// When included in a JSON payload, it is serialized as a base64url-encoded string.
/// When a member of type <c>BufferSource</c> is mentioned in the WebAuthn specification,
/// this type can be used to represent it in .NET.
/// </remarks>
[JsonConverter(typeof(BufferSourceJsonConverter))]
internal sealed class BufferSource : IEquatable<BufferSource>
{
    private readonly ReadOnlyMemory<byte> _bytes;

    /// <summary>
    /// Gets the length of the byte buffer.
    /// </summary>
    public int Length => _bytes.Length;

    /// <summary>
    /// Creates a new instance of <see cref="BufferSource"/> from a byte array.
    /// </summary>
    public static BufferSource FromBytes(ReadOnlyMemory<byte> bytes)
        => new(bytes);

    /// <summary>
    /// Creates a new instance of <see cref="BufferSource"/> from a string.
    /// </summary>
    public static BufferSource FromString(string value)
    {
        var buffer = Encoding.UTF8.GetBytes(value);
        return new(buffer);
    }

    private BufferSource(ReadOnlyMemory<byte> buffer)
    {
        _bytes = buffer;
    }

    /// <summary>
    /// Gets the byte buffer as a <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    public ReadOnlyMemory<byte> AsMemory()
        => _bytes;

    /// <summary>
    /// Gets the byte buffer as a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public ReadOnlySpan<byte> AsSpan()
        => _bytes.Span;

    /// <summary>
    /// Gets the byte buffer as a byte array.
    /// </summary>
    public byte[] ToArray()
        => _bytes.ToArray();

    /// <summary>
    /// Performs a value-based equality comparison with another <see cref="BufferSource"/> instance.
    /// </summary>
    public bool Equals(BufferSource? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null && _bytes.Span.SequenceEqual(other._bytes.Span);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is BufferSource other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => _bytes.GetHashCode();

    /// <summary>
    /// Performs a value-based equality comparison between two <see cref="BufferSource"/> instances.
    /// </summary>
    public static bool operator ==(BufferSource? left, BufferSource? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Performs a value-based inequality comparison between two <see cref="BufferSource"/> instances.
    /// </summary>
    public static bool operator !=(BufferSource? left, BufferSource? right)
        => !(left == right);

    /// <summary>
    /// Gets the UTF-8 string representation of the byte buffer.
    /// </summary>
    public override string ToString()
    {
        var span = _bytes.Span;

        if (span.IsEmpty)
        {
            return string.Empty;
        }

        return Encoding.UTF8.GetString(span);
    }
}
