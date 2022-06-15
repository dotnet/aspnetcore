// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

internal sealed class CreateCommand
{
    private static readonly string[] _dateTimeFormats = new[] {
        "yyyy-MM-dd", "yyyy-MM-dd HH:mm", "yyyy/MM/dd", "yyyy/MM/dd HH:mm" };
    private static readonly string[] _timeSpanFormats = new[] {
        @"d\dh\hm\ms\s", @"d\dh\hm\m", @"d\dh\h", @"d\d",
        @"h\hm\ms\s", @"h\hm\m", @"h\h",
        @"m\ms\s", @"m\m",
        @"s\s"
    };

    public static void Register(ProjectCommandLineApplication app)
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
                "--name",
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

            cmd.HelpOption("-h|--help");

            cmd.OnExecute(() =>
            {
                var (options, isValid) = ValidateArguments(
                    cmd.Reporter, cmd.ProjectOption, schemeNameOption, nameOption, audienceOption, issuerOption, notBeforeOption, expiresOnOption, validForOption, rolesOption, scopesOption, claimsOption);

                if (!isValid)
                {
                    return 1;
                }

                return Execute(cmd.Reporter, cmd.ProjectOption.Value(), options);
            });
        });
    }

    private static (JwtCreatorOptions, bool) ValidateArguments(
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
        CommandOption claimsOption)
    {
        var isValid = true;
        var project = DevJwtCliHelpers.GetProject(projectOption.Value());
        var scheme = schemeNameOption.HasValue() ? schemeNameOption.Value() : "Bearer";
        var name = nameOption.HasValue() ? nameOption.Value() : Environment.UserName;

        var audience = audienceOption.HasValue() ? audienceOption.Values : DevJwtCliHelpers.GetAudienceCandidatesFromLaunchSettings(project).ToList();
        if (audience is null)
        {
            reporter.Error(Resources.CreateCommand_NoAudience_Error);
            isValid = false;
        }
        var issuer = issuerOption.HasValue() ? issuerOption.Value() : DevJwtsDefaults.Issuer;

        var notBefore = DateTime.UtcNow;
        if (notBeforeOption.HasValue())
        {
            if (!ParseDate(notBeforeOption.Value(), out notBefore))
            {
                reporter.Error(Resources.FormatCreateCommand_InvalidDate_Error("--not-before"));
                isValid = false;
            }
        }

        var expiresOn = notBefore.AddMonths(3);
        if (expiresOnOption.HasValue())
        {
            if (!ParseDate(expiresOnOption.Value(), out expiresOn))
            {
                reporter.Error(Resources.FormatCreateCommand_InvalidDate_Error("--expires-on"));
                isValid = false;
            }
        }

        if (validForOption.HasValue())
        {
            if (!TimeSpan.TryParseExact(validForOption.Value(), _timeSpanFormats, CultureInfo.InvariantCulture, out var validForValue))
            {
                reporter.Error(Resources.FormatCreateCommand_InvalidPeriod_Error("--valid-for"));
            }
            expiresOn = notBefore.Add(validForValue);
        }

        var roles = rolesOption.HasValue() ? rolesOption.Values : new List<string>();
        var scopes = scopesOption.HasValue() ? scopesOption.Values : new List<string>();

        var claims = new Dictionary<string, string>();
        if (claimsOption.HasValue())
        {
            if (!DevJwtCliHelpers.TryParseClaims(claimsOption.Values, out claims))
            {
                reporter.Error(Resources.CreateCommand_InvalidClaims_Error);
                isValid = false;
            }
        }

        return (new JwtCreatorOptions(scheme, name, audience, issuer, notBefore, expiresOn, roles, scopes, claims), isValid);

        static bool ParseDate(string datetime, out DateTime parsedDateTime) =>
            DateTime.TryParseExact(datetime, _dateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsedDateTime);
    }

    private static int Execute(
        IReporter reporter,
        string projectPath,
        JwtCreatorOptions options)
    {
        if (!DevJwtCliHelpers.GetProjectAndSecretsId(projectPath, reporter, out var project, out var userSecretsId))
        {
            return 1;
        }
        var keyMaterial = DevJwtCliHelpers.GetOrCreateSigningKeyMaterial(userSecretsId);

        var jwtIssuer = new JwtIssuer(options.Issuer, keyMaterial);
        var jwtToken = jwtIssuer.Create(options);

        var jwtStore = new JwtStore(userSecretsId);
        var jwt = Jwt.Create(options.Scheme, jwtToken, JwtIssuer.WriteToken(jwtToken), options.Scopes, options.Roles, options.Claims);
        if (options.Claims is { } customClaims)
        {
            jwt.CustomClaims = customClaims;
        }
        jwtStore.Jwts.Add(jwtToken.Id, jwt);
        jwtStore.Save();

        var appsettingsFilePath = Path.Combine(Path.GetDirectoryName(project), "appsettings.Development.json");
        var settingsToWrite = new JwtAuthenticationSchemeSettings(options.Scheme, options.Audiences, options.Issuer);
        settingsToWrite.Save(appsettingsFilePath);

        reporter.Output(Resources.FormatCreateCommand_Confirmed(jwtToken.Id));

        return 0;
    }
}
