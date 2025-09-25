// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Analyzers.Tests.TestFiles.ComponentRefAndRenderModeAnalyzerTest
{
    public class NonComponentMethod
    {
        public void SomeMethod()
        {
            // This method doesn't use RenderTreeBuilder methods, so should not be analyzed
            var x = 1;
            var y = 2;
            var z = x + y;
        }

        public void AnotherMethod(RenderTreeBuilder builder)
        {
            // This method uses RenderTreeBuilder but is not a component BuildRenderTree method
            // It should not be analyzed since it's not in a component context
            builder.OpenElement(0, "div");
            builder.AddContent(1, "Hello World");
            builder.CloseElement();
        }
    }
}