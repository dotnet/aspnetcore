using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Analyzers.Tests.TestFiles.ComponentInternalUsageDiagnosticsAnalyzerTest
{
    /*MM*/class UsesRendererAsBaseClass : Renderer
    {
        public UsesRendererAsBaseClass()
            : base(null, null)
        {
        }

        public override Dispatcher Dispatcher => throw new NotImplementedException();

        protected override void HandleException(Exception exception)
        {
            throw new NotImplementedException();
        }

        protected override Task UpdateDisplayAsync(/*M1*/in RenderBatch renderBatch)
        {
            throw new NotImplementedException();
        }
    }
}
