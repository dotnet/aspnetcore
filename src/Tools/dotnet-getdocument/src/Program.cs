// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.ApiDescription.Tool.Commands;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.ApiDescription.Tool
{
    internal class Program : ProgramBase
    {
        public Program(IConsole console) : base(console)
        {
        }

        private static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            var console = GetConsole();

            return new Program(console).Run(args, new InvokeCommand(console), throwOnUnexpectedArg: false);
        }
    }
}
