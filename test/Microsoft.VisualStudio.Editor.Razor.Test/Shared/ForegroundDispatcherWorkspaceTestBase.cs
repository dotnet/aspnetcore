// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor;

namespace Xunit
{
    public abstract class ForegroundDispatcherWorkspaceTestBase : WorkspaceTestBase
    {
        internal ForegroundDispatcher Dispatcher { get; } = new SingleThreadedForegroundDispatcher();
    }
}
