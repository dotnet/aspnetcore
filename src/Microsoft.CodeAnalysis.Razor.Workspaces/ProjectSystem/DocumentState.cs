// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class DocumentState
    {
        private readonly object _lock;

        private DocumentGeneratedOutputTracker _generatedOutput;

        public DocumentState(HostWorkspaceServices services, HostDocument hostDocument)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (hostDocument == null)
            {
                throw new ArgumentNullException(nameof(hostDocument));
            }

            Services = services;
            HostDocument = hostDocument;
            Version = VersionStamp.Create();

            _lock = new object();
        }

        public DocumentState(DocumentState previous, ProjectDifference difference)
        {
            if (previous == null)
            {
                throw new ArgumentNullException(nameof(previous));
            }

            Services = previous.Services;
            HostDocument = previous.HostDocument;
            Version = previous.Version.GetNewerVersion();

            _generatedOutput = previous._generatedOutput?.ForkFor(this, difference);
        }

        public HostDocument HostDocument { get; }

        public HostWorkspaceServices Services { get; }

        public VersionStamp Version { get; }

        public DocumentGeneratedOutputTracker GeneratedOutput
        {
            get
            {
                if (_generatedOutput == null)
                {
                    lock (_lock)
                    {
                        if (_generatedOutput == null)
                        {
                            _generatedOutput = new DocumentGeneratedOutputTracker(null);
                        }
                    }
                }

                return _generatedOutput;
            }
        }
    }
}
