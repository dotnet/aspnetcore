// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.TestHost
{
    /// <summary>
    /// Options for the test server.
    /// </summary>
    public class TestServerOptions
    {
        /// <summary>
        /// Gets or sets a value that controls whether synchronous IO is allowed for the <see cref="HttpContext.Request"/> and <see cref="HttpContext.Response"/>. The default value is <see langword="false" />.
        /// </summary>
        public bool AllowSynchronousIO { get; set; }

        /// <summary>
        /// Gets or sets a value that controls if <see cref="ExecutionContext"/> and <see cref="AsyncLocal{T}"/> values are preserved from the client to the server. The default value is <see langword="false" />.
        /// </summary>
        public bool PreserveExecutionContext { get; set; }
    }
}
