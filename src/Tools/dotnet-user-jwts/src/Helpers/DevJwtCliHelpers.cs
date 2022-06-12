// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

internal static class DevJwtCliHelpers
{
    public static string GetOrSetUserSecretsId(IReporter reporter, string projectFilePath)
    {
        var resolver = new ProjectIdResolver(reporter, projectFilePath);
        var id = resolver.Resolve(projectFilePath, configuration: null);
        if (string.IsNullOrEmpty(id))
        {
            return UserSecretsCreator.CreateUserSecretsId(reporter, projectFilePath, projectFilePath);
        }
        return id;
    }

    public static string GetProject(string projectPath = null)
    {
        if (projectPath is not null)
        {
            return projectPath;
        }

        var csprojFiles = Directory.EnumerateFileSystemEntries(Directory.GetCurrentDirectory(), "*.*proj", SearchOption.TopDirectoryOnly)
                .Where(f => !".xproj".Equals(Path.GetExtension(f), StringComparison.OrdinalIgnoreCase))
                .ToList();
        if (csprojFiles is [var path])
        {
            return path;
        }
        return null;
    }

    public static bool GetProjectAndSecretsId(string projectPath, IReporter reporter, out string project, out string userSecretsId)
    {
        project = GetProject(projectPath);
        userSecretsId = null;
        if (project == null)
        {
            reporter.Error($"No project found at `-p|--project` path or current directory.");
            return false;
        }

        userSecretsId = GetOrSetUserSecretsId(reporter, project);
        if (userSecretsId == null)
        {
            reporter.Error($"Project does not contain a user secrets ID.");
            return false;
        }
        return true;
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
        var secretsFilePath = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);
        Directory.CreateDirectory(Path.GetDirectoryName(secretsFilePath));

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

        using var secretsWriteStream = new FileStream(secretsFilePath, FileMode.Create, FileAccess.Write);
        JsonSerializer.Serialize(secretsWriteStream, secrets);

        return newKeyMaterial;
    }

    public static string[] GetAudienceCandidatesFromLaunchSettings(string project)
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
                                        return applicationUrls.Split(';');
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

    public static void PrintJwt(IReporter reporter, Jwt jwt, JwtSecurityToken fullToken = null)
    {
        reporter.Output(JsonSerializer.Serialize(jwt, new JsonSerializerOptions { WriteIndented = true }));

        if (fullToken is not null)
        {
            reporter.Output($"Token Header: {fullToken.Header.SerializeToJson()}");
            reporter.Output($"Token Payload: {fullToken.Payload.SerializeToJson()}");
        }
        reporter.Output($"Compact Token: {jwt.Token}");
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
