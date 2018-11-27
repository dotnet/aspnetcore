// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    /// <summary>
    /// Determines the matching behavior an API convention parameter by type. 
    /// <see cref="ApiConventionTypeMatchBehavior"/> for supported options.
    /// <seealso cref="ApiConventionTypeAttribute"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="ApiConventionTypeMatchBehavior.AssignableFrom"/> is used if no value for this
    /// attribute is specified on a convention parameter.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class ApiConventionTypeMatchAttribute : Attribute
    {
        public ApiConventionTypeMatchAttribute(ApiConventionTypeMatchBehavior matchBehavior)
        {
            MatchBehavior = matchBehavior;
        }

        public ApiConventionTypeMatchBehavior MatchBehavior { get; }
    }
}
