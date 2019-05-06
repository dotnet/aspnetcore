// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using MockHostTypes;

namespace CreateWebHostBuilderInvalidSignature
{
    public class Program
    {
        static void Main(string[] args)
        {
        }

        // Wrong return type
        public static IWebHost CreateWebHostBuilder(string[] args) => new WebHost();
    }
}
