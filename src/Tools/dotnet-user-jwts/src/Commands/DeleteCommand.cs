// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.CommandLineUtils;

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
                return Execute(app.ProjectOption.Value(), idArgument.Value);
            });
        });
    }

    private static int Execute(string projectPath, string id)
    {
        var project = DevJwtCliHelpers.GetProject(projectPath);
        if (project == null)
        {
            Console.WriteLine($"No project found at `-p|--project` path or current directory.");
            return 1;
        }

        var userSecretsId = DevJwtCliHelpers.GetUserSecretsId(project);
        var jwtStore = new JwtStore(userSecretsId);

        if (!jwtStore.Jwts.ContainsKey(id))
        {
            Console.WriteLine($"[ERROR] No JWT with ID '{id}' found");
            return 1;
        }

        jwtStore.Jwts.Remove(id);
        jwtStore.Save();

        Console.WriteLine($"Deleted JWT with ID '{id}'");

        return 0;
    }
}
