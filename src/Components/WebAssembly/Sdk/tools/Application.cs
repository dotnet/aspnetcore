// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.NET.Sdk.BlazorWebAssembly.Tools
{
    internal class Application : CommandLineApplication
    {
        public Application(
            CancellationToken cancellationToken,
            TextWriter output = null,
            TextWriter error = null)
        {
            CancellationToken = cancellationToken;
            Out = output ?? Out;
            Error = error ?? Error;

            Name = "BlazorWebAssembly.Tools";
            FullName = "Microsoft Blazor WebAssembly SDK tool";
            Description = "CLI for Blazor WebAssembly operations.";
            ShortVersionGetter = GetInformationalVersion;

            HelpOption("-?|-h|--help");

            Commands.Add(new BrotliCompressCommand(this));
        }

        public CancellationToken CancellationToken { get; }

        public new int Execute(params string[] args)
        {
            try
            {
                return base.Execute(ExpandResponseFiles(args));
            }
            catch (AggregateException ex) when (ex.InnerException != null)
            {
                foreach (var innerException in ex.Flatten().InnerExceptions)
                {
                    Error.WriteLine(innerException.Message);
                    Error.WriteLine(innerException.StackTrace);
                }
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
            var assembly = typeof(Application).Assembly;
            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return attribute.InformationalVersion;
        }

        private static string[] ExpandResponseFiles(string[] args)
        {
            var expandedArgs = new List<string>();
            foreach (var arg in args)
            {
                if (!arg.StartsWith("@", StringComparison.Ordinal))
                {
                    expandedArgs.Add(arg);
                }
                else
                {
                    var fileName = arg.Substring(1);
                    expandedArgs.AddRange(File.ReadLines(fileName));
                }
            }

            return expandedArgs.ToArray();
        }
    }
}
