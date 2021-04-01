// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// Specifies the default status code associated with an <see cref="ActionResult"/>.
    /// </summary>
    /// <remarks>
    /// This attribute is informational only and does not have any runtime effects.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class DefaultStatusCodeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DefaultStatusCodeAttribute"/>.
        /// </summary>
        /// <param name="statusCode">The default status code.</param>
        public DefaultStatusCodeAttribute(int statusCode)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Gets the default status code.
        /// </summary>
        public int StatusCode { get; }
    }
}
