// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Editor.Razor;
using MonoDevelop.Projects;
using Workspace = Microsoft.CodeAnalysis.Workspace;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor
{
    internal abstract class VisualStudioMacWorkspaceAccessor : VisualStudioWorkspaceAccessor
    {
        public abstract bool TryGetWorkspace(Solution solution, out Workspace workspace);
    }
}
