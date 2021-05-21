// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Default implementation for <see cref="IHttpConnectionFeature"/>.
    /// </summary>
    public class HttpConnectionFeature : IHttpConnectionFeature
    {
        /// <inheritdoc />
        public string ConnectionId { get; set; } = default!;

        /// <inheritdoc />
        public IPAddress? LocalIpAddress { get; set; }

        /// <inheritdoc />
        public int LocalPort { get; set; }

        /// <inheritdoc />
        public IPAddress? RemoteIpAddress { get; set; }

        /// <inheritdoc />
        public int RemotePort { get; set; }
    }
}
