// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [System.Composition.Shared]
    [Export(typeof(VisualStudioWorkspaceAccessor))]
    internal class DefaultVisualStudioWorkspaceAccessor : VisualStudioWorkspaceAccessor
    {
        [ImportingConstructor]
        public DefaultVisualStudioWorkspaceAccessor([Import(typeof(VisualStudioWorkspace))] Workspace workspace)
        {
            Workspace = workspace;
        }

        public override Workspace Workspace { get; }
    }
}
