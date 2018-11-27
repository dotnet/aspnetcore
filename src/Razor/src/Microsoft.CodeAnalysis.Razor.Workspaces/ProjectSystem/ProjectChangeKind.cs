// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal enum ProjectChangeKind
    {
        ProjectAdded,
        ProjectRemoved,
        ProjectChanged,
        DocumentAdded,
        DocumentRemoved,

        // This could be a state change (opened/closed) or a content change.
        DocumentChanged,
    }
}
