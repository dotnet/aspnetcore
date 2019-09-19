using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Analyzers.Tests.TestFiles.ComponentInternalUsageDiagnosticsAnalyzerTest
{
    class UsesRenderTreeFrameAsParameter
    {
        private void Test(/*MM*/RenderTreeFrame frame)
        {
        }
    }
}
