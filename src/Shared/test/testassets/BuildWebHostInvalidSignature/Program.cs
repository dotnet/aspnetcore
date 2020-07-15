// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using MockHostTypes;

namespace BuildWebHostInvalidSignature
{
    public class Program
    {
        static void Main(string[] args)
        {
        }

        // Missing string[] args
        public static IWebHost BuildWebHost() => null;
    }
}
