// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal sealed class DebuggerWrapper : IDebugger
    {
        private DebuggerWrapper()
        { }

        public static IDebugger Singleton { get; } = new DebuggerWrapper();

        public bool IsAttached => Debugger.IsAttached;
    }
}
