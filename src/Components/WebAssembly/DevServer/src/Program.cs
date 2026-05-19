// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using DevServerProgram = Microsoft.AspNetCore.Components.WebAssembly.DevServer.Server.Program;

namespace Microsoft.AspNetCore.Components.WebAssembly.DevServer;

internal sealed class Program
{
    static int Main(string[] args)
    {
        DevServerProgram.BuildWebHost(args).Run();
        return 0;
    }
}
