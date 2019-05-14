// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal static class TaskCache
    {
        public static readonly Task<bool> True = Task.FromResult(true);
        public static readonly Task<bool> False = Task.FromResult(false);
    }
}