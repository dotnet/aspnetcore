// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.Kestrel.Infrastructure
{
    public static class TaskUtilities
    {
        public static Task CompletedTask = NewCompletedTask();

        private static Task NewCompletedTask()
        {
            var tcs = new TaskCompletionSource<int>();
            tcs.SetResult(0);
            return tcs.Task;
        }
    }
}
