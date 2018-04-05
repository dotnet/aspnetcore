// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    public interface IHubProtocolResolver
    {
        IReadOnlyList<IHubProtocol> AllProtocols { get; }
        IHubProtocol GetProtocol(string protocolName, IList<string> supportedProtocols);
    }
}
