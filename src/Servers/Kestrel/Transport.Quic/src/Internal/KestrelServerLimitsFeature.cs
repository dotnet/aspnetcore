// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
{
    internal class KestrelServerLimitsFeature
    {
        public TimeSpan KeepAliveTimeout { get; set; }
    }
}
