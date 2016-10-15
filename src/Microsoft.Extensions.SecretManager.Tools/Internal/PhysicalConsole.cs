// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Extensions.SecretManager.Tools.Internal
{
    public class PhysicalConsole : IConsole
    {
        private PhysicalConsole() { }

        public static IConsole Singleton { get; } = new PhysicalConsole();
        public TextWriter Error => Console.Error;
        public TextReader In => Console.In;
        public TextWriter Out => Console.Out;
        public bool IsInputRedirected => Console.IsInputRedirected;
    }
}
