// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.SecretManager.Tools.Internal;

namespace Microsoft.Extensions.SecretManager.Tools.Tests
{
    public class TestConsole : IConsole
    {
        public TextWriter Error { get; set; } = Console.Error;
        public TextReader In { get; set; } = Console.In;
        public TextWriter Out { get; set; } = Console.Out;
        public bool IsInputRedirected { get; set; } = false;
    }
}
