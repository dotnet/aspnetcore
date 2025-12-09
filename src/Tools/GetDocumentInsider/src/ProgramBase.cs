// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.Extensions.ApiDescription.Tool.Commands;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.ApiDescription.Tool;

internal abstract class ProgramBase
{
    private readonly IConsole _console;
    private readonly IReporter _reporter;

    public ProgramBase(IConsole console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _reporter = new ConsoleReporter(_console, verbose: false, quiet: false);
    }

    protected static IConsole GetConsole()
    {
        var console = PhysicalConsole.Singleton;
        if (console.IsOutputRedirected)
        {
            Console.OutputEncoding = Encoding.UTF8;
        }

        return console;
    }

    // Internal for testing
    internal int Run(string[] args, CommandBase command, bool throwOnUnexpectedArg)
    {
        try
        {
            // AllowArgumentSeparator and continueAfterUnexpectedArg are ignored when !throwOnUnexpectedArg _except_
            // AllowArgumentSeparator=true changes the help text (ignoring throwOnUnexpectedArg).
            var app = new CommandLineApplication(throwOnUnexpectedArg, continueAfterUnexpectedArg: true)
            {
                AllowArgumentSeparator = !throwOnUnexpectedArg,
                Error = _console.Error,
                FullName = Resources.CommandFullName,
                Name = Resources.CommandFullName,
                Out = _console.Out,
            };

            command.Configure(app);

            return app.Execute(args);
        }
        catch (Exception ex)
        {
            if (ex is CommandException || ex is CommandParsingException)
            {
                _reporter.WriteError(ex.Message);
            }
            else
            {
                _reporter.WriteError(ex.ToString());
            }

            return 1;
        }
    }
}
