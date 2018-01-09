// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    public static class TestWorkspace
    {
        private static readonly object WorkspaceLock = new object();

        public static Workspace Create(Action<AdhocWorkspace> configure = null)
        {
            lock (WorkspaceLock)
            {
                var workspace = new AdhocWorkspace();
                configure?.Invoke(workspace);

                return workspace;
            }
        }
    }
}
