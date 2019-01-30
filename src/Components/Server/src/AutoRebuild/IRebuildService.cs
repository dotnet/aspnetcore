// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Server.AutoRebuild
{
    /// <summary>
    /// Represents a mechanism for rebuilding a .NET project. For example, it
    /// could be a way of signalling to a VS process to perform a build.
    /// </summary>
    internal interface IRebuildService
    {
        Task<bool> PerformRebuildAsync(string projectFullPath, DateTime ifNotBuiltSince);
    }
}
