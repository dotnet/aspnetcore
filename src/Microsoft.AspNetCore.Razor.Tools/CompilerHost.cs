// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal abstract class CompilerHost
    {
        public static CompilerHost Create()
        {
            return new DefaultCompilerHost();
        }

        public abstract BuildResponse Execute(BuildRequest request, CancellationToken cancellationToken);

        private class DefaultCompilerHost : CompilerHost
        {
            public override BuildResponse Execute(BuildRequest request, CancellationToken cancellationToken)
            {
                if (!TryParseArguments(request, out var parsed))
                {
                    return new RejectedBuildResponse();
                }

                var app = new Application(cancellationToken);
                var commandArgs = parsed.args.ToArray();

                var exitCode = app.Execute(commandArgs);
                var output = app.Out.ToString() ?? string.Empty;

                return new CompletedBuildResponse(exitCode, utf8output: false, output: output);
            }

            private bool TryParseArguments(BuildRequest request, out (string workingDirectory, string tempDirectory, string[] args) parsed)
            {
                string workingDirectory = null;
                string tempDirectory = null;

                var args = new List<string>(request.Arguments.Count);

                for (var i = 0; i < request.Arguments.Count; i++)
                {
                    var argument = request.Arguments[i];
                    if (argument.ArgumentId == BuildProtocolConstants.ArgumentId.CurrentDirectory)
                    {
                        workingDirectory = argument.Value;
                    }
                    else if (argument.ArgumentId == BuildProtocolConstants.ArgumentId.TempDirectory)
                    {
                        tempDirectory = argument.Value;
                    }
                    else if (argument.ArgumentId == BuildProtocolConstants.ArgumentId.CommandLineArgument)
                    {
                        args.Add(argument.Value);
                    }
                }

                CompilerServerLogger.Log($"WorkingDirectory = '{workingDirectory}'");
                CompilerServerLogger.Log($"TempDirectory = '{tempDirectory}'");
                for (var i = 0; i < args.Count; i++)
                {
                    CompilerServerLogger.Log($"Argument[{i}] = '{request.Arguments[i]}'");
                }

                if (string.IsNullOrEmpty(workingDirectory))
                {
                    CompilerServerLogger.Log($"Rejecting build due to missing working directory");

                    parsed = default;
                    return false;
                }

                if (string.IsNullOrEmpty(tempDirectory))
                {
                    CompilerServerLogger.Log($"Rejecting build due to missing temp directory");

                    parsed = default;
                    return false;
                }

                if (string.IsNullOrEmpty(tempDirectory))
                {
                    CompilerServerLogger.Log($"Rejecting build due to missing temp directory");

                    parsed = default;
                    return false;
                }

                parsed = (workingDirectory, tempDirectory, args.ToArray());
                return true;
            }
        }
    }
}
