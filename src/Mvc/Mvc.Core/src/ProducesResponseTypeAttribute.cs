// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// A filter that specifies the type of the value and status code returned by the action.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class ProducesResponseTypeAttribute : Attribute, IApiResponseMetadataProvider
    {
        /// <summary>
        /// Initializes an instance of <see cref="ProducesResponseTypeAttribute"/>.
        /// </summary>       
        /// <param name="statusCode">The HTTP response status code.</param>
        public ProducesResponseTypeAttribute(int statusCode)
            : this(typeof(void), statusCode)
        {
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
        }

        /// <summary>
        /// Gets or sets the type of the value returned by an action.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code of the response.
        /// </summary>
        public int StatusCode { get; set; }

        /// <inheritdoc />
        void IApiResponseMetadataProvider.SetContentTypes(MediaTypeCollection contentTypes)
        {
            // Users are supposed to use the 'Produces' attribute to set the content types that an action can support.
        }
    }
}
