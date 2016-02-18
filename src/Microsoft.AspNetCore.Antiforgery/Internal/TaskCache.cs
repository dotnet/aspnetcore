// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Antiforgery.Internal
{
    public static class TaskCache
    {
        public static readonly Task CompletedTask = Task.FromResult(0);
    }
}
