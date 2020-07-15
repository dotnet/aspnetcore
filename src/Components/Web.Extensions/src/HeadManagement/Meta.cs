// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// A component that adds a meta tag to the HTML head.
    /// </summary>
    public class Meta : HeadTagBase
    {
        /// <summary>
        /// Instantiates a new <see cref="Meta"/> instance.
        /// </summary>
        public Meta() : base("meta")
        {
        }
    }
}
