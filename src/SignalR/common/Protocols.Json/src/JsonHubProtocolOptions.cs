// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR;

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
