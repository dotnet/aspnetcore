// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// A filter that specifies the type of the value and status code returned by the action.
    /// </summary>
    internal class ProducesResponseTypeAttribute : IProducesResponseTypeMetadata
    {
        private readonly MediaTypeCollection? _contentTypes;

        /// <summary>
        /// Initializes an instance of <see cref="ProducesResponseTypeAttribute"/>.
        /// </summary>
        /// <param name="statusCode">The HTTP response status code.</param>
        public ProducesResponseTypeAttribute(int statusCode)
            : this(typeof(void), statusCode)
        {
            IsResponseTypeSetByDefault = true;
        }

        /// <summary>
        /// Initializes an instance of <see cref="ProducesResponseTypeAttribute"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of object that is going to be written in the response.</param>
        /// <param name="statusCode">The HTTP response status code.</param>
        public ProducesResponseTypeAttribute(Type type, int statusCode)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            StatusCode = statusCode;
            IsResponseTypeSetByDefault = false;
        }

        /// <summary>
        /// Initializes an instance of <see cref="ProducesResponseTypeAttribute"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of object that is going to be written in the response.</param>
        /// <param name="statusCode">The HTTP response status code.</param>
        /// <param name="contentType">The content type associated with the response.</param>
        /// <param name="additionalContentTypes">Additional content types supported by the response.</param>
        public ProducesResponseTypeAttribute(Type type, int statusCode, string contentType, params string[] additionalContentTypes)
        {
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            Type = type ?? throw new ArgumentNullException(nameof(type));
            StatusCode = statusCode;
            IsResponseTypeSetByDefault = false;

            MediaTypeHeaderValue.Parse(contentType);
            for (var i = 0; i < additionalContentTypes.Length; i++)
            {
                MediaTypeHeaderValue.Parse(additionalContentTypes[i]);
            }

            _contentTypes = GetContentTypes(contentType, additionalContentTypes);
        }

        /// <summary>
        /// Gets or sets the type of the value returned by an action.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code of the response.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Used to distinguish a `Type` set by default in the constructor versus
        /// one provided by the user.
        ///
        /// When <see langword="false"/>, then <see cref="Type"/> is set by user.
        ///
        /// When <see langword="true"/>, then <see cref="Type"/> is set by by
        /// default in the constructor
        /// </summary>
        /// <value></value>
        internal bool IsResponseTypeSetByDefault { get; }

        // Internal for testing
        internal MediaTypeCollection? ContentTypes => _contentTypes;

        void IProducesResponseTypeMetadata.SetContentTypes(MediaTypeCollection contentTypes)
        {
            if (_contentTypes is not null)
            {
                contentTypes.Clear();
                foreach (var contentType in _contentTypes)
                {
                    contentTypes.Add(contentType);
                }
            }
        }

        private static MediaTypeCollection GetContentTypes(string contentType, string[] additionalContentTypes)
        {
            var completeContentTypes = new List<string>(additionalContentTypes.Length + 1);
            completeContentTypes.Add(contentType);
            completeContentTypes.AddRange(additionalContentTypes);
            MediaTypeCollection contentTypes = new();
            foreach (var type in completeContentTypes)
            {
                if (type.Contains('*', StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Could not parse '{type}'. Content types with wildcards are not supported.");
                }

                contentTypes.Add(type);
            }

            return contentTypes;
        }
    }
}
