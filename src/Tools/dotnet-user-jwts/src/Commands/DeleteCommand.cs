// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

internal sealed class DeleteCommand
{
    public static void Register(ProjectCommandLineApplication app)
    {
        app.Command("delete", cmd =>
        {
            cmd.Description = "Delete a given JWT";

            var idArgument = cmd.Argument("[id]", "The ID of the JWT to delete");
            cmd.HelpOption("-h|--help");

            cmd.OnExecute(() =>
            {
                if (idArgument.Value is null)
                {
                    cmd.ShowHelp();
                    return 0;
                }
                return Execute(cmd.Reporter, cmd.ProjectOption.Value(), idArgument.Value);
            });
        });
    }

    private static int Execute(IReporter reporter, string projectPath, string id)
    {
        if (!DevJwtCliHelpers.GetProjectAndSecretsId(projectPath, reporter, out var project, out var userSecretsId))
        {
            return 1;
        }
        var jwtStore = new JwtStore(userSecretsId);

        if (!jwtStore.Jwts.ContainsKey(id))
        {
            reporter.Error($"[ERROR] No JWT with ID '{id}' found");
            return 1;
        }

        var jwt = jwtStore.Jwts[id];
        var appsettingsFilePath = Path.Combine(Path.GetDirectoryName(project), "appsettings.Development.json");
        JwtAuthenticationSchemeSettings.RemoveScheme(appsettingsFilePath, jwt.Scheme);
        jwtStore.Jwts.Remove(id);
        jwtStore.Save();

        reporter.Output($"Deleted JWT with ID '{id}'");

        return 0;
    }
}
