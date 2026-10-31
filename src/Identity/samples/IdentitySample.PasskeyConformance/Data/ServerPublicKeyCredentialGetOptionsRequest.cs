// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace IdentitySample.PasskeyConformance.Data;

internal sealed class ServerPublicKeyCredentialGetOptionsRequest(string username, string userVerification)
{
    public string Username { get; } = username;
    public string UserVerification { get; } = userVerification;
    public JsonElement? Extensions { get; set; }
}
