// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
