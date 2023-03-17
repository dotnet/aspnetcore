// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Identity.Endpoints.DTO;

internal sealed class AccessTokenResponse
{
    [JsonPropertyName("token_type")]
    public string TokenType { get; } = "Bearer";

    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("expires_in")]
    public required double ExpiresInTotalSeconds { get; init; }

    // TODO: [JsonPropertyName("refresh_token")]
    // public required string RefreshToken { get; init; }
}
