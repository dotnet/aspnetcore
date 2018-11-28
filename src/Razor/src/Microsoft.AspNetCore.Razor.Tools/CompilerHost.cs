// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal abstract class CompilerHost
    {
        public static CompilerHost Create()
        {
            return new DefaultCompilerHost();
        }

        public abstract ServerResponse Execute(ServerRequest request, CancellationToken cancellationToken);

        private class DefaultCompilerHost : CompilerHost
        {
            public DefaultCompilerHost()
            {
                // The loader needs to live for the lifetime of the server. 
                //
                // This means that if a request tries to use a set of binaries that are inconsistent with what
                // the server already has, then it will be rejected to try again on the client.
                //
                // We also check each set of extensions for missing depenencies individually, so that we can
                // consistently reject a request that doesn't specify everything it needs. Otherwise the request
                // could succeed sometimes if it relies on transient state.
                Loader = new DefaultExtensionAssemblyLoader(Path.Combine(Path.GetTempPath(), "Razor-Server"));

                AssemblyReferenceProvider = (path, properties) => new CachingMetadataReference(path, properties);
            }

            public Func<string, MetadataReferenceProperties, PortableExecutableReference> AssemblyReferenceProvider { get; }

            public ExtensionAssemblyLoader Loader { get; }

            public override ServerResponse Execute(ServerRequest request, CancellationToken cancellationToken)
            {
                if (!TryParseArguments(request, out var parsed))
                {
                    return new RejectedServerResponse();
                }

                var exitCode = 0;
                var commandArgs = parsed.args.ToArray();

                var outputWriter = new StringWriter();
                var errorWriter = new StringWriter();

                var checker = new DefaultExtensionDependencyChecker(Loader, outputWriter, errorWriter);
                var app = new Application(cancellationToken, Loader, checker, AssemblyReferenceProvider, outputWriter, errorWriter);

                exitCode = app.Execute(commandArgs);

                var output = outputWriter.ToString();
                var error = errorWriter.ToString();

                outputWriter.Dispose();
                errorWriter.Dispose();

                // This will no-op if server logging is not enabled.
                ServerLogger.Log(output);
                ServerLogger.Log(error);

                return new CompletedServerResponse(exitCode, utf8output: false, output, error);
            }

            private bool TryParseArguments(ServerRequest request, out (string workingDirectory, string tempDirectory, string[] args) parsed)
            {
                string workingDirectory = null;
                string tempDirectory = null;

                var args = new List<string>(request.Arguments.Count);

                for (var i = 0; i < request.Arguments.Count; i++)
                {
                    var argument = request.Arguments[i];
                    if (argument.Id == RequestArgument.ArgumentId.CurrentDirectory)
                    {
                        workingDirectory = argument.Value;
                    }
                    else if (argument.Id == RequestArgument.ArgumentId.TempDirectory)
                    {
                        tempDirectory = argument.Value;
                    }
                    else if (argument.Id == RequestArgument.ArgumentId.CommandLineArgument)
                    {
                        args.Add(argument.Value);
                    }
                }

                ServerLogger.Log($"WorkingDirectory = '{workingDirectory}'");
                ServerLogger.Log($"TempDirectory = '{tempDirectory}'");
                for (var i = 0; i < args.Count; i++)
                {
                    ServerLogger.Log($"Argument[{i}] = '{request.Arguments[i]}'");
                }

                if (string.IsNullOrEmpty(workingDirectory))
                {
                    ServerLogger.Log($"Rejecting build due to missing working directory");

                    parsed = default;
                    return false;
                }

                if (string.IsNullOrEmpty(tempDirectory))
                {
                    ServerLogger.Log($"Rejecting build due to missing temp directory");

                    parsed = default;
                    return false;
                }

                if (string.IsNullOrEmpty(tempDirectory))
                {
                    ServerLogger.Log($"Rejecting build due to missing temp directory");

                    parsed = default;
                    return false;
                }

                parsed = (workingDirectory, tempDirectory, args.ToArray());
                return true;
            }
        }
    }
}
