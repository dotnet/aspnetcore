// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.TestHost
{
    internal static class Utilities
    {
        internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

        internal static Task<T> WithTimeout<T>(this Task<T> task) => task.TimeoutAfter(DefaultTimeout);

        internal static Task WithTimeout(this Task task) => task.TimeoutAfter(DefaultTimeout);
    }
}
