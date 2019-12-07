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
        public static Task InvokeEntrypointAsync(IntPtr entrypointMethodHandleValue, string[] args)
        {
            var entrypointMethodBase = CreateMethodBase(entrypointMethodHandleValue);

            // For "async Task Main", the C# compiler generates a method called "<Main>"
            // that is marked as the assembly entrypoint. Detect this case, and instead of
            // calling "<Whatever>", call the sibling "Whatever".
            if (entrypointMethodBase.IsSpecialName)
            {
                var origName = entrypointMethodBase.Name;
                var origNameLength = origName.Length;
                if (origNameLength > 2)
                {
                    var candidateMethodName = origName.Substring(1, origNameLength - 2);
                    var candidateMethod = entrypointMethodBase.DeclaringType.GetMethod(
                        candidateMethodName,
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        entrypointMethodBase.GetParameters().Select(p => p.ParameterType).ToArray(),
                        null);
                    if (candidateMethod != null)
                    {
                        entrypointMethodBase = candidateMethod;
                    }
                }
            }

            // We're not handling any errors here, synchronous or asynchronous. That means they'll
            // bubble up to the caller on the JS side, which can decide what to do with them.
            return entrypointMethodBase.Invoke(null, new object[] { args }) as Task;
        }

        private static MethodBase CreateMethodBase(IntPtr methodHandleValue)
        {
            var methodHandleCtor = typeof(RuntimeMethodHandle).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(IntPtr) }, null);
            var myConstructedMethodHandle = (RuntimeMethodHandle)methodHandleCtor.Invoke(new object[] { methodHandleValue });
            return MethodBase.GetMethodFromHandle(myConstructedMethodHandle);
        }
    }
}
