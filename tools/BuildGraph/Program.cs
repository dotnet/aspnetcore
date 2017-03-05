using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using UniverseTools;

namespace BuildGraph
{
    class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandLineApplication();
            var outputTypeOption = app.Option("--output-type",
                "Output type of generated graph. Valid values are: msbuild, and dgml.",
                CommandOptionType.SingleValue);

            var repositoriesRootOption = app.Option("-r|--repositories-root",
                "Directory containing repositories to calculate graph for.",
                CommandOptionType.SingleValue);

            var packageSpecsDirectoryOption = app.Option("--graph-specs-root",
                "Directory containing package specs. (Optional)",
                CommandOptionType.SingleValue);

            var outputPathArgument = app.Argument("Output path", "Output path");

            app.OnExecute(() =>
            {
                if (!repositoriesRootOption.HasValue())
                {
                    Console.Error.WriteLine($"Option {repositoriesRootOption.Template} must have a value.");
                    return 1;
                }

                var outputPath = outputPathArgument.Value;
                if (string.IsNullOrEmpty(outputPath))
                {
                    Console.Error.WriteLine($"Output path not specified.");
                    return 1;
                }

                var outputDirectory = Path.GetDirectoryName(outputPath);
                Directory.CreateDirectory(outputDirectory);

                var outputType = outputTypeOption.Value() ?? "msbuild";


                var graphSpecProvider = packageSpecsDirectoryOption.HasValue()
                    ? new DependencyGraphSpecProvider(packageSpecsDirectoryOption.Value().Trim())
                    : DependencyGraphSpecProvider.Default;
                IList<Repository> repositories;
                using (graphSpecProvider)
                {
                    repositories = Repository.ReadAllRepositories(repositoriesRootOption.Value().Trim(), graphSpecProvider);
                }

                var graph = GraphBuilder.Generate(repositories);
                GraphFormatter formatter;
                switch (outputType)
                {
                    case "msbuild":
                        formatter = new MSBuildGraphFormatter();
                        break;
                    case "dgml":
                        formatter = new DGMLFormatter();
                        break;
                    default:
                        app.Error.WriteLine($"Unknown output type: {outputType}.");
                        return 1;
                }

                formatter.Format(graph, outputPathArgument.Value);

                return 0;
            });

            return app.Execute(args);
        }
    }
}