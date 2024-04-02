// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

// This class manages signing keys for JWT tokens stored in user secrets.
// These signing keys are stored under the following configuration scheme:
//
//   Authentication:
//     Schemes:
//          Bearer:
//              SigningKeys: [{
//                  Id: abcdefghi,
//                  Value: somekeyhere,
//                  Issuer: someissuer,
//                  Length: 32
//              },
//              {
//                  Id: ihgfedcba,
//                  Value: somekeyhere,
//                  Issuer: someissuer2,
//                  Length: 32
//              }]
internal static class SigningKeysHandler
{
    public static byte[] GetSigningKeyMaterial(string userSecretsId, string scheme, string issuer)
    {
        var projectConfiguration = new ConfigurationBuilder()
            .AddUserSecrets(userSecretsId)
            .Build();

        var signingKey = projectConfiguration
            .GetSection(GetSigningKeyPropertyName(scheme))
            .Get<SigningKey[]>()
            ?.SingleOrDefault(key => key.Issuer == issuer);
        var signingKeyLength = signingKey?.Length ?? DevJwtsDefaults.SigningKeyLength;

        var keyMaterial = new byte[signingKeyLength];
        if (!string.IsNullOrEmpty(signingKey?.Value)
            && Convert.TryFromBase64String(signingKey.Value, keyMaterial, out var bytesWritten)
            && bytesWritten == signingKeyLength)
        {
            return keyMaterial;
        }

        return null;
    }

    public static byte[] GetOrCreateSigningKeyMaterial(string userSecretsId, string scheme, string issuer) =>
        GetSigningKeyMaterial(userSecretsId, scheme, issuer) ?? CreateSigningKeyMaterial(userSecretsId, scheme, issuer);

    public static byte[] CreateSigningKeyMaterial(string userSecretsId, string scheme, string issuer, int signingKeyLength = 32, bool reset = false)
    {
        // Create signing material and save to user secrets
        var newKeyMaterial = System.Security.Cryptography.RandomNumberGenerator.GetBytes(signingKeyLength);
        var secretsFilePath = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);
        Directory.CreateDirectory(Path.GetDirectoryName(secretsFilePath));

        JsonObject secrets = null;
        if (File.Exists(secretsFilePath))
        {
            using var secretsFileStream = new FileStream(secretsFilePath, FileMode.Open, FileAccess.Read);
            if (secretsFileStream.Length > 0)
            {
                secrets = JsonSerializer.Deserialize<JsonObject>(secretsFileStream, JwtSerializerOptions.Default);
            }
        }

        secrets ??= new JsonObject();
        var signkingKeysPropertyName = GetSigningKeyPropertyName(scheme);
        var shortId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var key = new SigningKey(shortId, issuer, Convert.ToBase64String(newKeyMaterial), signingKeyLength);

        if (secrets.ContainsKey(signkingKeysPropertyName))
        {
            var signingKeys = secrets[signkingKeysPropertyName].AsArray();
            if (reset)
            {
                var toRemove = signingKeys.SingleOrDefault(key => key["Issuer"].GetValue<string>() == issuer);
                signingKeys.Remove(toRemove);
            }
            signingKeys.Add(key);
        }
        else
        {
            secrets.Add(signkingKeysPropertyName, JsonValue.Create(new[] { key }));
        }

        using var secretsWriteStream = new FileStream(secretsFilePath, FileMode.Create, FileAccess.Write);
        JsonSerializer.Serialize(secretsWriteStream, secrets);

        return newKeyMaterial;
    }

    public static string GetSigningKeyPropertyName(string scheme)
        => $"Authentication:Schemes:{scheme}:{DevJwtsDefaults.SigningKeyConfigurationKey}";
}
