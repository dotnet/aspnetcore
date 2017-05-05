// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http.Headers;

namespace System.Net.Http.Formatting
{
    /// <summary>
    /// Represents the result of content negotiation performed using
    /// <see cref="IContentNegotiator.Negotiate(Type, HttpRequestMessage, IEnumerable{MediaTypeFormatter})"/>
    /// </summary>
    public class ContentNegotiationResult
    {
        private MediaTypeFormatter _formatter;

        /// <summary>
        /// Create the content negotiation result object.
        /// </summary>
        /// <param name="formatter">The formatter.</param>
        /// <param name="mediaType">The preferred media type. Can be <c>null</c>.</param>
        public ContentNegotiationResult(MediaTypeFormatter formatter, MediaTypeHeaderValue mediaType)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            _formatter = formatter;
            MediaType = mediaType;
        }

        /// <summary>
        /// The formatter chosen for serialization.
        /// </summary>
        public MediaTypeFormatter Formatter
        {
            get { return _formatter; }
            set
            {
                _formatter = value;
            }
        }

        /// <summary>
        /// The media type that is associated with the formatter chosen for serialization. Can be <c>null</c>.
        /// </summary>
        public MediaTypeHeaderValue MediaType { get; set; }
    }
}
