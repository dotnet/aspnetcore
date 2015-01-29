// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http.Headers;

namespace System.Net.Http.Formatting
{
    /// <summary>
    /// Constants related to media types.
    /// </summary>
    internal static class MediaTypeConstants
    {
        private static readonly MediaTypeHeaderValue _defaultApplicationXmlMediaType =
            new MediaTypeHeaderValue("application/xml");
        private static readonly MediaTypeHeaderValue _defaultTextXmlMediaType =
            new MediaTypeHeaderValue("text/xml");
        private static readonly MediaTypeHeaderValue _defaultApplicationJsonMediaType =
            new MediaTypeHeaderValue("application/json");
        private static readonly MediaTypeHeaderValue _defaultTextJsonMediaType =
            new MediaTypeHeaderValue("text/json");
        private static readonly MediaTypeHeaderValue _defaultApplicationOctetStreamMediaType =
            new MediaTypeHeaderValue("application/octet-stream");
        private static readonly MediaTypeHeaderValue _defaultApplicationFormUrlEncodedMediaType =
            new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        private static readonly MediaTypeHeaderValue _defaultApplicationBsonMediaType =
            new MediaTypeHeaderValue("application/bson");

        /// <summary>
        /// Gets a <see cref="MediaTypeHeaderValue"/> instance representing <c>application/octet-stream</c>.
        /// </summary>
        /// <value>
        /// A new <see cref="MediaTypeHeaderValue"/> instance representing <c>application/octet-stream</c>.
        /// </value>
        public static MediaTypeHeaderValue ApplicationOctetStreamMediaType
        {
            get { return _defaultApplicationOctetStreamMediaType; }
        }

        /// <summary>
        /// Gets a <see cref="MediaTypeHeaderValue"/> instance representing <c>application/xml</c>.
        /// </summary>
        /// <value>
        /// A new <see cref="MediaTypeHeaderValue"/> instance representing <c>application/xml</c>.
        /// </value>
        public static MediaTypeHeaderValue ApplicationXmlMediaType
        {
            get { return _defaultApplicationXmlMediaType; }
        }

        /// <summary>
        /// Gets a <see cref="MediaTypeHeaderValue"/> instance representing <c>application/json</c>.
        /// </summary>
        /// <value>
        /// A new <see cref="MediaTypeHeaderValue"/> instance representing <c>application/json</c>.
        /// </value>
        public static MediaTypeHeaderValue ApplicationJsonMediaType
        {
            get { return _defaultApplicationJsonMediaType; }
        }

        /// <summary>
        /// Gets a <see cref="MediaTypeHeaderValue"/> instance representing <c>text/xml</c>.
        /// </summary>
        /// <value>
        /// A new <see cref="MediaTypeHeaderValue"/> instance representing <c>text/xml</c>.
        /// </value>
        public static MediaTypeHeaderValue TextXmlMediaType
        {
            get { return _defaultTextXmlMediaType; }
        }

        /// <summary>
        /// Gets a <see cref="MediaTypeHeaderValue"/> instance representing <c>text/json</c>.
        /// </summary>
        /// <value>
        /// A new <see cref="MediaTypeHeaderValue"/> instance representing <c>text/json</c>.
        /// </value>
        public static MediaTypeHeaderValue TextJsonMediaType
        {
            get { return _defaultTextJsonMediaType; }
        }

        /// <summary>
        /// Gets a <see cref="MediaTypeHeaderValue"/> instance representing <c>application/x-www-form-urlencoded</c>.
        /// </summary>
        /// <value>
        /// A new <see cref="MediaTypeHeaderValue"/> instance representing <c>application/x-www-form-urlencoded</c>.
        /// </value>
        public static MediaTypeHeaderValue ApplicationFormUrlEncodedMediaType
        {
            get { return _defaultApplicationFormUrlEncodedMediaType; }
        }

        /// <summary>
        /// Gets a <see cref="MediaTypeHeaderValue"/> instance representing <c>application/bson</c>.
        /// </summary>
        /// <value>
        /// A new <see cref="MediaTypeHeaderValue"/> instance representing <c>application/bson</c>.
        /// </value>
        /// <remarks>
        /// Not yet a standard. In particular this media type is not currently listed at
        /// http://www.iana.org/assignments/media-types/application.
        /// </remarks>
        public static MediaTypeHeaderValue ApplicationBsonMediaType
        {
            get { return _defaultApplicationBsonMediaType; }
        }
    }
}
