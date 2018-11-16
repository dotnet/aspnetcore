// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            DebugMode.HandleDebugSwitch(ref args);

            var cancel = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) => { cancel.Cancel(); };

            var outputWriter = new StringWriter();
            var errorWriter = new StringWriter();

            // Prevent shadow copying.
            var loader = new DefaultExtensionAssemblyLoader(baseDirectory: null);
            var checker = new DefaultExtensionDependencyChecker(loader, outputWriter, errorWriter);

            var application = new Application(
                cancel.Token,
                loader,
                checker,
                (path, properties) => MetadataReference.CreateFromFile(path, properties),
                outputWriter,
                errorWriter);

            var result = application.Execute(args);

            var output = outputWriter.ToString();
            var error = errorWriter.ToString();

            outputWriter.Dispose();
            errorWriter.Dispose();

            Console.Write(output);
            Console.Error.Write(error);

            // This will no-op if server logging is not enabled.
            ServerLogger.Log(output);
            ServerLogger.Log(error);

            return result;
        }
    }
}