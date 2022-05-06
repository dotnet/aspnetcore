// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

<<<<<<< HEAD
internal class PrintCommand
=======
internal sealed class PrintCommand
>>>>>>> aed8a228a7 (Add dotnet dev-jwts tool)
{
    public static void Register(CommandLineApplication app)
    {
        app.Command("print", cmd =>
        {
            cmd.Description = "Print the details of a given JWT";

            var idArgument = cmd.Argument("id", "The ID of the JWT to print");

            var projectOption = cmd.Option(
                "--project",
                "The path of the project to operate on. Defaults to the project in the current directory.",
                CommandOptionType.SingleValue);

            var showFullOption = cmd.Option(
                "--show-full",
                "Whether to show the full JWT contents in addition to the compact serialized format",
                CommandOptionType.NoValue);

            cmd.HelpOption("-h|--help");

            cmd.OnExecute(() =>
            {
                return Execute(projectOption.Value(), idArgument.Value, showFullOption.HasValue());
            });
        });
    }

    private static int Execute(string projectPath, string id, bool showFull)
    {
        var project = DevJwtCliHelpers.GetProject(projectPath);
        if (project == null)
        {
            Console.WriteLine($"No project found at `--project` path or current directory.");
            return 1;
        }

        var userSecretsId = DevJwtCliHelpers.GetUserSecretsId(project);
        var jwtStore = new JwtStore(userSecretsId);

        if (!jwtStore.Jwts.ContainsKey(id))
        {
            Console.WriteLine($"No token with ID '{id}' found");
            return 1;
        }

        Console.WriteLine($"Found JWT with ID '{id}'");
        var jwt = jwtStore.Jwts[id];
        JwtSecurityToken fullToken;
        if (showFull)
        {
            var keyMaterial = DevJwtCliHelpers.GetOrCreateSigningKeyMaterial(userSecretsId);
            var jwtIssuer = new JwtIssuer(DevJwtsDefaults.Issuer, keyMaterial);
            fullToken = jwtIssuer.Extract(jwt.Token);
            DevJwtCliHelpers.PrintJwt(jwt, fullToken);
        }

        return 0;
    }
}
