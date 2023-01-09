// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;

namespace Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// A <see cref="HtmlEncoder"/> that does not encode. Should not be used when writing directly to a response
/// expected to contain valid HTML.
/// </summary>
public sealed class NullHtmlEncoder : HtmlEncoder
{
    /// <summary>
    /// Initializes a <see cref="NullHtmlEncoder"/> instance.
    /// </summary>
    private NullHtmlEncoder()
    {
    }

    /// <summary>
    /// A <see cref="HtmlEncoder"/> instance that does not encode. Should not be used when writing directly to a
    /// response expected to contain valid HTML.
    /// </summary>
    public static new NullHtmlEncoder Default { get; } = new NullHtmlEncoder();

    /// <inheritdoc />
    public override int MaxOutputCharactersPerInputCharacter => 1;

    /// <inheritdoc />
    public override string Encode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value;
    }

    /// <inheritdoc />
    public override void Encode(TextWriter output, char[] value, int startIndex, int characterCount)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(value);

        if (characterCount == 0)
        {
            return;
        }

        output.Write(value, startIndex, characterCount);
    }

    /// <inheritdoc />
    public override void Encode(TextWriter output, string value, int startIndex, int characterCount)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(value);

        if (characterCount == 0)
        {
            return;
        }

        var span = value.AsSpan(startIndex, characterCount);

        output.Write(span);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override unsafe int FindFirstCharacterToEncode(char* text, int textLength)
    {
        return -1;
    }

    /// <inheritdoc />
    public override unsafe bool TryEncodeUnicodeScalar(
        int unicodeScalar,
        char* buffer,
        int bufferLength,
        out int numberOfCharactersWritten)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        numberOfCharactersWritten = 0;

        return false;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool WillEncode(int unicodeScalar)
    {
        return false;
    }
}
