// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    /// <summary>
    /// The behavior for matching the name of a convention parameter.
    /// </summary>
    public enum ApiConventionTypeMatchBehavior
    {
        /// <summary>
        /// Matches any type. Use this if the parameter does not need to be matched.
        /// </summary>
        Any,

        /// <summary>
        /// The parameter in the convention is the exact type or a subclass of the type
        /// specified in the convention.
        /// </summary>
        AssignableFrom,
    }
}