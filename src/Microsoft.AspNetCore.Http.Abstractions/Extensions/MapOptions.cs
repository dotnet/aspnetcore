// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Builder.Extensions
{
    /// <summary>
    /// Options for the <see cref="MapMiddleware"/>.
    /// </summary>
    public class MapOptions
    {
        /// <summary>
        /// The path to match.
        /// </summary>
        public PathString PathMatch { get; set; }

        /// <summary>
        /// The branch taken for a positive match.
        /// </summary>
        public RequestDelegate Branch { get; set; }
    }
}