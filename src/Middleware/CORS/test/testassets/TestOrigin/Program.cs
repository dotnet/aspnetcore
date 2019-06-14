// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace SampleOrigin
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using (var host = WebHost.CreateDefaultBuilder(args).UseUrls("http://+:0").UseStartup<Startup>().Build())
            {
                host.Run();
            }
        }
    }
}
