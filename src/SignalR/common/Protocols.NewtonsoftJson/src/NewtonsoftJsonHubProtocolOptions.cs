// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR.Protocol;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// Options used to configure a <see cref="NewtonsoftJsonHubProtocol"/> instance.
/// </summary>
public class NewtonsoftJsonHubProtocolOptions
{
    /// <summary>
    /// Gets or sets the settings used to serialize invocation arguments and return values.
    /// </summary>
    public JsonSerializerSettings PayloadSerializerSettings { get; set; } = NewtonsoftJsonHubProtocol.CreateDefaultSerializerSettings();
}
