// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;
internal sealed class PrintCommand
{
    public static void Register(ProjectCommandLineApplication app)
    {
        app.Command("print", cmd =>
        {
            cmd.Description = "Print the details of a given JWT";

            var idArgument = cmd.Argument("[id]", "The ID of the JWT to print");

            var showFullOption = cmd.Option(
                "--show-full",
                "Whether to show the full JWT contents in addition to the compact serialized format",
                CommandOptionType.NoValue);

            cmd.HelpOption("-h|--help");

            cmd.OnExecute(() =>
            {
                if (idArgument.Value is null)
                {
                    cmd.ShowHelp();
                    return 0;
                }
                return Execute(cmd.Reporter, cmd.ProjectOption.Value(), idArgument.Value, showFullOption.HasValue());
            });
        });
    }

    private static int Execute(IReporter reporter, string projectPath, string id, bool showFull)
    {
        if (!DevJwtCliHelpers.GetProjectAndSecretsId(projectPath, reporter, out var _, out var userSecretsId))
        {
            return 1;
        }
        var jwtStore = new JwtStore(userSecretsId);

        if (!jwtStore.Jwts.ContainsKey(id))
        {
            reporter.Output($"No token with ID '{id}' found");
            return 1;
        }

        reporter.Output($"Found JWT with ID '{id}'");
        var jwt = jwtStore.Jwts[id];
        JwtSecurityToken fullToken;

        if (showFull)
        {
            fullToken = JwtIssuer.Extract(jwt.Token);
            DevJwtCliHelpers.PrintJwt(reporter, jwt, fullToken);
        }

        return 0;
    }
}
