// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.NET.Sdk.BlazorWebAssembly.Tools
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            DebugMode.HandleDebugSwitch(ref args);

            var cancel = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) => { cancel.Cancel(); };

            var application = new Application(
                cancel.Token,
                Console.Out,
                Console.Error);

            application.Commands.Add(new BrotliCompressCommand(application));

            return application.Execute(args);
        }
    }
}
