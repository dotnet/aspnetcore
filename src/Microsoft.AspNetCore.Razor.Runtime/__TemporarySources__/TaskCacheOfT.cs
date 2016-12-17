// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

// TODO remove this file and use sources packages https://github.com/aspnet/Common/issues/180

namespace Microsoft.Extensions.Internal
{
    internal static class TaskCache<T>
    {
        /// <summary>
        /// Gets a completed <see cref="Task"/> with the value of <c>default(T)</c>.
        /// </summary>
        public static Task<T> DefaultCompletedTask { get; }  = Task.FromResult(default(T));
    }

}