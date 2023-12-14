// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

public class Program
{
    private readonly IConsole _console;
    private readonly IReporter _reporter;

    public Program(IConsole console)
    {
        _console = console;
        _reporter = new ConsoleReporter(console);
    }

    // For testing.
    internal string UserJwtsFilePath { get; set; }

    public static void Main(string[] args)
    {
        new Program(PhysicalConsole.Singleton).Run(args);
    }

    public void Run(string[] args)
    {
        ProjectCommandLineApplication userJwts = new(_reporter)
        {
            Name = "dotnet user-jwts"
        };

        userJwts.HelpOption("-h|--help");

        // dotnet user-jwts list
        ListCommand.Register(userJwts);
        // dotnet user-jwts create
        CreateCommand.Register(userJwts, this);
        // dotnet user-jwts print ecd045
        PrintCommand.Register(userJwts);
        // dotnet user-jwts remove ecd045
        RemoveCommand.Register(userJwts);
        // dotnet user-jwts clear
        ClearCommand.Register(userJwts);
        // dotnet user-jwts key
        KeyCommand.Register(userJwts);

        // Show help information if no subcommand/option was specified.
        userJwts.OnExecute(() => userJwts.ShowHelp());

        try
        {
            userJwts.Execute(args);
        }
        catch (CommandParsingException parsingException)
        {
            _reporter.Error(parsingException.Message);
            userJwts.ShowHelp();
        }
        catch (Exception ex)
        {
            _reporter.Error(ex.Message);
        }
    }
}
