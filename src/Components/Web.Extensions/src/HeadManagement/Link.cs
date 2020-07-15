// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// A component that adds a link tag to the HTML head.
    /// </summary>
    public class Link : HeadTagBase
    {
        /// <summary>
        /// Instantiates a new <see cref="Link"/> instance.
        /// </summary>
        public Link() : base("link")
        {
        }
    }
}
