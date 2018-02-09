// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis
{
    public static class TestWorkspace
    {
        private static readonly object WorkspaceLock = new object();

        public static Workspace Create(Action<AdhocWorkspace> configure = null) => Create(services: null, configure: configure);

        public static Workspace Create(HostServices services, Action<AdhocWorkspace> configure = null)
        {
            lock (WorkspaceLock)
            {
                var workspace = services == null ? new AdhocWorkspace() : new AdhocWorkspace(services);
                configure?.Invoke(workspace);

                return workspace;
            }
        }
    }
}
