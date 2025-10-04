// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Analyzers.Tests.TestFiles.ComponentRefAndRenderModeAnalyzerTest
{
    public class ComponentWithBothRefAndRenderMode : ComponentBase
    {
        private TestComponent1 componentRef;
        private readonly InteractiveServerRenderMode1 renderMode = new InteractiveServerRenderMode1();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            /*MM1*/builder.OpenComponent<TestComponent1>(0);
            builder.AddComponentParameter(1, nameof(TestComponent1.Value), 42);
            builder.AddComponentReferenceCapture(2, component => componentRef = (TestComponent1)component);
            builder.AddComponentRenderMode(renderMode);
            builder.CloseComponent();
        }
    }

    public class TestComponent1 : ComponentBase
    {
        [Parameter] public int Value { get; set; }
    }

    public class InteractiveServerRenderMode1 : IComponentRenderMode
    {
    }
}