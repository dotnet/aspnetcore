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
            cmd.Description = Resources.PrintCommand_Description;

            var idArgument = cmd.Argument("[id]", Resources.PrintCommand_IdArgument_Description);
            var showAllOption = cmd.Option("--show-all", Resources.PrintCommand_ShowAllOption_Description, CommandOptionType.NoValue);

            cmd.HelpOption("-h|--help");

            cmd.OnExecute(() =>
            {
                if (idArgument.Value is null)
                {
                    cmd.ShowHelp();
                    return 0;
                }
                if (!cmd.TryGetProjectOrFilePath(out var projectPath, out var isFileBasedApp))
                {
                    return 1;
                }

                return Execute(
                    cmd.Reporter,
                    projectPath,
                    isFileBasedApp,
                    idArgument.Value,
                    showAllOption.HasValue(),
                    cmd.OutputOption.Value());
            });
        });
    }

    private static int Execute(IReporter reporter, string projectPath, bool isFileBasedApp, string id, bool showAll, string outputFormat)
    {
        if (!DevJwtCliHelpers.GetProjectAndSecretsId(projectPath, isFileBasedApp, reporter, out var _, out var userSecretsId))
        {
            return 1;
        }
        var jwtStore = new JwtStore(userSecretsId);

        if (!jwtStore.Jwts.TryGetValue(id, out var jwt))
        {
            reporter.Output(Resources.FormatPrintCommand_NoJwtFound(id));
            return 1;
        }

        JwtSecurityToken fullToken = JwtIssuer.Extract(jwt.Token);
        DevJwtCliHelpers.PrintJwt(reporter, jwt, showAll, outputFormat, fullToken);

        return 0;
    }
}
