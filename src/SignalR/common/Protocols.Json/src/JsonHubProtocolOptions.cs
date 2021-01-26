// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// Options used to configure a <see cref="JsonHubProtocol"/> instance.
    /// </summary>
    public class JsonHubProtocolOptions
    {
        /// <summary>
        /// Gets or sets the settings used to serialize invocation arguments and return values.
        /// </summary>
        public JsonSerializerOptions PayloadSerializerOptions { get; set; } = JsonHubProtocol.CreateDefaultSerializerSettings();
    }
}
