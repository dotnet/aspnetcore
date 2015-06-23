// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Kestrel
{
    public class Program
    {
        private readonly IServiceProvider _serviceProvider;

        public Program(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Main(string[] args)
        {
            var program = new Microsoft.AspNet.Hosting.Program(_serviceProvider);
            var mergedArgs = new[] { "--server", "Kestrel" }.Concat(args).ToArray();
            program.Main(mergedArgs);
        }
    }
}

