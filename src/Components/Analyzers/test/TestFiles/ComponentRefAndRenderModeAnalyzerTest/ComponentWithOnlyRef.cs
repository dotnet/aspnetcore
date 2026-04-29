// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Analyzers.Tests.TestFiles.ComponentRefAndRenderModeAnalyzerTest
{
    public class ComponentWithOnlyRef : ComponentBase
    {
        private TestComponent2 componentRef;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TestComponent2>(0);
            builder.AddComponentParameter(1, nameof(TestComponent2.Value), 42);
            builder.AddComponentReferenceCapture(2, component => componentRef = (TestComponent2)component);
            builder.CloseComponent();
        }
    }

    public class TestComponent2 : ComponentBase
    {
        [Parameter] public int Value { get; set; }
    }
}