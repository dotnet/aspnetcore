// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor
{
    public abstract class WorkspaceTestBase
    {
        private bool _initialized;
        private HostServices _hostServices;
        private Workspace _workspace;

        protected WorkspaceTestBase()
        {
        }

        protected HostServices HostServices
        {
            get
            {
                EnsureInitialized();
                return _hostServices;
            }
        }

        protected Workspace Workspace
        {
            get
            {
                EnsureInitialized();
                return _workspace;
            }
        }
        
        protected virtual void ConfigureWorkspaceServices(List<IWorkspaceService> services)
        {
        }

        protected virtual void ConfigureLanguageServices(List<ILanguageService> services)
        {
        }

        protected virtual void ConfigureWorkspace(AdhocWorkspace workspace)
        {
        }

        protected virtual void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
        {
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            var workspaceServices = new List<IWorkspaceService>()
            {
                new TestProjectSnapshotProjectEngineFactory()
                {
                    Configure = ConfigureProjectEngine,
                },
            };
            ConfigureWorkspaceServices(workspaceServices);

            var languageServices = new List<ILanguageService>();
            ConfigureLanguageServices(languageServices);

            _hostServices = TestServices.Create(workspaceServices, languageServices);
            _workspace = TestWorkspace.Create(_hostServices, ConfigureWorkspace);
            _initialized = true;
        }
    }
}
