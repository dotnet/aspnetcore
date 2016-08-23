// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.DotNet.Cli.Utils;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation
{
    public static class CommandResultExtensions
    {
        public static CommandResult EnsureSuccessful(this CommandResult commandResult)
        {
            var startInfo = commandResult.StartInfo;

            Assert.True(commandResult.ExitCode == 0,
                string.Join(Environment.NewLine,
                    $"{startInfo.FileName} {startInfo.Arguments} exited with {commandResult.ExitCode}.",
                    commandResult.StdOut,
                    commandResult.StdErr));

            return commandResult;
        }
    }
}
