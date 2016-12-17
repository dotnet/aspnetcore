// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

// TODO remove this file and use sources packages https://github.com/aspnet/Common/issues/180

namespace Microsoft.Extensions.Internal
{
    internal static class TaskCache
    {
        /// <summary>
        /// A <see cref="Task"/> that's already completed successfully.
        /// </summary>
        /// <remarks>
        /// We're caching this in a static readonly field to make it more inlinable and avoid the volatile lookup done
        /// by <c>Task.CompletedTask</c>.
        /// </remarks>
#if NET451
        public static readonly Task CompletedTask = Task.FromResult(0);
#else
        public static readonly Task CompletedTask = Task.CompletedTask;
#endif
    }
}