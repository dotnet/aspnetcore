// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

internal sealed class CreateCommand
{
    private static readonly string[] _dateTimeFormats = new[] {
        "yyyy-MM-dd", "yyyy-MM-dd HH:mm", "yyyy/MM/dd", "yyyy/MM/dd HH:mm", "yyyy-MM-ddTHH:mm:ss.fffffffzzz"  };
    private static readonly string[] _timeSpanFormats = new[] {
        @"d\dh\hm\ms\s", @"d\dh\hm\m", @"d\dh\h", @"d\d",
        @"h\hm\ms\s", @"h\hm\m", @"h\h",
        @"m\ms\s", @"m\m",
        @"s\s"
    };

    public static void Register(ProjectCommandLineApplication app, Program program)
    {
        app.Command("create", cmd =>
        {
            cmd.Description = Resources.CreateCommand_Description;

            var schemeNameOption = cmd.Option(
                "--scheme",
                Resources.CreateCommand_SchemeOption_Description,
                CommandOptionType.SingleValue
                );

            var nameOption = cmd.Option(
                "-n|--name",
                Resources.CreateCommand_NameOption_Description,
                CommandOptionType.SingleValue);

            var audienceOption = cmd.Option(
                "--audience",
                Resources.CreateCommand_AudienceOption_Description,
                CommandOptionType.MultipleValue);

            var issuerOption = cmd.Option(
                "--issuer",
                Resources.CreateCommand_IssuerOption_Description,
                CommandOptionType.SingleValue);

            var scopesOption = cmd.Option(
                "--scope",
                Resources.CreateCommand_ScopeOption_Description,
                CommandOptionType.MultipleValue);

            var rolesOption = cmd.Option(
                "--role",
                Resources.CreateCommand_RoleOption_Description,
                CommandOptionType.MultipleValue);

            var claimsOption = cmd.Option(
                "--claim",
                Resources.CreateCommand_ClaimOption_Description,
                CommandOptionType.MultipleValue);

            var notBeforeOption = cmd.Option(
                "--not-before",
                Resources.CreateCommand_NotBeforeOption_Description,
                CommandOptionType.SingleValue);

            var expiresOnOption = cmd.Option(
                "--expires-on",
                Resources.CreateCommand_ExpiresOnOption_Description,
                CommandOptionType.SingleValue);

            var validForOption = cmd.Option(
                "--valid-for",
                Resources.CreateCommand_ValidForOption_Description,
                CommandOptionType.SingleValue);

            var appsettingsFileOption = cmd.Option(
                "--appsettings-file",
                Resources.CreateCommand_appsettingsFileOption_Description,
                CommandOptionType.SingleValue);

            cmd.HelpOption("-h|--help");

            cmd.OnExecute(() =>
            {
                var (options, isValid, optionsString, appsettingsFile) = ValidateArguments(
                    cmd.Reporter, cmd.ProjectOption, schemeNameOption, nameOption, audienceOption, issuerOption, notBeforeOption, expiresOnOption, validForOption, rolesOption, scopesOption, claimsOption, appsettingsFileOption);

                if (!isValid)
                {
                    return 1;
                }

                return Execute(cmd.Reporter, cmd.ProjectOption.Value(), options, optionsString, cmd.OutputOption.Value(), appsettingsFile, program);
            });
        });
    }

    private static (JwtCreatorOptions, bool, string, string) ValidateArguments(
        IReporter reporter,
        CommandOption projectOption,
        CommandOption schemeNameOption,
        CommandOption nameOption,
        CommandOption audienceOption,
        CommandOption issuerOption,
        CommandOption notBeforeOption,
        CommandOption expiresOnOption,
        CommandOption validForOption,
        CommandOption rolesOption,
        CommandOption scopesOption,
        CommandOption claimsOption,
        CommandOption appsettingsFileOption)
    {
        var isValid = true;
        var finder = new MsBuildProjectFinder(Directory.GetCurrentDirectory());
        var project = finder.FindMsBuildProject(projectOption.Value());

        if (project == null)
        {
            reporter.Error(Resources.ProjectOption_ProjectNotFound);
            isValid = false;
            // Break out early if we haven't been able to resolve a project
            // since we depend on it for the managing of JWT tokens
            return (
                null,
                isValid,
                string.Empty,
                string.Empty
            );
        }

        var scheme = schemeNameOption.HasValue() ? schemeNameOption.Value() : "Bearer";
        var optionsString = schemeNameOption.HasValue() ? $"{Resources.JwtPrint_Scheme}: {scheme}{Environment.NewLine}" : string.Empty;

        var name = nameOption.HasValue() ? nameOption.Value() : Environment.UserName;
        optionsString += $"{Resources.JwtPrint_Name}: {name}{Environment.NewLine}";

        var audience = audienceOption.HasValue() ? audienceOption.Values : DevJwtCliHelpers.GetAudienceCandidatesFromLaunchSettings(project);
        optionsString += audienceOption.HasValue() ? $"{Resources.JwtPrint_Audiences}: {string.Join(", ", audience)}{Environment.NewLine}" : string.Empty;
        if (audience is null || audience.Count == 0)
        {
            reporter.Error(Resources.CreateCommand_NoAudience_Error);
            isValid = false;
        }
        var issuer = issuerOption.HasValue() ? issuerOption.Value() : DevJwtsDefaults.Issuer;
        optionsString += issuerOption.HasValue() ? $"{Resources.JwtPrint_Issuer}: {issuer}{Environment.NewLine}" : string.Empty;

        var notBefore = DateTime.UtcNow;
        if (notBeforeOption.HasValue())
        {
            if (!ParseDate(notBeforeOption.Value(), out notBefore))
            {
                reporter.Error(Resources.FormatCreateCommand_InvalidDate_Error("--not-before"));
                isValid = false;
            }
            optionsString += $"{Resources.JwtPrint_NotBefore}: {notBefore:O}{Environment.NewLine}";
        }

        var expiresOn = notBefore.AddMonths(3);
        if (expiresOnOption.HasValue())
        {
            if (!ParseDate(expiresOnOption.Value(), out expiresOn))
            {
                reporter.Error(Resources.FormatCreateCommand_InvalidDate_Error("--expires-on"));
                isValid = false;
            }

            if (validForOption.HasValue())
            {
                reporter.Error(Resources.CreateCommand_InvalidExpiresOn_Error);
                isValid = false;
            }
            else
            {
                optionsString += $"{Resources.JwtPrint_ExpiresOn}: {expiresOn:O}{Environment.NewLine}";
            }

        }

        if (validForOption.HasValue())
        {
            if (!TimeSpan.TryParseExact(validForOption.Value(), _timeSpanFormats, CultureInfo.InvariantCulture, out var validForValue))
            {
                reporter.Error(Resources.FormatCreateCommand_InvalidPeriod_Error("--valid-for"));
            }
            expiresOn = notBefore.Add(validForValue);

            if (expiresOnOption.HasValue())
            {
                reporter.Error(Resources.CreateCommand_InvalidExpiresOn_Error);
                isValid = false;
            }
            else
            {
                optionsString += $"{Resources.JwtPrint_ExpiresOn}: {expiresOn:O}{Environment.NewLine}";
            }
        }

        var roles = rolesOption.HasValue() ? rolesOption.Values : new List<string>();
        optionsString += rolesOption.HasValue() ? $"{Resources.JwtPrint_Roles}: [{string.Join(", ", roles)}]{Environment.NewLine}" : string.Empty;

        var scopes = scopesOption.HasValue() ? scopesOption.Values : new List<string>();
        optionsString += scopesOption.HasValue() ? $"{Resources.JwtPrint_Scopes}: {string.Join(", ", scopes)}{Environment.NewLine}" : string.Empty;

        var claims = new Dictionary<string, string>();
        if (claimsOption.HasValue())
        {
            if (!DevJwtCliHelpers.TryParseClaims(claimsOption.Values, out claims))
            {
                reporter.Error(Resources.CreateCommand_InvalidClaims_Error);
                isValid = false;
            }
            optionsString += $"{Resources.JwtPrint_CustomClaims}: [{string.Join(", ", claims.Select(kvp => $"{kvp.Key}={kvp.Value}"))}]{Environment.NewLine}";
        }

        var appsettingsFile = DevJwtCliHelpers.DefaultAppSettingsFile;
        if (appsettingsFileOption.HasValue())
        {
            isValid = DevJwtCliHelpers.GetAppSettingsFile(project, appsettingsFileOption.Value(), reporter, out appsettingsFile);

            optionsString += appsettingsFileOption.HasValue() ? $"{Resources.JwtPrint_appsettingsFile}: {appsettingsFile}{Environment.NewLine}" : string.Empty;
        }

        return (
            new JwtCreatorOptions(scheme, name, audience, issuer, notBefore, expiresOn, roles, scopes, claims),
            isValid,
            optionsString,
            appsettingsFile);

        static bool ParseDate(string datetime, out DateTime parsedDateTime) =>
            DateTime.TryParseExact(datetime, _dateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsedDateTime);
    }

    private static int Execute(
        IReporter reporter,
        string projectPath,
        JwtCreatorOptions options,
        string optionsString,
        string outputFormat,
        string appsettingsFile,
        Program program)
    {
        if (!DevJwtCliHelpers.GetProjectAndSecretsId(projectPath, reporter, out var project, out var userSecretsId))
        {
            return 1;
        }
        var keyMaterial = SigningKeysHandler.GetOrCreateSigningKeyMaterial(userSecretsId, options.Scheme, options.Issuer);

        var jwtIssuer = new JwtIssuer(options.Issuer, keyMaterial);
        var jwtToken = jwtIssuer.Create(options);

        var jwtStore = new JwtStore(userSecretsId, program);
        var jwt = Jwt.Create(options.Scheme, jwtToken, JwtIssuer.WriteToken(jwtToken), options.Scopes, options.Roles, options.Claims);
        if (options.Claims is { } customClaims)
        {
            jwt.CustomClaims = customClaims;
        }
        jwtStore.Jwts.Add(jwtToken.Id, jwt);
        jwtStore.Save();

        var appsettingsFilePath = Path.Combine(Path.GetDirectoryName(project), appsettingsFile);
        var settingsToWrite = new JwtAuthenticationSchemeSettings(options.Scheme, options.Audiences, options.Issuer);
        settingsToWrite.Save(appsettingsFilePath);

        switch (outputFormat)
        {
            case "token":
                reporter.Output(jwt.Token);
                break;
            case "json":
                reporter.Output(JsonSerializer.Serialize(jwt, JwtSerializerOptions.Default));
                break;
            default:
                reporter.Output(Resources.FormatCreateCommand_Confirmed(jwtToken.Id));
                reporter.Output(optionsString);
                reporter.Output($"{Resources.JwtPrint_Token}: {jwt.Token}");
                break;
        }

        return 0;
    }
}
