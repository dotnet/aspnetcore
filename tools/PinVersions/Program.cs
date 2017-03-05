using System;
using Microsoft.Extensions.CommandLineUtils;
using UniverseTools;

namespace PinVersions
{
    class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandLineApplication();

            var pinSourceOption = app.Option("-s|--source",
                "Feed containing packages to pin.",
                CommandOptionType.MultipleValue);

            var packageSpecsDirectoryOption = app.Option("--graph-specs-root",
               "Directory containing package specs. (Optional)",
               CommandOptionType.SingleValue);

            var repositoryArgument = app.Argument("Repository", "Repository directory");

            app.OnExecute(() =>
            {
                if (!pinSourceOption.HasValue())
                {
                    Console.Error.WriteLine($"Option {pinSourceOption.Template} must have a value.");
                    return 1;
                }

                if (string.IsNullOrEmpty(repositoryArgument.Value))
                {
                    Console.Error.WriteLine($"Repository argument must be specified.");
                    return 1;
                }

                var graphSpecProvider = packageSpecsDirectoryOption.HasValue() ?
                    new DependencyGraphSpecProvider(packageSpecsDirectoryOption.Value().Trim()) :
                    DependencyGraphSpecProvider.Default;

                using (graphSpecProvider)
                {
                    var pinVersionUtility = new PinVersionUtility(repositoryArgument.Value.Trim(), pinSourceOption.Values, graphSpecProvider);
                    pinVersionUtility.Execute();
                }

                return 0;
            });

            return app.Execute(args);
        }
    }
}