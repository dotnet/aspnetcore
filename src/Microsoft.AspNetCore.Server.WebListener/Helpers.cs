// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.WebListener
{
    internal static class Helpers
    {
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
