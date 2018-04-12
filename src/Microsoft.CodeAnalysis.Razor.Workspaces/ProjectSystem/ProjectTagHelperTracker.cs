// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class ProjectTagHelperTracker
    {
        private const ProjectDifference Mask =
            ProjectDifference.ConfigurationChanged |
            ProjectDifference.WorkspaceProjectAdded |
            ProjectDifference.WorkspaceProjectChanged |
            ProjectDifference.WorkspaceProjectRemoved;

        private readonly object _lock = new object();
        private readonly HostWorkspaceServices _services;

        private Task<IReadOnlyList<TagHelperDescriptor>> _task;

        public ProjectTagHelperTracker(ProjectState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            _services = state.Services;
        }

        public bool IsResultAvailable => _task?.IsCompleted == true;

        public ProjectTagHelperTracker ForkFor(ProjectState state, ProjectDifference difference)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if ((difference & Mask) != 0)
            {
                return null;
            }

            return this;
        }

        public Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelperInitializationTask(ProjectSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (_task == null)
            {
                lock (_lock)
                {
                    if (_task == null)
                    {
                        _task = GetTagHelperInitializationTaskCore(snapshot);
                    }
                }
            }

            return _task;
        }

        private async Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelperInitializationTaskCore(ProjectSnapshot snapshot)
        {
            var resolver = _services.GetLanguageServices(RazorLanguage.Name).GetRequiredService<TagHelperResolver>();
            return (await resolver.GetTagHelpersAsync(snapshot)).Descriptors;
        }
    }
}
