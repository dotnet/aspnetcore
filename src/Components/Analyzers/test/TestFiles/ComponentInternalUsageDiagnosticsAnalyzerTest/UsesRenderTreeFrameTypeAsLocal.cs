using System;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Analyzers.Tests.TestFiles.ComponentInternalUsageDiagnosticsAnalyzerTest
{
    class UsesRenderTreeFrameTypeAsLocal
    {
        private void Test()
        {
            var test = RenderTreeFrameType./*MM*/Attribute;
            GC.KeepAlive(test);
        }

    }
}
