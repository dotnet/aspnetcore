// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            DebugMode.HandleDebugSwitch(ref args);

            var cancel = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) => { cancel.Cancel(); };

            // Prevent shadow copying.
            var loader = new DefaultExtensionAssemblyLoader(baseDirectory: null);
            var checker = new DefaultExtensionDependencyChecker(loader, Console.Error);

            var application = new Application(cancel.Token, loader, checker);
            return application.Execute(args);
        }
    }
}