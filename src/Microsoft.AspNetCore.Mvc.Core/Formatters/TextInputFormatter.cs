// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// Reads an object from a request body with a text format.
    /// </summary>
    public abstract class TextInputFormatter : InputFormatter
    {
        // ASP.NET Core MVC 1.0 used Encoding.GetEncoding() when comparing the request charset with SupportedEncodings.
        // That method supports the following alternate names for system-supported encodings. This table maps from
        // .NET Core's keys in s_encodingDataTable to values in s_encodingDataTableItems, less identity mappings. It
        // should be kept in sync with those Unix-specific tables in
        // https://github.com/dotnet/coreclr/blob/master/src/mscorlib/src/System/Globalization/EncodingTable.Unix.cs
        // or their Windows equivalents (also for .NET Core not desktop .NET): COMNlsInfo::EncodingDataTable and
        // COMNlsInfo::CodePageDataTable in EncodingData in
        // https://github.com/dotnet/coreclr/blob/master/src/classlibnative/nls/encodingdata.cpp
        private static readonly IReadOnlyDictionary<string, string> EncodingAliases = new Dictionary<string, string>
        {
            { "ANSI_X3.4-1968", "us-ascii" },
            { "ANSI_X3.4-1986", "us-ascii" },
            { "ascii", "us-ascii" },
            { "cp367", "us-ascii" },
            { "cp819", "iso-8859-1" },
            { "csASCII", "us-ascii" },
            { "csISOLatin1", "iso-8859-1" },
            { "csUnicode11UTF7", "utf-7" },
            { "IBM367", "us-ascii" },
            { "ibm819", "iso-8859-1" },
            { "ISO-10646-UCS-2", "utf-16" },
            { "iso-ir-100", "iso-8859-1" },
            { "iso-ir-6", "us-ascii" },
            { "ISO646-US", "us-ascii" },
            { "ISO_646.irv:1991", "us-ascii" },
            { "iso_8859-1:1987", "iso-8859-1" },
            { "l1", "iso-8859-1" },
            { "latin1", "iso-8859-1" },
            { "ucs-2", "utf-16" },
            { "unicode", "utf-16"},
            { "unicode-1-1-utf-7", "utf-7" },
            { "unicode-1-1-utf-8", "utf-8" },
            { "unicode-2-0-utf-7", "utf-7" },
            { "unicode-2-0-utf-8", "utf-8" },
            { "unicodeFFFE", "utf-16BE"},
            { "us", "us-ascii" },
            { "UTF-16LE", "utf-16"},
            { "UTF-32LE", "utf-32" },
            { "x-unicode-1-1-utf-7", "utf-7" },
            { "x-unicode-1-1-utf-8", "utf-8" },
            { "x-unicode-2-0-utf-7", "utf-7" },
            { "x-unicode-2-0-utf-8", "utf-8" },
        };

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
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var request = context.HttpContext.Request;
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
        protected Encoding SelectCharacterEncoding(InputFormatterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (SupportedEncodings.Count == 0)
            {
                var message = Resources.FormatTextInputFormatter_SupportedEncodingsMustNotBeEmpty(
                    nameof(SupportedEncodings));
                throw new InvalidOperationException(message);
            }

            var requestContentType = context.HttpContext.Request.ContentType;
            var requestEncoding = requestContentType == null ?
                default(StringSegment) :
                new MediaType(requestContentType).Charset;
            if (requestEncoding.HasValue)
            {
                var encodingName = requestEncoding.Value;
                string alias;
                if (EncodingAliases.TryGetValue(encodingName, out alias))
                {
                    // Given name was an encoding alias. Use the preferred name.
                    encodingName = alias;
                }

                for (int i = 0; i < SupportedEncodings.Count; i++)
                {
                    if (string.Equals(encodingName, SupportedEncodings[i].WebName, StringComparison.OrdinalIgnoreCase))
                    {
                        return SupportedEncodings[i];
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
}