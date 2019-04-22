// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.DotNet.OpenApi.Commands;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.DotNet.OpenApi
{
    internal class Application : CommandLineApplication
    {
        static Application()
        {
            MSBuildLocator.RegisterDefaults();
        }

        public Application(
            string workingDirectory,
            Func<string, Task<Stream>> downloadProvider,
            TextWriter output = null,
            TextWriter error = null)
        {
            DownloadProvider = downloadProvider;
            Out = output ?? Out;
            Error = error ?? Error;

            WorkingDirectory = workingDirectory;

            Name = "openapi";
            FullName = "OpenApi reference management tool";
            Description = "OpenApi reference management operations.";
            ShortVersionGetter = GetInformationalVersion;

            HelpOption("-?|-h|--help");

            Invoke = () => {
                ShowHelp();
                return 0;
            };

            Commands.Add(new AddCommand(this));
            Commands.Add(new RemoveCommand(this));
            Commands.Add(new RefreshCommand(this));
        }

        public Func<string, Task<Stream>> DownloadProvider { get; }

        public string WorkingDirectory { get; }

        public new int Execute(params string[] args)
        {
            try
            {
                return base.Execute(args);
            }
            catch (AggregateException ex) when (ex.InnerException != null)
            {
                foreach (var innerException in ex.Flatten().InnerExceptions)
                {
                    Error.WriteLine(innerException.Message);
                    if (!(innerException is ArgumentException))
                    {
                        Error.WriteLine(innerException.StackTrace);
                    }
                }
                return 1;
            }

            catch (ArgumentException ex)
            {
                // Don't show a call stack when we have unneeded arguments, just print the error message.
                // The code that throws this exception will print help, so no need to do it here.
                Error.WriteLine(ex.Message);
                return 1;
            }
            catch (CommandParsingException ex)
            {
                // Don't show a call stack when we have unneeded arguments, just print the error message.
                // The code that throws this exception will print help, so no need to do it here.
                Error.WriteLine(ex.Message);
                return 1;
            }
            catch (OperationCanceledException)
            {
                // This is a cancellation, not a failure.
                Error.WriteLine("Cancelled");
                return 1;
            }
            catch (Exception ex)
            {
                Error.WriteLine(ex.Message);
                Error.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        private string GetInformationalVersion()
        {
            var assembly = typeof(Application).GetTypeInfo().Assembly;
            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return attribute.InformationalVersion;
        }
    }
}
