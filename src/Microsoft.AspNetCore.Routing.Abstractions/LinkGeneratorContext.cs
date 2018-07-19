// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    public class LinkGeneratorContext
    {
        public HttpContext HttpContext { get; set; }

        public IEnumerable<Endpoint> Endpoints { get; set; }

        public RouteValueDictionary ExplicitValues { get; set; }

        public RouteValueDictionary AmbientValues { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether all generated paths URLs are lower-case.
        /// Use <see cref="LowercaseQueryStrings" /> to configure the behavior for query strings.
        /// </summary>
        public bool? LowercaseUrls { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a generated query strings are lower-case.
        /// This property will be unless <see cref="LowercaseUrls" /> is also <c>true</c>.
        /// </summary>
        public bool? LowercaseQueryStrings { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a trailing slash should be appended to the generated URLs.
        /// </summary>
        public bool? AppendTrailingSlash { get; set; }
    }
}
