// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.ApiDescription.Tool.Commands
{
    internal class HelpCommandBase : CommandBase
    {
        public HelpCommandBase(IConsole console) : base(console)
        {
        }

        public override void Configure(CommandLineApplication command)
        {
            command.HelpOption();
            command.VersionOptionFromAssemblyAttributes();
            base.Configure(command);
        }
    }
}
