// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    [ExportWorkspaceService(typeof(FileChangeTrackerFactory), layer: ServiceLayer.Editor)]
    internal class DefaultFileChangeTrackerFactory : FileChangeTrackerFactory
    {
        public override FileChangeTracker Create(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            return new DefaultFileChangeTracker(filePath);
        }
    }
}
