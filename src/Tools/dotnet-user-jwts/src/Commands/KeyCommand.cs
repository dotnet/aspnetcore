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
            cmd.Description = Resources.KeyCommand_Description;

            var schemeOption = cmd.Option(
                "--scheme",
                Resources.KeyCommand_SchemeOption_Description,
                CommandOptionType.SingleValue);

            var issuerOption = cmd.Option(
                "--issuer",
                Resources.KeyCommand_IssuerOption_Description,
                CommandOptionType.SingleValue
            );

            var resetOption = cmd.Option(
                "--reset",
                Resources.KeyCommand_ResetOption_Description,
                CommandOptionType.NoValue);

            var forceOption = cmd.Option(
                "--force",
                Resources.KeyCommand_ForceOption_Description,
                CommandOptionType.NoValue);

            cmd.HelpOption("-h|--help");

            cmd.OnExecute(() =>
            {
                return Execute(cmd.Reporter,
                    cmd.ProjectOption.Value(),
                    schemeOption.Value() ?? DevJwtsDefaults.Scheme,
                    issuerOption.Value() ?? DevJwtsDefaults.Issuer,
                    resetOption.HasValue(), forceOption.HasValue());
            });
        });
    }

    private static int Execute(IReporter reporter, string projectPath, string scheme, string issuer, bool reset, bool force)
    {
        if (!DevJwtCliHelpers.GetProjectAndSecretsId(projectPath, reporter, out var _, out var userSecretsId))
        {
            return 1;
        }

        if (reset == true)
        {
            if (!force)
            {
                reporter.Output(Resources.KeyCommand_Permission);
                reporter.Error("[Y]es / [N]o");
                if (Console.ReadLine().Trim().ToUpperInvariant() != "Y")
                {
                    reporter.Output(Resources.KeyCommand_Canceled);
                    return 0;
                }
            }

            var key = SigningKeysHandler.CreateSigningKeyMaterial(userSecretsId, scheme, issuer, reset: true);
            reporter.Output(Resources.FormatKeyCommand_KeyCreated(Convert.ToBase64String(key)));
            return 0;
        }

        var signingKeyMaterial = SigningKeysHandler.GetSigningKeyMaterial(userSecretsId, scheme, issuer);

        if (signingKeyMaterial is null)
        {
            reporter.Output(Resources.KeyCommand_KeyNotFound);
            return 0;
        }

        reporter.Output(Resources.FormatKeyCommand_Confirmed(Convert.ToBase64String(signingKeyMaterial)));
        return 0;
    }
}
