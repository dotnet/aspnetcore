// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

<<<<<<< HEAD
internal class KeyCommand
=======
internal sealed class KeyCommand
>>>>>>> aed8a228a7 (Add dotnet dev-jwts tool)
{
    public static void Register(CommandLineApplication app)
    {
        app.Command("key", cmd =>
        {
            cmd.Description = "Display or reset the signing key used to issue JWTs";

            var projectOption = cmd.Option(
                "--project",
                "The path of the project to operate on. Defaults to the project in the current directory.",
                CommandOptionType.SingleValue);

            var resetOption = cmd.Option(
                "--reset",
                "Reset the signing key. This will invalidate all previously issued JWTs for this project.",
                CommandOptionType.NoValue);

            cmd.HelpOption("-h|--help");

            cmd.OnExecute(() =>
            {
                return Execute(projectOption.Value(), resetOption.HasValue());
            });
        });
    }

    private static int Execute(string projectPath, bool reset)
    {
        var project = DevJwtCliHelpers.GetProject(projectPath);
        if (project == null)
        {
            Console.WriteLine($"No project found at {projectPath} or current directory.");
            return 1;
        }

        var userSecretsId = DevJwtCliHelpers.GetUserSecretsId(project);

        if (reset == true)
        {
            Console.WriteLine("Are you sure you want to reset the JWT signing key? This will invalidate all JWTs previously issued for this project.\n [Y]es / [N]o");
            if (Console.ReadKey().Key == ConsoleKey.Y)
            {
                var key = DevJwtCliHelpers.CreateSigningKeyMaterial(userSecretsId, reset: true);
                Console.WriteLine($"New signing key created: {Convert.ToBase64String(key)}");
                return 0;
            }

            Console.WriteLine("Key reset canceled.");
            return 0;
        }

        var projectConfiguration = new ConfigurationBuilder()
            .AddUserSecrets(userSecretsId)
            .Build();
        var signingKeyMaterial = projectConfiguration[DevJwtsDefaults.SigningKeyConfigurationKey];

        if (signingKeyMaterial is null)
        {
            Console.WriteLine("Signing key for JWTs was not found. One will be created automatically when the first JWT is created, or you can force creation of a key with the --reset option.");
            return 0;
        }

        Console.WriteLine($"Signing Key: {signingKeyMaterial}");
        return 0;
    }
}
