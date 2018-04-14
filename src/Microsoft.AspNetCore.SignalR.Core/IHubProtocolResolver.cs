// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IHubProtocolResolver
    {
        IReadOnlyList<IHubProtocol> AllProtocols { get; }
        IHubProtocol GetProtocol(string protocolName, IReadOnlyList<string> supportedProtocols);
    }
}
