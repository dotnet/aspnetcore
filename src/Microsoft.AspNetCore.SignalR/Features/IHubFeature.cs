// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR.Features
{
    public interface IHubFeature
    {
        IHubProtocol Protocol { get; set; }
    }

    public class HubFeature : IHubFeature
    {
        public IHubProtocol Protocol { get; set; }
    }
}
