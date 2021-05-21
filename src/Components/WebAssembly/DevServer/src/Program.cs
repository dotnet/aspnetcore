// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using DevServerProgram = Microsoft.AspNetCore.Components.WebAssembly.DevServer.Server.Program;


namespace Microsoft.AspNetCore.Components.WebAssembly.DevServer
{
    internal class Program
    {
        static int Main(string[] args)
        {
            DevServerProgram.BuildWebHost(args).Run();
            return 0;
        }
    }
}
