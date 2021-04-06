using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorServerApp.Shared
{
    // A badly-behaved error boundary that tries to keep using its same ChildContent even after an error
    // This is to check we still don't retain the descendant component instances
    public class ErrorIgnorer : ErrorBoundary
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, ChildContent);
        }
    }
}
