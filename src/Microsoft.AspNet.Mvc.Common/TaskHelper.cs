// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Utility methods for dealing with <see cref="Task"/>.
    /// </summary>
    internal static class TaskHelper
    {
        /// <summary>
        /// Waits for the task to complete and throws the first faulting exception if the task is faulted. 
        /// It preserves the original stack trace when throwing the exception.
        /// </summary>
        /// <remarks>
        /// Invoking this method is equivalent to calling Wait() on the <paramref name="task" /> if it is not completed.
        /// </remarks>
        public static void WaitAndThrowIfFaulted(Task task)
        {
            task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Waits for the task to complete and throws the first faulting exception if the task is faulted. 
        /// It preserves the original stack trace when throwing the exception.
        /// </summary>
        /// <remarks>
        /// Invoking this method is equivalent to calling <see cref="Task{TResult}.Result"/> on the 
        /// <paramref name="task"/> if it is not completed.
        /// </remarks>
        public static TVal WaitAndThrowIfFaulted<TVal>(Task<TVal> task)
        {
            return task.GetAwaiter().GetResult();
        }
    }
}