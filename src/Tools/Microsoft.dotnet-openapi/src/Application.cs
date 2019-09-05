// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.Build.Locator;
using Microsoft.DotNet.Openapi.Tools;
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
            IHttpClientWrapper httpClient,
            TextWriter output = null,
            TextWriter error = null)
        {
            Out = output ?? Out;
            Error = error ?? Error;

            WorkingDirectory = workingDirectory;

            Name = "openapi";
            FullName = "OpenApi reference management tool";
            Description = "OpenApi reference management operations.";
            ShortVersionGetter = GetInformationalVersion;

            Help = HelpOption("-?|-h|--help");
            Help.Inherited = true;

            Invoke = () =>
            {
                ShowHelp();
                return 0;
            };

            Commands.Add(new AddCommand(this, httpClient));
            Commands.Add(new RemoveCommand(this, httpClient));
            Commands.Add(new RefreshCommand(this, httpClient));
        }

        public string WorkingDirectory { get; }

        public CommandOption Help { get; }

        public new int Execute(params string[] args)
        {
            try
            {
                return base.Execute(args);
            }
            catch (AggregateException ex) when (ex.InnerException != null)
            {
                foreach (var innerException in ex.InnerExceptions)
                {
                    Error.WriteLine(ex.InnerException.Message);
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
                Error.WriteLine(ex);
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
