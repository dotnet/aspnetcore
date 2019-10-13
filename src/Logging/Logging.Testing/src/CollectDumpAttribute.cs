// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.Extensions.Logging.Testing
{
    /// <summary>
    /// Capture the memory dump upon test failure.
    /// </summary>
    /// <remarks>
    /// This currently only works in Windows environments
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CollectDumpAttribute : Attribute, ITestMethodLifecycle
    {
        public Task OnTestStartAsync(TestContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task OnTestEndAsync(TestContext context, Exception exception, CancellationToken cancellationToken)
        {
            if (exception != null)
            {
                var path = Path.Combine(context.FileOutput.TestClassOutputDirectory, context.FileOutput.GetUniqueFileName(context.FileOutput.TestName, ".dmp"));
                var process = Process.GetCurrentProcess();
                DumpCollector.Collect(process, path);
            }

            return Task.CompletedTask;
        }
    }
}
