using Microsoft.Blazor.BuildTools.Cli;
using Microsoft.Extensions.CommandLineUtils;
using System;

namespace Microsoft.Blazor.BuildTools
{
    static class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "blazor-buildtools"
            };
            app.HelpOption("-?|-h|--help");

            app.Command("checknodejs", CheckNodeJsInstalled.Command);

            if (args.Length > 0)
            {
                return app.Execute(args);
            }
            else
            {
                app.ShowHelp();
                return 0;
            }
        }
    }
}
