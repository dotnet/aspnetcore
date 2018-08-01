// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Repl;
using Microsoft.Repl.Commanding;
using Microsoft.Repl.Parsing;
using Microsoft.HttpRepl.Commands;

namespace Microsoft.HttpRepl
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var state = new HttpState();
            var dispatcher = DefaultCommandDispatcher.Create(state.GetPrompt, state);

            dispatcher.AddCommand(new ChangeDirectoryCommand());
            dispatcher.AddCommand(new ClearCommand());
            //dispatcher.AddCommand(new ConfigCommand());
            dispatcher.AddCommand(new DeleteCommand());
            dispatcher.AddCommand(new EchoCommand());
            dispatcher.AddCommand(new ExitCommand());
            dispatcher.AddCommand(new HeadCommand());
            dispatcher.AddCommand(new HelpCommand());
            dispatcher.AddCommand(new GetCommand());
            dispatcher.AddCommand(new ListCommand());
            dispatcher.AddCommand(new OptionsCommand());
            dispatcher.AddCommand(new PatchCommand());
            dispatcher.AddCommand(new PrefCommand());
            dispatcher.AddCommand(new PostCommand());
            dispatcher.AddCommand(new PutCommand());
            dispatcher.AddCommand(new RunCommand());
            dispatcher.AddCommand(new SetBaseCommand());
            dispatcher.AddCommand(new SetDiagCommand());
            dispatcher.AddCommand(new SetHeaderCommand());
            dispatcher.AddCommand(new SetSwaggerCommand());
            dispatcher.AddCommand(new UICommand());

            CancellationTokenSource source = new CancellationTokenSource();
            var shell = new Shell(dispatcher);
            shell.ShellState.ConsoleManager.AddBreakHandler(() => source.Cancel());
            if (args.Length > 0)
            {
                shell.ShellState.CommandDispatcher.OnReady(shell.ShellState);
                shell.ShellState.InputManager.SetInput(shell.ShellState, $"set base \"{args[0]}\"");
                await shell.ShellState.CommandDispatcher.ExecuteCommandAsync(shell.ShellState, CancellationToken.None).ConfigureAwait(false);
            }
            Task result = shell.RunAsync(source.Token);
            await result.ConfigureAwait(false);
        }
    }
}
