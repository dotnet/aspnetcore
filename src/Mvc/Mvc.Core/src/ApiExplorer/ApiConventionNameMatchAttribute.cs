// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    /// <summary>
    /// Determines the matching behavior an API convention method or parameter by name. 
    /// <see cref="ApiConventionNameMatchBehavior"/> for supported options. 
    /// <seealso cref="ApiConventionTypeAttribute"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="ApiConventionNameMatchBehavior.Exact"/> is used if no value for this
    /// attribute is specified on a convention method or parameter.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class ApiConventionNameMatchAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ApiConventionNameMatchAttribute"/>.
        /// </summary>
        /// <param name="matchBehavior">The <see cref="ApiConventionNameMatchBehavior"/>.</param>
        public ApiConventionNameMatchAttribute(ApiConventionNameMatchBehavior matchBehavior)
        {
            MatchBehavior = matchBehavior;
        }

        /// <summary>
        /// Gets the <see cref="ApiConventionNameMatchBehavior"/>.
        /// </summary>
        public ApiConventionNameMatchBehavior MatchBehavior { get; }
    }
}
