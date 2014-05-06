// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

#if DEBUG

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Diagnostics
{
    /// <summary>
    /// Options for the DiagnosticsPageMiddleware
    /// </summary>
    public class DiagnosticsPageOptions
    {
        /// <summary>
        /// Specifies which requests paths will be responded to. Exact matches only. Leave null to handle all requests.
        /// </summary>
        public PathString Path { get; set; }
    }
}
#endif