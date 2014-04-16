// -----------------------------------------------------------------------
// <copyright file="Helpers.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.Net.Server
{
    internal static class Helpers
    {
        internal static Task CompletedTask()
        {
            return Task.FromResult<object>(null);
        }

        internal static ConfiguredTaskAwaitable SupressContext(this Task task)
        {
            return task.ConfigureAwait(continueOnCapturedContext: false);
        }

        internal static ConfiguredTaskAwaitable<T> SupressContext<T>(this Task<T> task)
        {
            return task.ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
