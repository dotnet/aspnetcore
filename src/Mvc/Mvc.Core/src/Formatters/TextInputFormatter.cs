// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// Reads an object from a request body with a text format.
/// </summary>
public abstract class TextInputFormatter : InputFormatter
{
    /// <summary>
    /// Returns UTF8 Encoding without BOM and throws on invalid bytes.
    /// </summary>
    protected static readonly Encoding UTF8EncodingWithoutBOM
        = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Returns UTF16 Encoding which uses littleEndian byte order with BOM and throws on invalid bytes.
    /// </summary>
    protected static readonly Encoding UTF16EncodingLittleEndian
        = new UnicodeEncoding(bigEndian: false, byteOrderMark: true, throwOnInvalidBytes: true);

    /// <summary>
    /// Gets the mutable collection of character encodings supported by
    /// this <see cref="TextInputFormatter"/>. The encodings are
    /// used when reading the data.
    /// </summary>
    public IList<Encoding> SupportedEncodings { get; } = new List<Encoding>();

    /// <inheritdoc />
    public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var selectedEncoding = SelectCharacterEncoding(context);
        if (selectedEncoding == null)
        {
            var message = Resources.FormatUnsupportedContentType(
                context.HttpContext.Request.ContentType);

            var exception = new UnsupportedContentTypeException(message);
            context.ModelState.AddModelError(context.ModelName, exception, context.Metadata);

            return InputFormatterResult.FailureAsync();
        }

        return ReadRequestBodyAsync(context, selectedEncoding);
    }

    /// <summary>
    /// Reads an object from the request body.
    /// </summary>
    /// <param name="context">The <see cref="InputFormatterContext"/>.</param>
    /// <param name="encoding">The <see cref="Encoding"/> used to read the request body.</param>
    /// <returns>A <see cref="Task"/> that on completion deserializes the request body.</returns>
    public abstract Task<InputFormatterResult> ReadRequestBodyAsync(
        InputFormatterContext context,
        Encoding encoding);

    /// <summary>
    /// Returns an <see cref="Encoding"/> based on <paramref name="context"/>'s
    /// character set.
    /// </summary>
    /// <param name="context">The <see cref="InputFormatterContext"/>.</param>
    /// <returns>
    /// An <see cref="Encoding"/> based on <paramref name="context"/>'s
    /// character set. <c>null</c> if no supported encoding was found.
    /// </returns>
    protected Encoding? SelectCharacterEncoding(InputFormatterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (SupportedEncodings.Count == 0)
        {
            var message = Resources.FormatTextInputFormatter_SupportedEncodingsMustNotBeEmpty(
                nameof(SupportedEncodings));
            throw new InvalidOperationException(message);
        }

        var requestContentType = context.HttpContext.Request.ContentType;
        var requestMediaType = string.IsNullOrEmpty(requestContentType) ? default : new MediaType(requestContentType);
        if (requestMediaType.Charset.HasValue)
        {
            // Create Encoding based on requestMediaType.Charset to support charset aliases and custom Encoding
            // providers. Charset -> Encoding -> encoding.WebName chain canonicalizes the charset name.
            var requestEncoding = requestMediaType.Encoding;
            if (requestEncoding != null)
            {
                for (int i = 0; i < SupportedEncodings.Count; i++)
                {
                    if (string.Equals(
                        requestEncoding.WebName,
                        SupportedEncodings[i].WebName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return SupportedEncodings[i];
                    }
                }
            }

            // The client specified an encoding in the content type header of the request
            // but we don't understand it. In this situation we don't try to pick any other encoding
            // from the list of supported encodings and read the body with that encoding.
            // Instead, we return null and that will translate later on into a 415 Unsupported Media Type
            // response.
            return null;
        }

        // We want to do our best effort to read the body of the request even in the
        // cases where the client doesn't send a content type header or sends a content
        // type header without encoding. For that reason we pick the first encoding of the
        // list of supported encodings and try to use that to read the body. This encoding
        // is UTF-8 by default in our formatters, which generally is a safe choice for the
        // encoding.
        return SupportedEncodings[0];
    }
}
