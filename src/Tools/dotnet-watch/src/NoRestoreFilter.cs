// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher.Tools
{
    public sealed class NoRestoreFilter : IWatchFilter
    {
        private bool _canUseNoRestore;
        private string[] _noRestoreArguments;

        public ValueTask ProcessAsync(DotNetWatchContext context, CancellationToken cancellationToken)
        {
            if (context.Iteration == 0)
            {
                var arguments = context.ProcessSpec.Arguments;
                _canUseNoRestore = CanUseNoRestore(arguments, context.Reporter);
                if (_canUseNoRestore)
                {
                    // Create run --no-restore <other args>
                    _noRestoreArguments = arguments.Take(1).Append("--no-restore").Concat(arguments.Skip(1)).ToArray();
                    context.Reporter.Verbose($"No restore arguments: {string.Join(" ", _noRestoreArguments)}");
                }
            }
            else if (_canUseNoRestore)
            {
                if (context.RequiresMSBuildRevaluation)
                {
                    context.Reporter.Verbose("Cannot use --no-restore since msbuild project files have changed.");
                }
                else
                {

                    context.Reporter.Verbose("Modifying command to use --no-restore");
                    context.ProcessSpec.Arguments = _noRestoreArguments;
                }
            }

            return default;
        }

        private static bool CanUseNoRestore(IEnumerable<string> arguments, IReporter reporter)
        {
            // For some well-known dotnet commands, we can pass in the --no-restore switch to avoid unnecessary restores between iterations.
            // For now we'll support the "run" and "test" commands.
            if (arguments.Any(a => string.Equals(a, "--no-restore", StringComparison.Ordinal)))
            {
                // Did the user already configure a --no-restore?
                return false;
            }

            var dotnetCommand = arguments.FirstOrDefault();
            if (string.Equals(dotnetCommand, "run", StringComparison.Ordinal) || string.Equals(dotnetCommand, "test", StringComparison.Ordinal))
            {
                reporter.Verbose("Watch command can be configured to use --no-restore.");
                return true;
            }
            else
            {
                reporter.Verbose($"Watch command will not use --no-restore. Unsupport dotnet-command '{dotnetCommand}'.");
                return false;
            }
        }
    }
}
