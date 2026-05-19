// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.CommandLineUtils;

internal sealed class CommandParsingException : Exception
{
    public CommandParsingException(CommandLineApplication command, string message)
        : base(message)
    {
        Command = command;
    }

    public CommandLineApplication Command { get; }
}
