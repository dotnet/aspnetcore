// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Diagnostics
{
    /// <summary>
    /// Options for the RuntimeInfoPage
    /// </summary>
    public class RuntimeInfoPageOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeInfoPageOptions" /> class
        /// </summary>
        public RuntimeInfoPageOptions()
        {
            Path = new PathString("/runtimeinfo");
        }

        /// <summary>
        /// Specifies which request path will be responded to. Exact match only. Set to null to handle all requests.
        /// </summary>
        public PathString Path { get; set; }
    }
}