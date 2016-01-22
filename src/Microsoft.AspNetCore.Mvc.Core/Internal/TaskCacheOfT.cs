// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public static class TaskCache<T>
    {
        private static readonly Task<T> _defaultCompletedTask = Task.FromResult(default(T));

        /// <summary>
        /// Gets a completed <see cref="Task"/> with the value of <c>default(T)</c>.
        /// </summary>
        public static Task<T> DefaultCompletedTask => _defaultCompletedTask;
    }

}
