// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Analyzers.Tests.TestFiles.ComponentRefAndRenderModeAnalyzerTest
{
    public class ComponentWithOnlyRenderMode : ComponentBase
    {
        private readonly InteractiveServerRenderMode3 renderMode = new InteractiveServerRenderMode3();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TestComponent3>(0);
            builder.AddComponentParameter(1, nameof(TestComponent3.Value), 42);
            builder.AddComponentRenderMode(renderMode);
            builder.CloseComponent();
        }
    }

    public class TestComponent3 : ComponentBase
    {
        [Parameter] public int Value { get; set; }
    }

    public class InteractiveServerRenderMode3 : IComponentRenderMode
    {
    }
}