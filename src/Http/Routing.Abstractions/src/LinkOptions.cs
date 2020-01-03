// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing
{
    public class LinkOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether all generated paths URLs are lowercase.
        /// Use <see cref="LowercaseQueryStrings" /> to configure the behavior for query strings.
        /// </summary>
        public bool? LowercaseUrls { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a generated query strings are lowercase.
        /// This property will be unless <see cref="LowercaseUrls" /> is also <c>true</c>.
        /// </summary>
        public bool? LowercaseQueryStrings { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a trailing slash should be appended to the generated URLs.
        /// </summary>
        public bool? AppendTrailingSlash { get; set; }
    }
}
