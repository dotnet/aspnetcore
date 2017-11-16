// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    // ----------------------------------------------------------------------------------------------------
    // NOTE: This is only here for VisualStudio binary compatibility. This type should not be used; instead
    // use TagHelperResolver.
    // ----------------------------------------------------------------------------------------------------
    [Export(typeof(ITagHelperResolver))]
    internal class LegacyTagHelperResolver : DefaultTagHelperResolver, ITagHelperResolver
    {
        [ImportingConstructor]
        public LegacyTagHelperResolver(
            [Import(typeof(VisualStudioWorkspace))] Workspace workspace)
            : base(workspace.Services.GetRequiredService<ErrorReporter>(), workspace)
        {
        }
    }
}