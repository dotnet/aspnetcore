// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public static class TaskCache
    {
#if DNX451
        static readonly Task _completedTask = Task.FromResult(0);
#endif

        /// <summary>Gets a task that's already been completed successfully.</summary>
        /// <remarks>May not always return the same instance.</remarks>        
        public static Task CompletedTask
        {
            get
            {
#if DNX451
                return _completedTask;
#else
                return Task.CompletedTask;
#endif
            }
        }
    }

}
