using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorServerApp.Shared
{
    // A badly-behaved error boundary that tries to keep using its same ChildContent even after an error
    // This is to check we still don't retain the descendant component instances
    public class ErrorIgnorer : ErrorBoundaryBase
    {
        protected override ValueTask LogExceptionAsync(Exception exception)
        {
            Console.WriteLine($"There was an error, but we'll try to ignore it. La la la la, can't year you. [{exception}]");
            return ValueTask.CompletedTask;
        }

        protected override void RenderDefaultErrorContent(RenderTreeBuilder builder, Exception exception)
        {
            ChildContent!(builder);
        }
    }
}
