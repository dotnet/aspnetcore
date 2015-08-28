// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An <see cref="ActionResult"/> that returns a Created (201) response with a Location header.
    /// </summary>
    public class CreatedResult : ObjectResult
    {
        private string _location;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatedResult"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="location">The location at which the content has been created.</param>
        /// <param name="value">The value to format in the entity body.</param>
        public CreatedResult([NotNull] string location, object value)
            : base(value)
        {
            Location = location;
            StatusCode = StatusCodes.Status201Created;
        }

        /// <summary>
        /// Gets or sets the location at which the content has been created.
        /// </summary>
        public string Location
        {
            get
            {
                return _location;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _location = value;
            }
        }

        /// <inheritdoc />
        protected override void OnFormatting([NotNull] ActionContext context)
        {
            context.HttpContext.Response.Headers[HeaderNames.Location] = Location;
        }
    }
}