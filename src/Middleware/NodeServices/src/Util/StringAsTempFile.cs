// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;

namespace Microsoft.AspNetCore.NodeServices
{
    /// <summary>
    /// Makes it easier to pass script files to Node in a way that's sure to clean up after the process exits.
    /// </summary>
    [Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public sealed class StringAsTempFile : IDisposable
    {
        private bool _disposedValue;
        private bool _hasDeletedTempFile;
        private object _fileDeletionLock = new object();
        private IDisposable _applicationLifetimeRegistration;

        /// <summary>
        /// Create a new instance of <see cref="StringAsTempFile"/>.
        /// </summary>
        /// <param name="content">The contents of the temporary file to be created.</param>
        /// <param name="applicationStoppingToken">A token that indicates when the host application is stopping.</param>
        public StringAsTempFile(string content, CancellationToken applicationStoppingToken)
        {
            FileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.WriteAllText(FileName, content);

            // Because .NET finalizers don't reliably run when the process is terminating, also
            // add event handlers for other shutdown scenarios.
            _applicationLifetimeRegistration = applicationStoppingToken.Register(EnsureTempFileDeleted);
        }

        /// <summary>
        /// Specifies the filename of the temporary file.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Disposes the instance and deletes the associated temporary file.
        /// </summary>
        public void Dispose()
        {
            DisposeImpl(true);
            GC.SuppressFinalize(this);
        }

        private void DisposeImpl(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state
                    _applicationLifetimeRegistration.Dispose();
                }

                EnsureTempFileDeleted();

                _disposedValue = true;
            }
        }

        private void EnsureTempFileDeleted()
        {
            lock (_fileDeletionLock)
            {
                if (!_hasDeletedTempFile)
                {
                    File.Delete(FileName);
                    _hasDeletedTempFile = true;
                }
            }
        }

        /// <summary>
        /// Implements the finalization part of the IDisposable pattern by calling Dispose(false).
        /// </summary>
        ~StringAsTempFile()
        {
            DisposeImpl(false);
        }
    }
}
