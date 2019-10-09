// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing;

namespace System.Threading.Tasks
{
    public static class TaskTimeoutExtensions
    {
        public static Task<T> DefaultTimeout<T>(this ValueTask<T> task)
        {
            return task.AsTask().TimeoutAfter(TestConstants.DefaultTimeout);
        }

        public static Task DefaultTimeout(this ValueTask task)
        {
            return task.AsTask().TimeoutAfter(TestConstants.DefaultTimeout);
        }

        public static Task<T> DefaultTimeout<T>(this Task<T> task)
        {
            return task.TimeoutAfter(TestConstants.DefaultTimeout);
        }

        public static Task DefaultTimeout(this Task task)
        {
            return task.TimeoutAfter(TestConstants.DefaultTimeout);
        }
    }
}
