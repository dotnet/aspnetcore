// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Analyzers.Tests.TestFiles.ComponentRefAndRenderModeAnalyzerTest
{
    public class MultipleComponentsWithMixedUsage : ComponentBase
    {
        private TestComponent4 componentRef1;
        private TestComponent4 componentRef2;
        private readonly InteractiveServerRenderMode4 renderMode = new InteractiveServerRenderMode4();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            // Component with only ref - should not report diagnostic
            builder.OpenComponent<TestComponent4>(0);
            builder.AddComponentParameter(1, nameof(TestComponent4.Value), 42);
            builder.AddComponentReferenceCapture(2, component => componentRef1 = (TestComponent4)component);
            builder.CloseComponent();

            // Component with both ref and rendermode - should report diagnostic
            /*MM1*/builder.OpenComponent<TestComponent4>(3);
            builder.AddComponentParameter(4, nameof(TestComponent4.Value), 100);
            builder.AddComponentReferenceCapture(5, component => componentRef2 = (TestComponent4)component);
            builder.AddComponentRenderMode(renderMode);
            builder.CloseComponent();

            // Component with only rendermode - should not report diagnostic
            builder.OpenComponent<TestComponent4>(6);
            builder.AddComponentParameter(7, nameof(TestComponent4.Value), 200);
            builder.AddComponentRenderMode(renderMode);
            builder.CloseComponent();
        }
    }

    public class TestComponent4 : ComponentBase
    {
        [Parameter] public int Value { get; set; }
    }

    public class InteractiveServerRenderMode4 : IComponentRenderMode
    {
    }
}