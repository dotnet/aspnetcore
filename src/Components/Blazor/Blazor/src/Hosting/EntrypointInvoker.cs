// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Blazor.Hosting
{
    internal static class EntrypointInvoker
    {
        // This method returns void because currently the JS side is not listening to any result,
        // nor will it handle any exceptions. We handle all exceptions internally to this method.
        // In the future we may want Blazor.start to return something that exposes the possibly-async
        // entrypoint result to the JS caller. There's no requirement to do that today, and if we
        // do change this it will be non-breaking.
        public static void InvokeEntrypoint(string assemblyName, string[] args)
        {
            object entrypointResult;
            try
            {
                var assembly = Assembly.Load(assemblyName);
                var entrypoint = FindUnderlyingEntrypoint(assembly);
                var @params = entrypoint.GetParameters().Length == 1 ? new object[] { args ?? Array.Empty<string>() } : new object[] { };
                entrypointResult = entrypoint.Invoke(null, @params);
            }
            catch (Exception syncException)
            {
                HandleStartupException(syncException);
                return;
            }

            // If the entrypoint is async, handle async exceptions in the same way that we would
            // have handled sync ones
            if (entrypointResult is Task entrypointTask)
            {
                entrypointTask.ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        HandleStartupException(task.Exception);
                    }
                });
            }
        }

        private static MethodBase FindUnderlyingEntrypoint(Assembly assembly)
        {
            // This is the entrypoint declared in .NET metadata. In the case of async main, it's the
            // compiler-generated wrapper method. Otherwise it's the developer-defined method.
            var metadataEntrypointMethodBase = assembly.EntryPoint;

            // For "async Task Main", the C# compiler generates a method called "<Main>"
            // that is marked as the assembly entrypoint. Detect this case, and instead of
            // calling "<Whatever>", call the sibling "Whatever".
            if (metadataEntrypointMethodBase.IsSpecialName)
            {
                var origName = metadataEntrypointMethodBase.Name;
                var origNameLength = origName.Length;
                if (origNameLength > 2)
                {
                    var candidateMethodName = origName.Substring(1, origNameLength - 2);
                    var candidateMethod = metadataEntrypointMethodBase.DeclaringType.GetMethod(
                        candidateMethodName,
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        metadataEntrypointMethodBase.GetParameters().Select(p => p.ParameterType).ToArray(),
                        null);

                    if (candidateMethod != null)
                    {
                        return candidateMethod;
                    }
                }
            }

            // Either it's not async main, or for some reason we couldn't locate the underlying entrypoint,
            // so use the one from assembly metadata.
            return metadataEntrypointMethodBase;
        }

        private static void HandleStartupException(Exception exception)
        {
            // Logs to console, and causes the error UI to appear
            Console.Error.WriteLine(exception);
        }
    }
}
