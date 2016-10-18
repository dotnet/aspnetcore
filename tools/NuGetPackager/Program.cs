// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.DotNet.Cli.Utils;

namespace NuGetPackager
{
    /// <summary>
    /// This replaces the "dotnet-pack" command, which doesn't not yet support "package types"
    /// and probably won't in time for the next release.
    /// TODO remove this once CLI supports package type
    /// </summary>
    public class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication();
            var optOutput = app.Option("-o|--output-dir <dir>", "Output dir", CommandOptionType.SingleValue);
            var optConfig = app.Option("-c|--configuration <configuration>", "Config", CommandOptionType.SingleValue);
            var optsNuspec = app.Option("-n|--nuspec <nuspec>", "nuspec", CommandOptionType.MultipleValue);

            app.OnExecute(async () =>
            {
                if (!optsNuspec.Values.Any())
                {
                    Reporter.Error.WriteLine("Missing values for --nuspec");
                    return 1;
                }

                var config = optConfig.HasValue()
                    ? optConfig.Value()
                    : "Debug";
                var output = optOutput.Value() ?? Directory.GetCurrentDirectory();

                if (!Path.IsPathRooted(output))
                {
                    output = Path.Combine(Directory.GetCurrentDirectory(), output);
                }

                var packer = new PackCommand(Directory.GetCurrentDirectory());
                foreach (var nuspec in optsNuspec.Values)
                {
                    await packer.PackAsync(nuspec, config, output);
                }

                return 0;
            });

            return app.Execute(args);
        }
    }
}