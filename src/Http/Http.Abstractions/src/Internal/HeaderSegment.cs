// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http;

internal readonly struct HeaderSegment : IEquatable<HeaderSegment>
{
    private readonly StringSegment _formatting;
    private readonly StringSegment _data;

    // <summary>
    // Initializes a new instance of the <see cref="HeaderSegment"/> structure.
    // </summary>
    public HeaderSegment(StringSegment formatting, StringSegment data)
    {
        _formatting = formatting;
        _data = data;
    }

    public StringSegment Formatting
    {
        get { return _formatting; }
    }

    public StringSegment Data
    {
        get { return _data; }
    }

    public bool Equals(HeaderSegment other)
    {
        return _formatting.Equals(other._formatting) && _data.Equals(other._data);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        return obj is HeaderSegment value && Equals(value);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (_formatting.GetHashCode() * 397) ^ _data.GetHashCode();
        }
    }

    public static bool operator ==(HeaderSegment left, HeaderSegment right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(HeaderSegment left, HeaderSegment right)
    {
        return !left.Equals(right);
    }
}
