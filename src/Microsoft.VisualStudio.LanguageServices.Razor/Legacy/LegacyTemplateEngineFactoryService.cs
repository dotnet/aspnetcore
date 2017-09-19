// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Inner = Microsoft.CodeAnalysis.Razor.RazorTemplateEngineFactoryService;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    // ----------------------------------------------------------------------------------------------------
    // NOTE: This is only here for VisualStudio binary compatibility. This type should not be used; instead
    // use DefaultTemplateEngineFactoryService.
    // ----------------------------------------------------------------------------------------------------
    [Export(typeof(RazorTemplateEngineFactoryService))]
    internal class LegacyTemplateEngineFactoryService : RazorTemplateEngineFactoryService
    {
        private readonly Inner _inner;
        private readonly Workspace _workspace;

        [ImportingConstructor]
        public LegacyTemplateEngineFactoryService([Import(typeof(VisualStudioWorkspace))] Workspace workspace)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            _workspace = workspace;
            _inner = workspace.Services.GetLanguageServices(RazorLanguage.Name).GetRequiredService<Inner>();
        }

        // internal for testing
        internal LegacyTemplateEngineFactoryService(Workspace workspace, Inner inner)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            _workspace = workspace;
            _inner = inner;
        }

        public override RazorTemplateEngine Create(string projectPath, Action<IRazorEngineBuilder> configure)
        {
            if (projectPath == null)
            {
                throw new ArgumentNullException(nameof(projectPath));
            }

            return _inner.Create(projectPath, configure);
        }
    }
}
