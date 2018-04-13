// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR.Protocol;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR
{
    public class JsonHubProtocolOptions
    {
        public JsonSerializerSettings PayloadSerializerSettings { get; set; } = JsonHubProtocol.CreateDefaultSerializerSettings();
    }
}
