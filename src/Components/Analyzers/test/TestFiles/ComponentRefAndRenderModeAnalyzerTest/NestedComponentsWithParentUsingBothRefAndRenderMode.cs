// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Analyzers.Tests.TestFiles.ComponentRefAndRenderModeAnalyzerTest
{
    public class NestedComponentsWithParentUsingBothRefAndRenderMode : ComponentBase
    {
        private TestComponent5 componentRef;
        private readonly InteractiveServerRenderMode5 renderMode = new InteractiveServerRenderMode5();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            // Parent component with both ref and rendermode - should report diagnostic
            /*MM1*/builder.OpenComponent<TestComponent5>(0);
            builder.AddComponentParameter(1, nameof(TestComponent5.Value), 42);
            builder.AddComponentReferenceCapture(2, component => componentRef = (TestComponent5)component);
            builder.AddComponentRenderMode(renderMode);
            
            // Nested child component - should not interfere with parent analysis
            builder.OpenComponent<TestComponent5>(3);
            builder.AddComponentParameter(4, nameof(TestComponent5.Value), 100);
            builder.CloseComponent();
            
            builder.CloseComponent(); // Close parent component
        }
    }

    public class TestComponent5 : ComponentBase
    {
        [Parameter] public int Value { get; set; }
    }

    public class InteractiveServerRenderMode5 : IComponentRenderMode
    {
    }
}
