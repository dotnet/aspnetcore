// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.SecretManager.Tools.Internal;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.SecretManager.Tools;

public class Program
{
    private readonly IConsole _console;
    private readonly string _workingDirectory;

    public static int Main(string[] args)
    {
        DebugHelper.HandleDebugSwitch(ref args);

        int rc;
        new Program(PhysicalConsole.Singleton, Directory.GetCurrentDirectory()).TryRun(args, out rc);
        return rc;
    }

    public Program(IConsole console, string workingDirectory)
    {
        _console = console;
        _workingDirectory = workingDirectory;
    }

    // For testing.
    internal string SecretsFilePath { get; private set; }

    public bool TryRun(string[] args, out int returnCode)
    {
        try
        {
            returnCode = RunInternal(args);
            return true;
        }
        catch (Exception exception)
        {
            var reporter = CreateReporter(verbose: true);
            reporter.Verbose(exception.ToString());
            reporter.Error(Resources.FormatError_Command_Failed(exception.Message));
            returnCode = 1;
            return false;
        }
    }

    internal int RunInternal(params string[] args)
    {
        CommandLineOptions options;
        try
        {
            options = CommandLineOptions.Parse(args, _console);
        }
        catch (CommandParsingException ex)
        {
            CreateReporter(verbose: false).Error(ex.Message);
            return 1;
        }

        if (options == null)
        {
            return 1;
        }

        if (options.IsHelp)
        {
            return 2;
        }

        var reporter = CreateReporter(options.IsVerbose);

        if (options.Command is InitCommandFactory initCmd)
        {
            initCmd.Execute(new CommandContext(null, reporter, _console), _workingDirectory);
            return 0;
        }

        var userSecretsId = ResolveId(options, reporter);

        if (string.IsNullOrEmpty(userSecretsId))
        {
            return 1;
        }

        var store = new SecretsStore(userSecretsId, reporter);
        var context = new Internal.CommandContext(store, reporter, _console);
        options.Command.Execute(context);

        // For testing.
        SecretsFilePath = store.SecretsFilePath;

        return 0;
    }

    private IReporter CreateReporter(bool verbose)
        => new ConsoleReporter(_console, verbose, quiet: false);

    internal string ResolveId(CommandLineOptions options, IReporter reporter)
    {
        if (!string.IsNullOrEmpty(options.Id))
        {
            return options.Id;
        }

        var resolver = new ProjectIdResolver(reporter, _workingDirectory);
        return resolver.Resolve(options.Project, options.Configuration);
    }
}
