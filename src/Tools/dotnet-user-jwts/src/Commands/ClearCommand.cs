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
            cmd.Description = "Delete all issued JWTs for a project";

            var forceOption = cmd.Option(
                "--force",
                "Don't prompt for confirmation before deleting JWTs",
                CommandOptionType.NoValue);

            cmd.HelpOption("-h|--help");

            cmd.OnExecute(() =>
            {
                return Execute(cmd.Reporter, cmd.ProjectOption.Value(), forceOption.HasValue());
            });
        });
    }

    private static int Execute(IReporter reporter, string projectPath, bool force)
    {
        if (!DevJwtCliHelpers.GetProjectAndSecretsId(projectPath, reporter, out var project, out var userSecretsId))
        {
            return 1;
        }
        var jwtStore = new JwtStore(userSecretsId);
        var count = jwtStore.Jwts.Count;

        if (count == 0)
        {
            reporter.Output($"There are no JWTs to delete from {project}.");
            return 0;
        }

        if (!force)
        {
            reporter.Output($"Are you sure you want to delete {count} JWT(s) for {project}?{Environment.NewLine} [Y]es / [N]o");
            if (Console.ReadLine().Trim().ToUpperInvariant() != "Y")
            {
                reporter.Output("Canceled, no JWTs were deleted.");
                return 0;
            }
        }

        var appsettingsFilePath = Path.Combine(Path.GetDirectoryName(project), "appsettings.Development.json");
        foreach (var jwt in jwtStore.Jwts)
        {
            JwtAuthenticationSchemeSettings.RemoveScheme(appsettingsFilePath, jwt.Value.Scheme);
        }

        jwtStore.Jwts.Clear();
        jwtStore.Save();

        reporter.Output($"Deleted {count} token(s) from {project} successfully.");

        return 0;
    }
}
