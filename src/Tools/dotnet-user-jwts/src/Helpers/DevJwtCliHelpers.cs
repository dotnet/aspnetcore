// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Tools.Internal;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

internal static class DevJwtCliHelpers
{
    public static string GetOrSetUserSecretsId(string projectFilePath)
    {
        var resolver = new ProjectIdResolver(NullReporter.Singleton, projectFilePath);
        var id = resolver.Resolve(projectFilePath, configuration: null);
        if (string.IsNullOrEmpty(id))
        {
            return UserSecretsCreator.CreateUserSecretsId(NullReporter.Singleton, projectFilePath, projectFilePath);
        }
        return id;
    }

    public static bool GetProjectAndSecretsId(string projectPath, IReporter reporter, out string project, out string userSecretsId)
    {
        var finder = new MsBuildProjectFinder(Directory.GetCurrentDirectory());
        project = finder.FindMsBuildProject(projectPath);
        userSecretsId = null;
        if (project == null)
        {
            reporter.Error(Resources.ProjectOption_ProjectNotFound);
            return false;
        }

        userSecretsId = GetOrSetUserSecretsId(project);
        if (userSecretsId == null)
        {
            reporter.Error(Resources.ProjectOption_SercretIdNotFound);
            return false;
        }
        return true;
    }

    public static List<string> GetAudienceCandidatesFromLaunchSettings(string project)
    {
        if (string.IsNullOrEmpty(project))
        {
            return new List<string>();
        }

        var launchSettingsFilePath = Path.Combine(Path.GetDirectoryName(project)!, "Properties", "launchSettings.json");
        var applicationUrls = new HashSet<string>();
        if (File.Exists(launchSettingsFilePath))
        {
            using var launchSettingsFileStream = new FileStream(launchSettingsFilePath, FileMode.Open, FileAccess.Read);
            if (launchSettingsFileStream.Length > 0)
            {
                var launchSettingsJson = JsonDocument.Parse(launchSettingsFileStream);

                if (ExtractIISExpressUrlFromProfile(launchSettingsJson.RootElement) is { } iisUrls)
                {
                    applicationUrls.UnionWith(iisUrls);
                }

                if (launchSettingsJson.RootElement.TryGetProperty("profiles", out var profiles))
                {
                    var profilesEnumerator = profiles.EnumerateObject();
                    foreach (var profile in profilesEnumerator)
                    {
                        if (ExtractKestrelUrlsFromProfile(profile) is { } kestrelUrls)
                        {
                            applicationUrls.UnionWith(kestrelUrls);
                        }
                    }
                }
            }
        }

        return applicationUrls.ToList();

        static List<string> ExtractIISExpressUrlFromProfile(JsonElement rootElement)
        {
            if (rootElement.TryGetProperty("iisSettings", out var iisSettings))
            {
                if (iisSettings.TryGetProperty("iisExpress", out var iisExpress))
                {
                    List<string> iisUrls = new();
                    if (iisExpress.TryGetProperty("applicationUrl", out var iisUrl))
                    {
                        iisUrls.Add(iisUrl.GetString());
                    }

                    if (iisExpress.TryGetProperty("sslPort", out var sslPort))
                    {
                        iisUrls.Add($"https://localhost:{sslPort.GetInt32()}");
                    }

                    return iisUrls;
                }
            }

            return null;
        }

        static string[] ExtractKestrelUrlsFromProfile(JsonProperty profile)
        {
            if (profile.Value.TryGetProperty("commandName", out var commandName))
            {
                if (commandName.ValueEquals("Project"))
                {
                    if (profile.Value.TryGetProperty("applicationUrl", out var applicationUrl))
                    {
                        var value = applicationUrl.GetString();
                        if (value is { } urls)
                        {
                            return urls.Split(';');
                        }
                    }
                }
            }

            return null;
        }
    }

    public static void PrintJwt(IReporter reporter, Jwt jwt, bool showAll, string outputFormat, JwtSecurityToken fullToken = null)
    {
        switch (outputFormat)
        {
            case "token":
                reporter.Output(fullToken is not null ? jwt.Token : string.Empty);
                break;
            case "json":
                PrintJwtJson(reporter, jwt, showAll, fullToken);
                break;
            default:
                PrintJwtDefault(reporter, jwt, showAll, fullToken);
                break;
        }

        static void PrintJwtJson(IReporter reporter, Jwt jwt, bool showAll, JwtSecurityToken fullToken)
        {
            reporter.Output(JsonSerializer.Serialize(jwt, JwtSerializerOptions.Default));
        }

        static void PrintJwtDefault(IReporter reporter, Jwt jwt, bool showAll, JwtSecurityToken fullToken)
        {
            reporter.Output(Resources.FormatPrintCommand_Confirmed(jwt.Id));
            reporter.Output($"{Resources.JwtPrint_Id}: {jwt.Id}");
            reporter.Output($"{Resources.JwtPrint_Name}: {jwt.Name}");
            reporter.Output($"{Resources.JwtPrint_Scheme}: {jwt.Scheme}");
            reporter.Output($"{Resources.JwtPrint_Audiences}: {jwt.Audience}");
            reporter.Output($"{Resources.JwtPrint_NotBefore}: {jwt.NotBefore:O}");
            reporter.Output($"{Resources.JwtPrint_ExpiresOn}: {jwt.Expires:O}");
            reporter.Output($"{Resources.JwtPrint_IssuedOn}: {jwt.Issued:O}");

            if (!jwt.Scopes.IsNullOrEmpty() || showAll)
            {
                var scopesValue = jwt.Scopes.IsNullOrEmpty()
                    ? "none"
                    : string.Join(", ", jwt.Scopes);
                reporter.Output($"{Resources.JwtPrint_Scopes}: {scopesValue}");
            }

            if (!jwt.Roles.IsNullOrEmpty() || showAll)
            {
                var rolesValue = jwt.Roles.IsNullOrEmpty()
                    ? "none"
                    : string.Join(", ", jwt.Roles);
                reporter.Output($"{Resources.JwtPrint_Roles}: [{rolesValue}]");
            }

            if (!jwt.CustomClaims.IsNullOrEmpty() || showAll)
            {
                var customClaimsValue = jwt.CustomClaims.IsNullOrEmpty()
                    ? "none"
                    : string.Join(", ", jwt.CustomClaims.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                reporter.Output($"{Resources.JwtPrint_CustomClaims}: [{customClaimsValue}]");
            }

            if (showAll)
            {
                reporter.Output($"{Resources.JwtPrint_TokenHeader}: {fullToken.Header.SerializeToJson()}");
                reporter.Output($"{Resources.JwtPrint_TokenPayload}: {fullToken.Payload.SerializeToJson()}");
            }

            var tokenValueFieldName = showAll ? Resources.JwtPrint_CompactToken : Resources.JwtPrint_Token;
            reporter.Output($"{tokenValueFieldName}: {jwt.Token}");
        }
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

    public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
    {
        return enumerable == null || !enumerable.Any();
    }
}
