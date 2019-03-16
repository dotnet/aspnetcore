// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using MockHostTypes;

namespace CreateHostBuilderInvalidSignature
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var webHost = CreateHostBuilder(null, args).Build();
        }

        // Extra parameter
        private static IHostBuilder CreateHostBuilder(object extraParam, string[] args) => null;
    }
}
