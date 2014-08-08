// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        /// <summary>
        /// Gets the mutable collection of character encodings supported by
        /// this <see cref="OutputFormatter"/> instance. The encodings are
        /// used when writing the data.
        /// </summary>
        public IList<Encoding> SupportedEncodings { get; private set; }

        /// <summary>
        /// Gets the mutable collection of <see cref="MediaTypeHeaderValue"/> elements supported by
        /// this <see cref="OutputFormatter"/> instance.
        /// </summary>
        public IList<MediaTypeHeaderValue> SupportedMediaTypes { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputFormatter"/> class.
        /// </summary>
        protected OutputFormatter()
        {
            SupportedEncodings = new List<Encoding>();
            SupportedMediaTypes = new List<MediaTypeHeaderValue>();
        }

        /// <summary>
        /// Determines the best <see cref="Encoding"/> amongst the supported encodings
        /// for reading or writing an HTTP entity body based on the provided <paramref name="contentTypeHeader"/>.
        /// </summary>
        /// <param name="context">The formatter context associated with the call.
        /// </param>
        /// <returns>The <see cref="Encoding"/> to use when reading the request or writing the response.</returns>
        public virtual Encoding SelectCharacterEncoding(OutputFormatterContext context)
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
        public virtual bool CanWriteResult(OutputFormatterContext context, MediaTypeHeaderValue contentType)
        {
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
        public async Task WriteAsync(OutputFormatterContext context)
        {
            WriteResponseContentHeaders(context);
            await WriteResponseBodyAsync(context);
        }

        /// <summary>
        /// Sets the content-type headers with charset value to the HttpResponse.
        /// </summary>
        /// <param name="context">The formatter context associated with the call.</param>
        public virtual void WriteResponseContentHeaders(OutputFormatterContext context)
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
        public abstract Task WriteResponseBodyAsync(OutputFormatterContext context);

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
