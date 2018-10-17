// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using McMaster.Extensions.CommandLineUtils;
using System.Threading.Tasks;

namespace TriageBuildFailures.Commands
{
    internal abstract class CommandBase
    {
        private CommandLineApplication _application;

        public void Configure(CommandLineApplication application)
        {
            ConfigureCore(application);

            _application = application;

            application.HelpOption("-h|--help");

            application.OnExecute(
                async () =>
                {
                    if (IsValid())
                    {
                        return await Execute();
                    }
                    else
                    {
                        application.ShowHelp();
                        return 1;
                    }
                });
        }

        protected abstract void ConfigureCore(CommandLineApplication application);

        protected virtual Task<int> Execute()
        {
            _application.ShowHelp();

            return Task.FromResult(0);
        }

        protected abstract bool IsValid();
    }
}
