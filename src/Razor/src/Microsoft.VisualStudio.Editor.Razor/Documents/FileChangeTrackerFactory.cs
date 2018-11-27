// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    internal abstract class FileChangeTrackerFactory : IWorkspaceService
    {
        public abstract FileChangeTracker Create(string filePath);
    }
}
