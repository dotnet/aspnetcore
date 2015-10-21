// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace SampleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var mergedArgs = new[] { "--server", "Microsoft.AspNet.Server.Kestrel" }.Concat(args).ToArray();
            Microsoft.AspNet.Hosting.Program.Main(mergedArgs);
        }
    }
}
