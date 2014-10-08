// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Writes an object to the output stream.
    /// </summary>
    public abstract class OutputFormatter : IOutputFormatter
    {
        // using a field so we can return it as both IList and IReadOnlyList
        private readonly List<MediaTypeHeaderValue> _supportedMediaTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputFormatter"/> class.
        /// </summary>
        protected OutputFormatter()
        {
            SupportedEncodings = new List<Encoding>();
            _supportedMediaTypes = new List<MediaTypeHeaderValue>();
        }

        /// <summary>
        /// Gets the mutable collection of character encodings supported by
        /// this <see cref="OutputFormatter"/>. The encodings are
        /// used when writing the data.
        /// </summary>
        public IList<Encoding> SupportedEncodings { get; private set; }

        /// <summary>
        /// Gets the mutable collection of <see cref="MediaTypeHeaderValue"/> elements supported by
        /// this <see cref="OutputFormatter"/>.
        /// </summary>
        public IList<MediaTypeHeaderValue> SupportedMediaTypes
        {
            get { return _supportedMediaTypes; }
        }

        /// <summary>
        /// Returns a value indicating whether or not the given type can be written by this serializer.
        /// </summary>
        /// <param name="declaredType">The declared type.</param>
        /// <param name="runtimeType">The runtime type.</param>
        /// <returns><c>true</c> if the type can be written, otherwise <c>false</c>.</returns>
        protected virtual bool CanWriteType(Type declaredType, Type runtimeType)
        {
            return true;
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<MediaTypeHeaderValue> GetSupportedContentTypes(
            Type declaredType,
            Type runtimeType,
            MediaTypeHeaderValue contentType)
        {
            if (!CanWriteType(declaredType, runtimeType))
            {
                return null;
            }

            if (contentType == null)
            {
                // If contentType is null, then any type we support is valid.
                return _supportedMediaTypes.Count > 0 ? _supportedMediaTypes : null;
            }
            else
            {
                List<MediaTypeHeaderValue> mediaTypes = null;

                foreach (var mediaType in _supportedMediaTypes)
                {
                    if (mediaType.IsSubsetOf(contentType))
                    {
                        if (mediaTypes == null)
                        {
                            mediaTypes = new List<MediaTypeHeaderValue>();
                        }

                        mediaTypes.Add(mediaType);
                    }
                }

                return mediaTypes;
            }
        }

        /// <summary>
        /// Determines the best <see cref="Encoding"/> amongst the supported encodings
        /// for reading or writing an HTTP entity body based on the provided <paramref name="contentTypeHeader"/>.
        /// </summary>
        /// <param name="context">The formatter context associated with the call.
        /// </param>
        /// <returns>The <see cref="Encoding"/> to use when reading the request or writing the response.</returns>
        public virtual Encoding SelectCharacterEncoding([NotNull] OutputFormatterContext context)
        {
            var request = context.ActionContext.HttpContext.Request;
            var encoding = MatchAcceptCharacterEncoding(request.AcceptCharset);
            if (encoding == null)
            {
                // Match based on request acceptHeader.
                MediaTypeHeaderValue requestContentType = null;
                if (MediaTypeHeaderValue.TryParse(request.ContentType, out requestContentType) &&
                    !string.IsNullOrEmpty(requestContentType.Charset))
                {
                    var requestCharset = requestContentType.Charset;
                    encoding = SupportedEncodings.FirstOrDefault(
                                                            supportedEncoding =>
                                                                requestCharset.Equals(supportedEncoding.WebName));
                }
            }

            encoding = encoding ?? SupportedEncodings.FirstOrDefault();
            return encoding;
        }

        /// <inheritdoc />
        public virtual bool CanWriteResult([NotNull] OutputFormatterContext context, MediaTypeHeaderValue contentType)
        {
            var runtimeType = context.Object == null ? null : context.Object.GetType();
            if (!CanWriteType(context.DeclaredType, runtimeType))
            {
                return false;
            }

            MediaTypeHeaderValue mediaType = null;
            if (contentType == null)
            {
                // If the desired content type is set to null, the current formatter is free to choose the 
                // response media type. 
                mediaType = SupportedMediaTypes.FirstOrDefault();
            }
            else
            {
                // Since supportedMedia Type is going to be more specific check if supportedMediaType is a subset
                // of the content type which is typically what we get on acceptHeader.
                mediaType = SupportedMediaTypes
                                  .FirstOrDefault(supportedMediaType => supportedMediaType.IsSubsetOf(contentType));
            }

            if (mediaType != null)
            {
                context.SelectedContentType = mediaType;
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public async Task WriteAsync([NotNull] OutputFormatterContext context)
        {
            WriteResponseHeaders(context);
            await WriteResponseBodyAsync(context);
        }

        /// <summary>
        /// Sets the headers on <see cref="Microsoft.AspNet.Http.HttpResponse"/> object.
        /// </summary>
        /// <param name="context">The formatter context associated with the call.</param>
        public virtual void WriteResponseHeaders([NotNull] OutputFormatterContext context)
        {
            var selectedMediaType = context.SelectedContentType;

            // If content type is not set then set it based on supported media types.
            selectedMediaType = selectedMediaType ?? SupportedMediaTypes.FirstOrDefault();
            if (selectedMediaType == null)
            {
                throw new InvalidOperationException(Resources.FormatOutputFormatterNoMediaType(GetType().FullName));
            }

            var selectedEncoding = SelectCharacterEncoding(context);
            if (selectedEncoding == null)
            {
                // No supported encoding was found so there is no way for us to start writing.
                throw new InvalidOperationException(Resources.FormatOutputFormatterNoEncoding(GetType().FullName));
            }

            context.SelectedEncoding = selectedEncoding;

            // Override the content type value even if one already existed. 
            selectedMediaType.Charset = selectedEncoding.WebName;

            context.SelectedContentType = context.SelectedContentType ?? selectedMediaType;
            var response = context.ActionContext.HttpContext.Response;
            response.ContentType = selectedMediaType.RawValue;
        }

        /// <summary>
        /// Writes the response body.
        /// </summary>
        /// <param name="context">The formatter context associated with the call.</param>
        /// <returns>A task which can write the response body.</returns>
        public abstract Task WriteResponseBodyAsync([NotNull] OutputFormatterContext context);

        private Encoding MatchAcceptCharacterEncoding(string acceptCharsetHeader)
        {
            var acceptCharsetHeaders = HeaderParsingHelpers
                                                .GetAcceptCharsetHeaders(acceptCharsetHeader);

            if (acceptCharsetHeaders != null && acceptCharsetHeaders.Count > 0)
            {
                var sortedAcceptCharsetHeaders = acceptCharsetHeaders
                                                    .Where(acceptCharset =>
                                                                acceptCharset.Quality != HttpHeaderUtilitites.NoMatch)
                                                    .OrderByDescending(
                                                        m => m, StringWithQualityHeaderValueComparer.QualityComparer);

                foreach (var acceptCharset in sortedAcceptCharsetHeaders)
                {
                    var charset = acceptCharset.Value;
                    if (!string.IsNullOrWhiteSpace(charset))
                    {
                        var encoding = SupportedEncodings.FirstOrDefault(
                                                        supportedEncoding =>
                                                            charset.Equals(supportedEncoding.WebName,
                                                                           StringComparison.OrdinalIgnoreCase) ||
                                                            charset.Equals("*", StringComparison.OrdinalIgnoreCase));
                        if (encoding != null)
                        {
                            return encoding;
                        }
                    }
                }
            }

            return null;
        }
    }
}
