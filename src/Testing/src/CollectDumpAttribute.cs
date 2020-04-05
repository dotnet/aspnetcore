// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Testing
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
