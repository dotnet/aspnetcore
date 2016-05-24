// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Watcher.Core;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watcher.Tools
{
    public class Program
    {
        private readonly ILoggerFactory _loggerFactory;

        public Program()
        {
            _loggerFactory = new LoggerFactory();

            var commandProvider = new CommandOutputProvider();
            _loggerFactory.AddProvider(commandProvider);
        }

        public static int Main(string[] args)
        {
            using (CancellationTokenSource ctrlCTokenSource = new CancellationTokenSource())
            {
                Console.CancelKeyPress += (sender, ev) =>
                {
                    ctrlCTokenSource.Cancel();
                    ev.Cancel = false;
                };
                
                return new Program().MainInternal(args, ctrlCTokenSource.Token);
            }
        }

        private int MainInternal(string[] args, CancellationToken cancellationToken)
        {
            var app = new CommandLineApplication();
            app.Name = "dotnet-watch";
            app.FullName = "Microsoft dotnet File Watcher";

            app.HelpOption("-?|-h|--help");
 
            app.OnExecute(() =>
            {
                var projectToWatch = Path.Combine(Directory.GetCurrentDirectory(), ProjectModel.Project.FileName);
                var watcher = DotNetWatcher.CreateDefault(_loggerFactory);

                try
                {
                    watcher.WatchAsync(projectToWatch, args, cancellationToken).Wait();
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerExceptions.Count != 1 || !(ex.InnerException is TaskCanceledException))
                    {
                        throw;
                    }
                }

                return 0;
            });

            if (args == null ||
                args.Length == 0 ||
                args[0].Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                args[0].Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                args[0].Equals("-?", StringComparison.OrdinalIgnoreCase))
            {
                app.ShowHelp();
                return 1;
            }

            return app.Execute();
        }
    }
}
