// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Analyzers.Tests.TestFiles.ComponentInternalUsageDiagnosticsAnalyzerTest
{
    class UsersRendererTypesInMethodBody
    {
        private void Test()
        {
            var test = /*MMField*/RenderTreeFrameType.Attribute;
            GC.KeepAlive(test);

            var frame = /*MMNewObject*/new RenderTreeFrame();
            GC.KeepAlive(/*MMProperty*/frame.Component);

            var range = /*MMNewObject2*/new ArrayRange<string>(null, 0);
            /*MMInvocation*/range.Clone();
        }
    }
}
