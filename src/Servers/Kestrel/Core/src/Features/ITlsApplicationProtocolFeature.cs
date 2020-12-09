// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features
{
    /// <summary>
    /// Feature to set access the TLS application protocol
    /// </summary>
    public interface ITlsApplicationProtocolFeature
    {
        /// <summary>
        /// Gets the <see cref="ReadOnlyMemory{T}"/> represeting the application protocol.
        /// </summary>
        ReadOnlyMemory<byte> ApplicationProtocol { get; }
    }
}
