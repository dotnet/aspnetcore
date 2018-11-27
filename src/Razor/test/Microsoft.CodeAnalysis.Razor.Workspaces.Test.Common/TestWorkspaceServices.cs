// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.CodeAnalysis.Host
{
    internal class TestWorkspaceServices : HostWorkspaceServices
    {
        private static readonly Workspace DefaultWorkspace = TestWorkspace.Create();

        private readonly HostServices _hostServices;
        private readonly IEnumerable<IWorkspaceService> _workspaceServices;
        private readonly TestRazorLanguageServices _razorLanguageServices;
        private readonly Workspace _workspace;

        public TestWorkspaceServices(
            HostServices hostServices,
            IEnumerable<IWorkspaceService> workspaceServices,
            IEnumerable<ILanguageService> razorLanguageServices,
            Workspace workspace)
        {
            if (hostServices == null)
            {
                throw new ArgumentNullException(nameof(hostServices));
            }

            if (workspaceServices == null)
            {
                throw new ArgumentNullException(nameof(workspaceServices));
            }

            if (razorLanguageServices == null)
            {
                throw new ArgumentNullException(nameof(razorLanguageServices));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            _hostServices = hostServices;
            _workspaceServices = workspaceServices;
            _razorLanguageServices = new TestRazorLanguageServices(this, razorLanguageServices);
            _workspace = workspace;
        }

        public override HostServices HostServices => _hostServices;

        public override Workspace Workspace => _workspace;

        public override TWorkspaceService GetService<TWorkspaceService>()
        {
            var service = _workspaceServices.OfType<TWorkspaceService>().FirstOrDefault();

            if (service == null)
            {
                // Fallback to default host services to resolve roslyn specific features.
                service = DefaultWorkspace.Services.GetService<TWorkspaceService>();
            }

            return service;
        }

        public override HostLanguageServices GetLanguageServices(string languageName)
        {
            if (languageName != RazorLanguage.Name)
            {
                throw new InvalidOperationException($"Test services do not support language service '{languageName}'. The only language services supported are '{RazorLanguage.Name}'.");
            }

            return _razorLanguageServices;
        }

        public override IEnumerable<string> SupportedLanguages => new[] { RazorLanguage.Name };

        public override bool IsSupported(string languageName) => languageName == RazorLanguage.Name;

        public override IEnumerable<TLanguageService> FindLanguageServices<TLanguageService>(MetadataFilter filter) => throw new NotImplementedException();
    }
}
