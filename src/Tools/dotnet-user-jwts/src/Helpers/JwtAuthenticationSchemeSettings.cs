// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

internal sealed record JwtAuthenticationSchemeSettings(string SchemeName, List<string> Audiences, string ClaimsIssuer)
{
    private const string AuthenticationKey = "Authentication";
    private const string SchemesKey = "Schemes";

    public void Save(string filePath)
    {
        using var reader = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var config = JsonSerializer.Deserialize<JsonObject>(reader, JwtSerializerOptions.Default);
        reader.Close();

        var settingsObject = new JsonObject
        {
            [nameof(TokenValidationParameters.ValidAudiences)] = new JsonArray(Audiences.Select(aud => JsonValue.Create(aud)).ToArray()),
            [nameof(TokenValidationParameters.ValidIssuer)] = ClaimsIssuer
        };

        if (config[AuthenticationKey] is JsonObject authentication)
        {
            if (authentication[SchemesKey] is JsonObject schemes)
            {
                // If a scheme with the same name has already been registered, we
                // override with the latest token's options
                schemes[SchemeName] = settingsObject;
            }
            else
            {
                authentication.Add(SchemesKey, new JsonObject
                {
                    [SchemeName] = settingsObject
                });
            }
        }
        else
        {
            config[AuthenticationKey] = new JsonObject
            {
                [SchemesKey] = new JsonObject
                {
                    [SchemeName] = settingsObject
                }
            };
        }

        var streamOptions = new FileStreamOptions { Access = FileAccess.Write, Mode = FileMode.Create };
        if (!OperatingSystem.IsWindows())
        {
            streamOptions.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }
        using var writer = new FileStream(filePath, streamOptions);
        JsonSerializer.Serialize(writer, config, JwtSerializerOptions.Default);
    }

    public static void RemoveScheme(string filePath, string name)
    {
        using var reader = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var config = JsonSerializer.Deserialize<JsonObject>(reader, JwtSerializerOptions.Default);
        reader.Close();

        if (config[AuthenticationKey] is JsonObject authentication &&
            authentication[SchemesKey] is JsonObject schemes)
        {
            schemes.Remove(name);
        }

        using var writer = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        JsonSerializer.Serialize(writer, config, JwtSerializerOptions.Default);
    }
}
