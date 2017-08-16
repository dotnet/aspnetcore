// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Tls
{
    internal class TlsApplicationProtocolFeature : ITlsApplicationProtocolFeature
    {
        public TlsApplicationProtocolFeature(string applicationProtocol)
        {
            ApplicationProtocol = applicationProtocol;
        }

        public string ApplicationProtocol { get; }
    }
}
