// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features
{
    // TODO: this should be merged with ITlsConnectionFeature
    public interface ITlsApplicationProtocolFeature
    {
        string ApplicationProtocol { get; }
    }
}
