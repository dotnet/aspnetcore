// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

internal sealed class CreateCommand
{
    private static readonly string[] _dateTimeFormats = new[] {
        "yyyy-MM-dd", "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd", "yyyy/MM/dd HH:mm", "yyyy/MM/dd HH:mm:ss" };
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
            cmd.Description = "Issue a new JSON Web Token";

            var schemeNameOption = cmd.Option(
                "--scheme",
                "The scheme name to use for the generated token. Defaults to 'Bearer'",
                CommandOptionType.SingleValue
                );

            var nameOption = cmd.Option(
                "--name",
                "The name of the user to create the JWT for. Defaults to the current environment user.",
                CommandOptionType.SingleValue);

            var audienceOption = cmd.Option(
                "--audience",
                "The audience to create the JWT for. Defaults to the first HTTPS URL configured in the project's launchSettings.json",
                CommandOptionType.SingleValue);

            var issuerOption = cmd.Option(
                "--issuer",
                "The issuer of the JWT. Defaults to the dotnet-user-jwts",
                CommandOptionType.SingleValue);

            var scopesOption = cmd.Option(
                "--scope",
                "A scope claim to add to the JWT. Specify once for each scope.",
                CommandOptionType.MultipleValue);

            var rolesOption = cmd.Option(
                "--role",
                "A role claim to add to the JWT. Specify once for each role",
                CommandOptionType.MultipleValue);

            var claimsOption = cmd.Option(
                "--claim",
                "Claims to add to the JWT. Specify once for each claim in the format \"name=value\"",
                CommandOptionType.MultipleValue);

            var notBeforeOption = cmd.Option(
                "--not-before",
                @"The UTC date & time the JWT should not be valid before in the format 'yyyy-MM-dd [[HH:mm[[:ss]]]]'. Defaults to the date & time the JWT is created",
                CommandOptionType.SingleValue);

            var expiresOnOption = cmd.Option(
                "--expires-on",
                @"The UTC date & time the JWT should expire in the format 'yyyy-MM-dd [[[[HH:mm]]:ss]]'. Defaults to 6 months after the --not-before date. " +
                         "Do not use this option in conjunction with the --valid-for option.",
                CommandOptionType.SingleValue);

            var validForOption = cmd.Option(
                "--valid-for",
                "The period the JWT should expire after. Specify using a number followed by a period type like 'd' for days, 'h' for hours, " +
                         "'m' for minutes, and 's' for seconds, e.g. '365d'. Do not use this option in conjunction with the --expires-on option.",
                CommandOptionType.SingleValue);

            cmd.HelpOption("-h|--help");

            cmd.OnExecute(() =>
            {
                var (options, isValid) = ValidateArguments(
                    app.ProjectOption, schemeNameOption, nameOption, audienceOption, issuerOption, notBeforeOption, expiresOnOption, validForOption, rolesOption, scopesOption, claimsOption);

                if (!isValid)
                {
                    return 1;
                }

                return Execute(app.ProjectOption.Value(), options);
            });
        });
    }

    private static (JwtCreatorOptions, bool) ValidateArguments(
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

        var audience = audienceOption.HasValue() ? audienceOption.Value() : DevJwtCliHelpers.GetApplicationUrl(project);
        if (audience is null)
        {
            Console.WriteLine("Could not determine the project's HTTPS URL. Please specify an audience for the JWT using the --audience option.");
            isValid = false;
        }
        var issuer = issuerOption.HasValue() ? issuerOption.Value() : DevJwtsDefaults.Issuer;

        var notBefore = DateTime.UtcNow;
        if (notBeforeOption.HasValue())
        {
            if (!ParseDate(notBeforeOption.Value(), out notBefore))
            {
                Console.WriteLine(@"The date provided for --not-before could not be parsed. Ensure you use the format 'yyyy-MM-dd [[[[HH:mm]]:ss]]'.");
                isValid = false;
            }
        }

        var expiresOn = notBefore.AddMonths(6);
        if (expiresOnOption.HasValue())
        {
            if (!ParseDate(expiresOnOption.Value(), out expiresOn))
            {
                Console.WriteLine(@"The date provided for --expires-on could not be parsed. Ensure you use the format 'yyyy-MM-dd [[[[HH:mm]]:ss]]'.");
                isValid = false;
            }
        }

        if (validForOption.HasValue())
        {
            if (!TimeSpan.TryParseExact(validForOption.Value(), _timeSpanFormats, CultureInfo.InvariantCulture, out var validForValue))
            {
                Console.WriteLine("The period provided for --valid-for could not be parsed. Ensure you use a format like '10d', '22h', '45s' etc.");
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
                Console.WriteLine("Malformed claims supplied. Ensure each claim is in the format \"name=value\".");
                isValid = false;
            }
        }

        return (new JwtCreatorOptions(scheme, name, audience, issuer, notBefore, expiresOn, roles, scopes, claims), isValid);

        static bool ParseDate(string datetime, out DateTime parsedDateTime) =>
            DateTime.TryParseExact(datetime, _dateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsedDateTime);
    }

    private static int Execute(
        string projectPath,
        JwtCreatorOptions options)
    {
        var project = DevJwtCliHelpers.GetProject(projectPath);
        if (project == null)
        {
            Console.WriteLine($"No project found at `-p|--project` path or current directory.");
            return 1;
        }

        var userSecretsId = DevJwtCliHelpers.GetUserSecretsId(project);
        var keyMaterial = DevJwtCliHelpers.GetOrCreateSigningKeyMaterial(userSecretsId);

        var jwtIssuer = new JwtIssuer(options.Issuer, keyMaterial);
        var jwtToken = jwtIssuer.Create(options);

        var jwtStore = new JwtStore(userSecretsId);
        var jwt = Jwt.Create(jwtToken, JwtIssuer.WriteToken(jwtToken), options.Scopes, options.Roles, options.Claims);
        if (options.Claims is { } customClaims)
        {
            jwt.CustomClaims = customClaims;
        }
        jwtStore.Jwts.Add(jwtToken.Id, jwt);
        jwtStore.Save();

        var appsettingsFilePath = Path.Combine(Path.GetDirectoryName(project), "appsettings.Development.json");
        var settingsToWrite = new JwtAuthenticationSchemeSettings(options.Scheme, options.Name, options.Audience, options.Issuer);
        settingsToWrite.Save(appsettingsFilePath);

        Console.WriteLine("New JWT saved!");

        return 0;
    }
}
