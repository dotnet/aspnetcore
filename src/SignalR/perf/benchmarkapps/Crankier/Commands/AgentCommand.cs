// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.CommandLineUtils;

using static Microsoft.AspNetCore.SignalR.Crankier.Commands.CommandLineUtilities;

namespace Microsoft.AspNetCore.SignalR.Crankier.Commands
{
    internal class AgentCommand
    {
        public static void Register(CommandLineApplication app)
        {
            app.Command("agent", cmd => cmd.OnExecute(() => Fail("Not yet implemented")));
        }
    }
}
