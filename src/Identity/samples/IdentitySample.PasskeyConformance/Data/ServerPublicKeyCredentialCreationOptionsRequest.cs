// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace IdentitySample.PasskeyConformance.Data;

internal sealed class ServerPublicKeyCredentialCreationOptionsRequest(string username, string displayName)
{
    public string Username { get; } = username;
    public string DisplayName { get; } = displayName;
    public AuthenticatorSelectionCriteria? AuthenticatorSelection { get; set; }
    public JsonElement? Extensions { get; set; }
    public string? Attestation { get; set; } = "none";
}
