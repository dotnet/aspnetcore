// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ApiExplorer;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.Formatters
{
    /// <summary>
    /// Writes an object to the output stream.
    /// </summary>
    public abstract class OutputFormatter : IOutputFormatter, IApiResponseFormatMetadataProvider
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
        public IList<Encoding> SupportedEncodings { get; }

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
        /// <param name="type">The object type.</param>
        /// <returns><c>true</c> if the type can be written, otherwise <c>false</c>.</returns>
        protected virtual bool CanWriteType(Type type)
        {
            return true;
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<MediaTypeHeaderValue> GetSupportedContentTypes(
            MediaTypeHeaderValue contentType,
            Type objectType)
        {
            if (!CanWriteType(objectType))
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

                // Confirm this formatter supports a more specific media type than requested e.g. OK if "text/*"
                // requested and formatter supports "text/plain". Treat contentType like it came from an Accept header.
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
        public virtual Encoding SelectCharacterEncoding(OutputFormatterWriteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var request = context.HttpContext.Request;
            var encoding = MatchAcceptCharacterEncoding(request.GetTypedHeaders().AcceptCharset);
            if (encoding != null)
            {
                return encoding;
            }

            var charset = context.ContentType?.Charset;
            if (charset != null)
            {
                for (var i = 0; i < SupportedEncodings.Count; i++)
                {
                    if (string.Equals(charset, SupportedEncodings[i].WebName, StringComparison.OrdinalIgnoreCase))
                    {
                        // This is supported.
                        return context.ContentType.Encoding;
                    }
                }
            }

            // A formatter for a non-text media-type won't have any supported encodings.
            return SupportedEncodings.Count > 0 ? SupportedEncodings[0] : null;
        }

        /// <inheritdoc />
        public virtual bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            
            if (!CanWriteType(context.ObjectType))
            {
                return false;
            }
            
            if (context.ContentType == null)
            {
                // If the desired content type is set to null, then the current formatter can write anything
                // it wants.
                if (SupportedMediaTypes.Count > 0)
                {
                    context.ContentType = SupportedMediaTypes[0];
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                // Confirm this formatter supports a more specific media type than requested e.g. OK if "text/*"
                // requested and formatter supports "text/plain". contentType is typically what we got in an Accept
                // header.
                for (var i = 0; i < SupportedMediaTypes.Count; i++)
                {
                    var mediaType = SupportedMediaTypes[i];
                    if (mediaType.IsSubsetOf(context.ContentType))
                    {
                        context.ContentType = mediaType;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc />
        public Task WriteAsync(OutputFormatterWriteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var selectedMediaType = context.ContentType;
            if (selectedMediaType == null)
            {
                // If content type is not set then set it based on supported media types.
                if (SupportedEncodings.Count > 0)
                {
                    selectedMediaType = SupportedMediaTypes[0];
                }
                else
                {
                    throw new InvalidOperationException(Resources.FormatOutputFormatterNoMediaType(GetType().FullName));
                }
            }

            // Copy the media type as it may be a 'frozen' instance.
            selectedMediaType = selectedMediaType.Copy();

            // Note: Text-based media types will use an encoding/charset - binary formats just ignore it. We want to
            // make this class work with media types that use encodings, and those that don't.
            //
            // The default implementation of SelectCharacterEncoding will read from the list of SupportedEncodings
            // and will always choose a default encoding if any are supported. So, the only cases where the
            // selectedEncoding can be null are:
            //
            // 1). No supported encodings - we assume this is a non-text format
            // 2). Custom implementation of SelectCharacterEncoding - trust the user and give them what they want.
            var selectedEncoding = SelectCharacterEncoding(context);
            if (selectedEncoding != null)
            {
                // Override the content type value even if one already existed.
                selectedMediaType.Encoding = selectedEncoding;
            }

            context.ContentType = selectedMediaType;

            WriteResponseHeaders(context);
            return WriteResponseBodyAsync(context);
        }

        /// <summary>
        /// Sets the headers on <see cref="Microsoft.AspNet.Http.HttpResponse"/> object.
        /// </summary>
        /// <param name="context">The formatter context associated with the call.</param>
        public virtual void WriteResponseHeaders(OutputFormatterWriteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var response = context.HttpContext.Response;
            response.ContentType = context.ContentType?.ToString();
        }

        /// <summary>
        /// Writes the response body.
        /// </summary>
        /// <param name="context">The formatter context associated with the call.</param>
        /// <returns>A task which can write the response body.</returns>
        public abstract Task WriteResponseBodyAsync(OutputFormatterWriteContext context);

        private Encoding MatchAcceptCharacterEncoding(IList<StringWithQualityHeaderValue> acceptCharsetHeaders)
        {
            if (acceptCharsetHeaders != null && acceptCharsetHeaders.Count > 0)
            {
                var acceptValues = Sort(acceptCharsetHeaders);
                for (var i = 0; i < acceptValues.Count; i++)
                {
                    var charset = acceptValues[i].Value;
                    if (!string.IsNullOrEmpty(charset))
                    {
                        for (var j = 0; j < SupportedEncodings.Count; j++)
                        {
                            var encoding = SupportedEncodings[j];
                            if (charset.Equals(encoding.WebName, StringComparison.OrdinalIgnoreCase) ||
                                charset.Equals("*", StringComparison.Ordinal))
                            {
                                return encoding;
                            }
                        }
                    }
                }
            }

            return null;
        }

        // There's no allocation-free way to sort an IList and we may have to filter anyway,
        // so we're going to have to live with the copy + insertion sort.
        private IList<StringWithQualityHeaderValue> Sort(IList<StringWithQualityHeaderValue> values)
        {
            var sortNeeded = false;
            var count = 0;

            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                if (value.Quality == HeaderQuality.NoMatch)
                {
                    // Exclude this one
                }
                else if (value.Quality != null)
                {
                    count++;
                    sortNeeded = true;
                }
                else
                {
                    count++;
                }
            }

            if (!sortNeeded)
            {
                return values;
            }

            var sorted = new List<StringWithQualityHeaderValue>();
            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                if (value.Quality == HeaderQuality.NoMatch)
                {
                    // Exclude this one
                }
                else
                {
                    // Doing an insertion sort.
                    var position = sorted.BinarySearch(value, StringWithQualityHeaderValueComparer.QualityComparer);
                    if (position >= 0)
                    {
                        sorted.Insert(position + 1, value);
                    }
                    else
                    {
                        sorted.Insert(~position, value);
                    }
                }
            }

            // We want a descending sort, but BinarySearch does ascending
            sorted.Reverse();
            return sorted;
        }
    }
}
