// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

<<<<<<< HEAD
internal class ListCommand
=======
internal sealed class ListCommand
>>>>>>> aed8a228a7 (Add dotnet dev-jwts tool)
{
    public static void Register(CommandLineApplication app)
    {
        app.Command("list", cmd =>
        {
            cmd.Description = "Lists the JWTs issued for the project";

            var projectOption = cmd.Option(
                "--project",
                "The path of the project to operate on. Defaults to the project in the current directory",
                CommandOptionType.SingleValue);

            var showTokensOption = cmd.Option(
                "--show-tokens",
                "Indicates whether JWT base64 strings should be shown",
                CommandOptionType.NoValue);

            cmd.HelpOption("-h|--help");

            cmd.OnExecute(() =>
            {
                return Execute(projectOption.Value(), showTokensOption.HasValue());
            });
        });
    }

    private static int Execute(string projectPath, bool showTokens)
    {
        var project = DevJwtCliHelpers.GetProject(projectPath);
        if (project == null)
        {
            Console.WriteLine($"No project found at {projectPath} or current directory.");
            return 1;
        }
        var userSecretsId = DevJwtCliHelpers.GetUserSecretsId(project);
        if (userSecretsId == null)
        {
            Console.WriteLine($"Project does not contain a user secret ID.");
            return 1;
        }
        var jwtStore = new JwtStore(userSecretsId);

        Console.WriteLine($"Project: {project}");
        Console.WriteLine($"User Secrets Id: {userSecretsId}");

        if (jwtStore.Jwts is { Count: > 0 } jwts)
        {
            var table = new ConsoleTable();
            table.AddColumns("Id", "Name", "Audience", "Issued", "Expires");

            if (showTokens)
            {
                table.AddColumns("Encoded Token");
            }

            foreach (var jwtRow in jwts)
            {
                var jwt = jwtRow.Value;
                if (showTokens)
                {
                    table.AddRows(jwt.Id, jwt.Name, jwt.Audience, jwt.Issued.ToString("O"), jwt.Expires.ToString("O"), jwt.Token);
                }
                else
                {
                    table.AddRows(jwt.Id, jwt.Name, jwt.Audience, jwt.Issued.ToString("O"), jwt.Expires.ToString("O"));
                }
            }

            table.Write();
        }
        else
        {
            Console.WriteLine("No JWTs created yet!");
        }

        return 0;
    }
}
