// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Extensions.Tools.Internal
{
    public class PhysicalConsole : IConsole
    {
        private PhysicalConsole()
        {
            Console.CancelKeyPress += (o, e) =>
            {
                CancelKeyPress?.Invoke(o, e);
            };
        }

        public static IConsole Singleton { get; } = new PhysicalConsole();

        public event ConsoleCancelEventHandler CancelKeyPress;
        public TextWriter Error => Console.Error;
        public TextReader In => Console.In;
        public TextWriter Out => Console.Out;
        public bool IsInputRedirected => Console.IsInputRedirected;
        public bool IsOutputRedirected => Console.IsOutputRedirected;
        public bool IsErrorRedirected => Console.IsErrorRedirected;
        public ConsoleColor ForegroundColor
        {
            get => Console.ForegroundColor;
            set => Console.ForegroundColor = value;
        }

        public void ResetColor() => Console.ResetColor();
    }
}
