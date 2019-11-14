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
        }

    }
}
