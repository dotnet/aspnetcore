// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR.Internal;

namespace Microsoft.AspNetCore.SignalR.Features
{
    public interface IHubFeature
    {
        HubProtocolReaderWriter ProtocolReaderWriter { get; set; }
    }

    public class HubFeature : IHubFeature
    {
        public HubProtocolReaderWriter ProtocolReaderWriter { get; set; }
    }
}
