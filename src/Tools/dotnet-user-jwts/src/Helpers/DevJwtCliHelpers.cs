// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

internal static class DevJwtCliHelpers
{
    public static string GetUserSecretsId(string projectFilePath)
    {
        var projectDocument = XDocument.Load(projectFilePath, LoadOptions.PreserveWhitespace);
        var existingUserSecretsId = projectDocument.XPathSelectElements("//UserSecretsId").FirstOrDefault();

        if (existingUserSecretsId == null)
        {
            return null;
        }

        return existingUserSecretsId.Value;
    }

    public static string GetProject(string projectPath = null)
    {
        if (projectPath is not null)
        {
            return projectPath;
        }

        var csprojFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj");
        if (csprojFiles is [var path])
        {
            return path;
        }
        return null;
    }

    public static byte[] GetOrCreateSigningKeyMaterial(string userSecretsId)
    {
        var projectConfiguration = new ConfigurationBuilder()
            .AddUserSecrets(userSecretsId)
            .Build();

        var signingKeyMaterial = projectConfiguration[DevJwtsDefaults.SigningKeyConfigurationKey];

        var keyMaterial = new byte[DevJwtsDefaults.SigningKeyLength];
        if (signingKeyMaterial is not null && Convert.TryFromBase64String(signingKeyMaterial, keyMaterial, out var bytesWritten) && bytesWritten == DevJwtsDefaults.SigningKeyLength)
        {
            return keyMaterial;
        }

        return CreateSigningKeyMaterial(userSecretsId);
    }

    public static byte[] CreateSigningKeyMaterial(string userSecretsId, bool reset = false)
    {
        // Create signing material and save to user secrets
        var newKeyMaterial = System.Security.Cryptography.RandomNumberGenerator.GetBytes(DevJwtsDefaults.SigningKeyLength);

        string secretsFilePath;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            secretsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", userSecretsId, "secrets.json");
        }
        else
        {
            secretsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".microsoft", "usersecrets", userSecretsId, "secrets.json");
        }

        IDictionary<string, string> secrets = null;
        if (File.Exists(secretsFilePath))
        {
            using var secretsFileStream = new FileStream(secretsFilePath, FileMode.Open, FileAccess.Read);
            if (secretsFileStream.Length > 0)
            {
                secrets = JsonSerializer.Deserialize<IDictionary<string, string>>(secretsFileStream) ?? new Dictionary<string, string>();
            }
        }

        secrets ??= new Dictionary<string, string>();

        if (reset && secrets.ContainsKey(DevJwtsDefaults.SigningKeyConfigurationKey))
        {
            secrets.Remove(DevJwtsDefaults.SigningKeyConfigurationKey);
        }
        secrets.Add(DevJwtsDefaults.SigningKeyConfigurationKey, Convert.ToBase64String(newKeyMaterial));

        using var secretsWriteStream = new FileStream(secretsFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        JsonSerializer.Serialize(secretsWriteStream, secrets);

        return newKeyMaterial;
    }

    public static string GetApplicationUrl(string project)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(project));

        var launchSettingsFilePath = Path.Combine(Path.GetDirectoryName(project)!, "Properties", "launchSettings.json");
        if (File.Exists(launchSettingsFilePath))
        {
            using var launchSettingsFileStream = new FileStream(launchSettingsFilePath, FileMode.Open, FileAccess.Read);
            if (launchSettingsFileStream.Length > 0)
            {
                var launchSettingsJson = JsonDocument.Parse(launchSettingsFileStream);
                if (launchSettingsJson.RootElement.TryGetProperty("profiles", out var profiles))
                {
                    var profilesEnumerator = profiles.EnumerateObject();
                    foreach (var profile in profilesEnumerator)
                    {
                        if (profile.Value.TryGetProperty("commandName", out var commandName))
                        {
                            if (commandName.ValueEquals("Project"))
                            {
                                if (profile.Value.TryGetProperty("applicationUrl", out var applicationUrl))
                                {
                                    var value = applicationUrl.GetString();
                                    if (value is { } applicationUrls)
                                    {
                                        var urls = applicationUrls.Split(";");
                                        var firstHttpsUrl = urls.FirstOrDefault(u => u.StartsWith("https:", StringComparison.OrdinalIgnoreCase));
                                        if (firstHttpsUrl is { } result)
                                        {
                                            return result;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    public static void PrintJwt(Jwt jwt, JwtSecurityToken fullToken = null)
    {
        var table = new ConsoleTable();
        table.AddColumns("Id", "Name", "Audience", "Expires", "Issued", "Scopes", "Roles", "Custom Claims");
        if (fullToken is not null)
        {
            table.AddColumns("Token Header", "Token Payload");
        }

        if (fullToken is not null)
        {
            table.AddRows(
                jwt.Id,
                jwt.Name,
                jwt.Audience,
                jwt.Expires.ToString("O"),
                jwt.Issued.ToString("O"),
                jwt.Scopes.Any() ? string.Join(", ", jwt.Scopes) : "[none]",
                jwt.Roles.Any() ? string.Join(", ", jwt.Roles) : "[none]",
                jwt.CustomClaims?.Count > 0 ? string.Join(", ", jwt.CustomClaims.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "[none]",
                fullToken.Header.SerializeToJson(),
                fullToken.Payload.SerializeToJson()
            );
        }
        else
        {
            table.AddRows(
                jwt.Id,
                jwt.Name,
                jwt.Audience,
                jwt.Expires.ToString("O"),
                jwt.Issued.ToString("O"),
                jwt.Scopes is not null ? string.Join(", ", jwt.Scopes) : "[none]",
                jwt.Roles is not null ? string.Join(", ", jwt.Roles) : "[none]",
                jwt.CustomClaims?.Count > 0 ? jwt.CustomClaims.Select(kvp => $"{kvp.Key}={kvp.Value}") : "[none]"
            );
        }

        table.Write();
        Console.WriteLine($"Compact Token: {jwt.Token}");
    }

    public static bool TryParseClaims(List<string> input, out Dictionary<string, string> claims)
    {
        claims = new Dictionary<string, string>();
        foreach (var claim in input)
        {
            var parts = claim.Split('=');
            if (parts.Length != 2)
            {
                return false;
            }

            var key = parts[0];
            var value = parts[1];

            claims.Add(key, value);
        }
        return true;
    }
}
