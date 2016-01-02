// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.Kestrel.Infrastructure
{
    public static class TaskUtilities
    {
#if DOTNET5_4
        public static Task CompletedTask = Task.CompletedTask;
#else
        public static Task CompletedTask = Task.FromResult<object>(null);
#endif
    }
}