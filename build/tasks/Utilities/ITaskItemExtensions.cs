// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Build.Framework;

namespace RepoTasks.Utilities
{
    public static class ITaskItemExtensions
    {
        public static string GetRecursiveDir(this ITaskItem item)
            => item.GetMetadata("RecursiveDir");
        public static string GetFileName(this ITaskItem item)
            => item.GetMetadata("Filename");
        public static string GetExtension(this ITaskItem item)
            => item.GetMetadata("Extension");
    }
}
