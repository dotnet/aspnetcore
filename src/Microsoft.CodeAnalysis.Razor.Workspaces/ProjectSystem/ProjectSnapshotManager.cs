// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal abstract class ProjectSnapshotManager : ILanguageService
    {
        public abstract event EventHandler<ProjectChangeEventArgs> Changed;

        public abstract IReadOnlyList<ProjectSnapshot> Projects { get; }

        public abstract ProjectSnapshot GetLoadedProject(string filePath);

        public abstract ProjectSnapshot GetOrCreateProject(string filePath);
    }
}
