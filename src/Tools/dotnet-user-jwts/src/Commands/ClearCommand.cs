// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

internal sealed class ClearCommand
{
    public static void Register(ProjectCommandLineApplication app)
    {
        app.Command("clear", cmd =>
        {
            cmd.Description = Resources.ClearCommand_Description;

            var forceOption = cmd.Option(
                "--force",
                Resources.ClearCommand_ForceOption_Description,
                CommandOptionType.NoValue);

            var appsettingsFileOption = cmd.Option(
                "--appsettings-file",
                Resources.CreateCommand_appsettingsFileOption_Description,
                CommandOptionType.SingleValue);

            cmd.HelpOption("-h|--help");

            cmd.OnExecute(() =>
            {
                if (!DevJwtCliHelpers.GetProjectAndSecretsId(cmd.ProjectOption.Value(), cmd.Reporter, out var project, out var userSecretsId))
                {
                    return 1;
                }

                if (!DevJwtCliHelpers.GetAppSettingsFile(project, appsettingsFileOption.Value(), cmd.Reporter, out var appsettingsFile))
                {
                    return 1;
                }

                return Execute(cmd.Reporter, project, userSecretsId, forceOption.HasValue(), appsettingsFile);
            });
        });
    }

    private static int Execute(IReporter reporter, string project, string userSecretsId, bool force, string appsettingsFile)
    {
        var jwtStore = new JwtStore(userSecretsId);
        var count = jwtStore.Jwts.Count;

        if (count == 0)
        {
            reporter.Output(Resources.FormatClearCommand_NoJwtsRemoved(project));
            return 0;
        }

        if (!force)
        {
            reporter.Output(Resources.FormatClearCommand_Permission(count, project));
            reporter.Output("[Y]es / [N]o");
            if (Console.ReadLine().Trim().ToUpperInvariant() != "Y")
            {
                reporter.Output(Resources.ClearCommand_Canceled);
                return 0;
            }
        }

        var appsettingsFilePath = Path.Combine(Path.GetDirectoryName(project), appsettingsFile);
        foreach (var jwt in jwtStore.Jwts)
        {
            JwtAuthenticationSchemeSettings.RemoveScheme(appsettingsFilePath, jwt.Value.Scheme);
        }

        jwtStore.Jwts.Clear();
        jwtStore.Save();

        reporter.Output(Resources.FormatClearCommand_Confirmed(count, project));

        return 0;
    }
}
