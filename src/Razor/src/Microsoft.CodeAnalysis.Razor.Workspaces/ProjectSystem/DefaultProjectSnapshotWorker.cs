// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class DefaultProjectSnapshotWorker : ProjectSnapshotWorker
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly TagHelperResolver _tagHelperResolver;

        public DefaultProjectSnapshotWorker(ForegroundDispatcher foregroundDispatcher, TagHelperResolver tagHelperResolver)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (tagHelperResolver == null)
            {
                throw new ArgumentNullException(nameof(tagHelperResolver));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _tagHelperResolver = tagHelperResolver;
        }

        public override Task ProcessUpdateAsync(ProjectSnapshotUpdateContext update, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (update == null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            // Don't block the main thread
            if (_foregroundDispatcher.IsForegroundThread)
            {
                return Task.Factory.StartNew(ProjectUpdatesCoreAsync, update, cancellationToken, TaskCreationOptions.None, _foregroundDispatcher.BackgroundScheduler);
            }

            return ProjectUpdatesCoreAsync(update);
        }

        protected virtual void OnProcessingUpdate()
        {
        }

        private async Task ProjectUpdatesCoreAsync(object state)
        {
            var update = (ProjectSnapshotUpdateContext)state;

            OnProcessingUpdate();

            var snapshot = new DefaultProjectSnapshot(update.HostProject, update.WorkspaceProject, update.Version);
            var result = await _tagHelperResolver.GetTagHelpersAsync(snapshot, CancellationToken.None);
            update.TagHelpers = result.Descriptors;
        }
    }
}
