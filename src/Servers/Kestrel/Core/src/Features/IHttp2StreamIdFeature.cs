// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features
{
    /// <summary>
    /// The stream id for a given stream in an HTTP/2 connection.
    /// </summary>
    public interface IHttp2StreamIdFeature
    {
        /// <summary>
        /// Gets the id for the HTTP/2 stream.
        /// </summary>
        int StreamId { get; }
    }
}
