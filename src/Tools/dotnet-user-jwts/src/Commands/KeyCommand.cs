// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

internal sealed class KeyCommand
{
    public static void Register(ProjectCommandLineApplication app)
    {
        app.Command("key", cmd =>
        {
            cmd.Description = "Display or reset the signing key used to issue JWTs";

            var resetOption = cmd.Option(
                "--reset",
                "Reset the signing key. This will invalidate all previously issued JWTs for this project.",
                CommandOptionType.NoValue);

            var forceOption = cmd.Option(
                "--force",
                "Don't prompt for confirmation before resetting the signing key.",
                CommandOptionType.NoValue);

            cmd.HelpOption("-h|--help");

            cmd.OnExecute(() =>
            {
                return Execute(cmd.Reporter, cmd.ProjectOption.Value(), resetOption.HasValue(), forceOption.HasValue());
            });
        });
    }

    private static int Execute(IReporter reporter, string projectPath, bool reset, bool force)
    {
        if (!DevJwtCliHelpers.GetProjectAndSecretsId(projectPath, reporter, out var _, out var userSecretsId))
        {
            return 1;
        }

        if (reset == true)
        {
            if (!force)
            {
                reporter.Output("Are you sure you want to reset the JWT signing key? This will invalidate all JWTs previously issued for this project.\n [Y]es / [N]o");
                if (Console.ReadLine().Trim().ToUpperInvariant() != "Y")
                {
                    reporter.Output("Key reset canceled.");
                    return 0;
                }
            }

            var key = DevJwtCliHelpers.CreateSigningKeyMaterial(userSecretsId, reset: true);
            reporter.Output($"New signing key created: {Convert.ToBase64String(key)}");
            return 0; 
        }

        var projectConfiguration = new ConfigurationBuilder()
            .AddUserSecrets(userSecretsId)
            .Build();
        var signingKeyMaterial = projectConfiguration[DevJwtsDefaults.SigningKeyConfigurationKey];

        if (signingKeyMaterial is null)
        {
            reporter.Output("Signing key for JWTs was not found. One will be created automatically when the first JWT is created, or you can force creation of a key with the --reset option.");
            return 0;
        }

        reporter.Output($"Signing Key: {signingKeyMaterial}");
        return 0;
    }
}
