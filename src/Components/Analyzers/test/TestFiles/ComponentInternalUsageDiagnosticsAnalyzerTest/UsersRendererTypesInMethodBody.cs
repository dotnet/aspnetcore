// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
