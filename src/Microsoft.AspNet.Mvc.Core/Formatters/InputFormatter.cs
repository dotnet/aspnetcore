// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.Formatters
{
    /// <summary>
    /// Reads an object from the request body.
    /// </summary>
    public abstract class InputFormatter : IInputFormatter
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
        /// this <see cref="InputFormatter"/>. The encodings are
        /// used when reading the data.
        /// </summary>
        public IList<Encoding> SupportedEncodings { get; } = new List<Encoding>();

        /// <summary>
        /// Gets the mutable collection of media type elements supported by
        /// this <see cref="InputFormatter"/>.
        /// </summary>
        public MediaTypeCollection SupportedMediaTypes { get; } = new MediaTypeCollection();

        protected object GetDefaultValueForType(Type modelType)
        {
            if (modelType.GetTypeInfo().IsValueType)
            {
                return Activator.CreateInstance(modelType);
            }

            return null;
        }

        /// <inheritdoc />
        public virtual bool CanRead(InputFormatterContext context)
        {
            if (!CanReadType(context.ModelType))
            {
                return false;
            }

            var contentType = context.HttpContext.Request.ContentType;
            if (string.IsNullOrEmpty(contentType))
            {
                return false;
            }

            // Confirm the request's content type is more specific than a media type this formatter supports e.g. OK if
            // client sent "text/plain" data and this formatter supports "text/*".
            return IsSubsetOfAnySupportedContentType(contentType);
        }

        private bool IsSubsetOfAnySupportedContentType(string contentType)
        {
            var parsedContentType = new MediaType(contentType);
            for (var i = 0; i < SupportedMediaTypes.Count; i++)
            {
                var supportedMediaType = new MediaType(SupportedMediaTypes[i]);
                if (parsedContentType.IsSubsetOf(supportedMediaType))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether this <see cref="InputFormatter"/> can deserialize an object of the given
        /// <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of object that will be read.</param>
        /// <returns><c>true</c> if the <paramref name="type"/> can be read, otherwise <c>false</c>.</returns>
        protected virtual bool CanReadType(Type type)
        {
            return true;
        }

        /// <inheritdoc />
        public virtual Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
        {
            var request = context.HttpContext.Request;
            if (request.ContentLength == 0)
            {
                return InputFormatterResult.SuccessAsync(GetDefaultValueForType(context.ModelType));
            }

            return ReadRequestBodyAsync(context);
        }

        /// <summary>
        /// Reads an object from the request body.
        /// </summary>
        /// <param name="context">The <see cref="InputFormatterContext"/>.</param>
        /// <returns>A <see cref="Task"/> that on completion deserializes the request body.</returns>
        public abstract Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context);

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
            var request = context.HttpContext.Request;

            if (request.ContentType != null)
            {
                var encoding = MediaType.GetEncoding(request.ContentType);
                if (encoding != null)
                {
                    foreach (var supportedEncoding in SupportedEncodings)
                    {
                        if (string.Equals(
                            encoding.WebName,
                            supportedEncoding.WebName,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            return supportedEncoding;
                        }
                    }
                }
            }

            if (SupportedEncodings.Count > 0)
            {
                return SupportedEncodings[0];
            }

            // No supported encoding was found so there is no way for us to start reading.
            context.ModelState.TryAddModelError(
                context.ModelName,
                Resources.FormatInputFormatterNoEncoding(GetType().FullName));

            return null;
        }
    }
}