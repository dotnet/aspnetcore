// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace CreateDefaultBuilderOfTApp
{
    public class Program
    {
        static void Main(string[] args) => WebHost.CreateDefaultBuilder<Startup>(new[] { "--cliKey", "cliValue" }) .Build().Run();
    }
}