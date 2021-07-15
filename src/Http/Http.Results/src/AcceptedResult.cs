// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Http.Result
{
    internal sealed class AcceptedResult : ObjectResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AcceptedResult"/> class with the values
        /// provided.
        /// </summary>
        public AcceptedResult()
            : base(value: null, StatusCodes.Status202Accepted)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcceptedResult"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="location">The location at which the status of requested content can be monitored.</param>
        /// <param name="value">The value to format in the entity body.</param>
        public AcceptedResult(string? location, object? value)
            : base(value, StatusCodes.Status202Accepted)
        {
            Location = location;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcceptedResult"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="locationUri">The location at which the status of requested content can be monitored.</param>
        /// <param name="value">The value to format in the entity body.</param>
        public AcceptedResult(Uri locationUri, object? value)
            : base(value, StatusCodes.Status202Accepted)
        {
            if (locationUri == null)
            {
                throw new ArgumentNullException(nameof(locationUri));
            }

            if (locationUri.IsAbsoluteUri)
            {
                Location = locationUri.AbsoluteUri;
            }
            else
            {
                Location = locationUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
            }
        }

        /// <summary>
        /// Gets or sets the location at which the status of the requested content can be monitored.
        /// </summary>
        public string? Location { get; set; }

        /// <inheritdoc />
        protected override void ConfigureResponseHeaders(HttpContext context)
        {
            if (!string.IsNullOrEmpty(Location))
            {
                context.Response.Headers.Location = Location;
            }
        }
    }
}
