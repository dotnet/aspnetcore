// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Http.Result
{
    internal sealed class CreatedResult : ObjectResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreatedResult"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="location">The location at which the content has been created.</param>
        /// <param name="value">The value to format in the entity body.</param>
        public CreatedResult(string location, object? value)
            : base(value, StatusCodes.Status201Created)
        {
            Location = location;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatedResult"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="location">The location at which the content has been created.</param>
        /// <param name="value">The value to format in the entity body.</param>
        public CreatedResult(Uri location, object? value)
            : base(value, StatusCodes.Status201Created)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (location.IsAbsoluteUri)
            {
                Location = location.AbsoluteUri;
            }
            else
            {
                Location = location.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
            }
        }

        /// <summary>
        /// Gets or sets the location at which the content has been created.
        /// </summary>
        public string Location { get; init; }

        /// <inheritdoc />
        protected override void ConfigureResponseHeaders(HttpContext context)
        {
            context.Response.Headers.Location = Location;
        }
    }
}
