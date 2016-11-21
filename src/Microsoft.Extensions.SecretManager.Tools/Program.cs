// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.SecretManager.Tools.Internal;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.SecretManager.Tools
{
    public class Program
    {
        private ILogger _logger;
        private CommandOutputProvider _loggerProvider;
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

            var loggerFactory = new LoggerFactory();
            CommandOutputProvider = new CommandOutputProvider();
            loggerFactory.AddProvider(CommandOutputProvider);
            Logger = loggerFactory.CreateLogger<Program>();
        }

        public ILogger Logger
        {
            get { return _logger; }
            set { _logger = Ensure.NotNull(value, nameof(value)); }
        }

        public CommandOutputProvider CommandOutputProvider
        {
            get { return _loggerProvider; }
            set { _loggerProvider = Ensure.NotNull(value, nameof(value)); }
        }

        public bool TryRun(string[] args, out int returnCode)
        {
            try
            {
                returnCode = RunInternal(args);
                return true;
            }
            catch (Exception exception)
            {
                Logger.LogDebug(exception.ToString());
                Logger.LogCritical(Resources.Error_Command_Failed, exception.Message);
                returnCode = 1;
                return false;
            }
        }

        internal int RunInternal(params string[] args)
        {
            var options = CommandLineOptions.Parse(args, _console);

            if (options == null)
            {
                return 1;
            }

            if (options.IsHelp)
            {
                return 2;
            }

            if (options.IsVerbose)
            {
                CommandOutputProvider.LogLevel = LogLevel.Debug;
            }

            string userSecretsId;
            try
            {
                userSecretsId = ResolveId(options);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is FileNotFoundException)
            {
                _logger.LogError(ex.Message);
                return 1;
            }

            var store = new SecretsStore(userSecretsId, Logger);
            var context = new Internal.CommandContext(store, Logger, _console);
            options.Command.Execute(context);
            return 0;
        }

        internal string ResolveId(CommandLineOptions options)
        {
            if (!string.IsNullOrEmpty(options.Id))
            {
                return options.Id;
            }

            using (var resolver = new ProjectIdResolver(Logger, _workingDirectory))
            {
                return resolver.Resolve(options.Project, options.Configuration);
            }
        }
    }
}