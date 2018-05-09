// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    [Flags]
    internal enum ProjectDifference
    {
        None = 0,
        ConfigurationChanged = 1,
        WorkspaceProjectAdded = 2,
        WorkspaceProjectRemoved = 4,
        WorkspaceProjectChanged = 8,
        DocumentAdded = 16,
        DocumentRemoved = 32,
        DocumentChanged = 64,
    }
}
