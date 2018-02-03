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

        public DefaultProjectSnapshotWorker(ForegroundDispatcher foregroundDispatcher)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            _foregroundDispatcher = foregroundDispatcher;
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

        private Task ProjectUpdatesCoreAsync(object state)
        {
            OnProcessingUpdate();

            return Task.CompletedTask;
        }
    }
}
