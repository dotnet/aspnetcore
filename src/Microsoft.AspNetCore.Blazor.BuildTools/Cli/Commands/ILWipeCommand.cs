// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Blazor.BuildTools.Core.ILWipe;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Blazor.BuildTools.Cli.Commands
{
    static class ILWipeCommand
    {
        public static void Command(CommandLineApplication command)
        {
            command.Description = "Wipes code from .NET assemblies.";
            command.HelpOption("-?|-h|--help");

            var inputDirOption = command.Option(
                "-i|--input",
                "The directory containing assemblies from which code should be wiped.",
                CommandOptionType.SingleValue);

            var specFileOption = command.Option(
                "-s|--spec",
                "The directory containing spec files describing which members to wipe from the assemblies.",
                CommandOptionType.SingleValue);

            var verboseOption = command.Option(
                "-v|--verbose",
                "If set, logs additional information to the console.",
                CommandOptionType.NoValue);

            var listOption = command.Option(
                "-l|--list",
                "If set, just writes lists the assembly contents to disk.",
                CommandOptionType.NoValue);

            var outputOption = command.Option(
                "-o|--output",
                "The directory to which the wiped assembly files should be written.",
                CommandOptionType.SingleValue);

            command.OnExecute(() =>
            {
                var inputDir = GetRequiredOptionValue(inputDirOption);
                var outputDir = GetRequiredOptionValue(outputOption);
                var specDir = GetRequiredOptionValue(specFileOption);

                var specFiles = Directory.EnumerateFiles(
                    specDir, "*.txt",
                    new EnumerationOptions { RecurseSubdirectories = true });

                foreach (var specFilePath in specFiles)
                {
                    var specFileRelativePath = Path.GetRelativePath(specDir, specFilePath);
                    var assemblyRelativePath = Path.ChangeExtension(specFileRelativePath, ".dll");
                    var inputAssemblyPath = Path.Combine(inputDir, assemblyRelativePath);
                    var outputAssemblyPath = Path.Combine(outputDir, assemblyRelativePath);
                    var inputAssemblySize = new FileInfo(inputAssemblyPath).Length;

                    if (listOption.HasValue())
                    {
                        var outputList = AssemblyItem
                            .ListContents(inputAssemblyPath)
                            .Select(item => item.ToString());
                        File.WriteAllLines(
                            Path.ChangeExtension(outputAssemblyPath, ".txt"),
                            outputList);
                    }
                    else
                    {
                        WipeAssembly.Exec(
                            inputAssemblyPath,
                            outputAssemblyPath,
                            specFilePath,
                            verboseOption.HasValue());

                        Console.WriteLine(
                            $"{assemblyRelativePath.PadRight(40)} " +
                            $"{FormatFileSize(inputAssemblySize)} ==> " +
                            $"{FormatFileSize(outputAssemblyPath)}");
                    }
                }

                return 0;
            });
        }

        private static string FormatFileSize(string path)
        {
            return FormatFileSize(new FileInfo(path).Length);
        }

        private static string FormatFileSize(long length)
        {
            return string.Format("{0:0.000} MB", ((double)length) / (1024*1024));
        }

        private static string GetRequiredOptionValue(CommandOption option)
        {
            if (!option.HasValue())
            {
                throw new InvalidOperationException($"Missing value for required option '{option.LongName}'.");
            }

            return option.Value();
        }
    }
}
