// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Http.Connections
{
    public class ConnectionOptions
    {
        /// <summary>
        /// Gets or sets the interval used by the server to timeout idle connections.
        /// </summary>
        public TimeSpan? DisconnectTimeout { get; set; }
    }
}
