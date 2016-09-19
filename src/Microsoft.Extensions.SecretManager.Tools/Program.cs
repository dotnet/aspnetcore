// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.SecretManager.Tools.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.SecretManager.Tools
{
    public class Program
    {
        private ILogger _logger;
        private CommandOutputProvider _loggerProvider;
        private readonly TextWriter _consoleOutput;
        private readonly string _workingDirectory;

        public Program()
            : this(Console.Out, Directory.GetCurrentDirectory())
        {
        }

        internal Program(TextWriter consoleOutput, string workingDirectory)
        {
            _consoleOutput = consoleOutput;
            _workingDirectory = workingDirectory;

            var loggerFactory = new LoggerFactory();
            CommandOutputProvider = new CommandOutputProvider();
            loggerFactory.AddProvider(CommandOutputProvider);
            Logger = loggerFactory.CreateLogger<Program>();
        }

        public ILogger Logger
        {
            get { return _logger; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _logger = value;
            }
        }

        public CommandOutputProvider CommandOutputProvider
        {
            get { return _loggerProvider; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _loggerProvider = value;
            }
        }

        public static int Main(string[] args)
        {
            HandleDebugFlag(ref args);

            int rc;
            new Program().TryRun(args, out rc);
            return rc;
        }

        [Conditional("DEBUG")]
        private static void HandleDebugFlag(ref string[] args)
        {
            for (var i = 0; i < args.Length; ++i)
            {
                if (args[i] == "--debug")
                {
                    Console.WriteLine("Process ID " + Process.GetCurrentProcess().Id);
                    Console.WriteLine("Paused for debugger. Press ENTER to continue");
                    Console.ReadLine();

                    args = args.Take(i).Concat(args.Skip(i + 1)).ToArray();

                    return;
                }
            }
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
                if (exception is GracefulException)
                {
                    Logger.LogError(exception.Message);
                }
                else
                {
                    Logger.LogDebug(exception.ToString());
                    Logger.LogCritical(Resources.Error_Command_Failed, exception.Message);
                }
                returnCode = 1;
                return false;
            }
        }

        internal int RunInternal(params string[] args)
        {
            var options = CommandLineOptions.Parse(args, _consoleOutput);

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

            var userSecretsId = ResolveUserSecretsId(options);
            var store = new SecretsStore(userSecretsId, Logger);
            options.Command.Execute(store, Logger);
            return 0;
        }

        private string ResolveUserSecretsId(CommandLineOptions options)
        {
            var projectPath = options.Project ?? _workingDirectory;

            if (!projectPath.EndsWith("project.json", StringComparison.OrdinalIgnoreCase))
            {
                projectPath = Path.Combine(projectPath, "project.json");
            }

            var fileInfo = new PhysicalFileInfo(new FileInfo(projectPath));

            Logger.LogDebug(Resources.Message_Project_File_Path, fileInfo.PhysicalPath);
            return ReadUserSecretsId(fileInfo);
        }

        // TODO can use runtime API when upgrading to 1.1
        private string ReadUserSecretsId(IFileInfo fileInfo)
        {
            if (fileInfo == null || !fileInfo.Exists)
            {
                throw new GracefulException($"Could not find file '{fileInfo.PhysicalPath}'");
            }

            using (var stream = fileInfo.CreateReadStream())
            using (var streamReader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                var obj = JObject.Load(jsonReader);

                var userSecretsId = obj.Value<string>("userSecretsId");

                if (string.IsNullOrEmpty(userSecretsId))
                {
                    throw new GracefulException($"Could not find 'userSecretsId' in json file '{fileInfo.PhysicalPath}'");
                }

                return userSecretsId;
            }
        }
    }
}