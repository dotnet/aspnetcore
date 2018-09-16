// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Razor;
using Microsoft.AspNetCore.Blazor.RenderTree;
using Microsoft.AspNetCore.Blazor.Test.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    public class GenericComponentRazorIntegrationTest : RazorIntegrationTestBase
    {
        private readonly CSharpSyntaxTree GenericContextComponent = Parse(@"
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;
namespace Test
{
    public class GenericContext<TItem> : BlazorComponent
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var items = (IReadOnlyList<TItem>)Items ?? Array.Empty<TItem>();
            for (var i = 0; i < items.Count; i++)
            {
                builder.AddContent(i, ChildContent, new Context() { Index = i, Item = items[i], });
            }
        }

        [Parameter]
        List<TItem> Items { get; set; }

        [Parameter]
        RenderFragment<Context> ChildContent { get; set; }

        public class Context
        {
            public int Index { get; set; }
            public TItem Item { get; set; }
        }
    }
}
");

        private readonly CSharpSyntaxTree MultipleGenericParameterComponent = Parse(@"
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;
namespace Test
{
    public class MultipleGenericParameter<TItem1, TItem2, TItem3> : BlazorComponent
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Item1);
            builder.AddContent(1, Item2);
            builder.AddContent(2, Item3);
        }

        [Parameter]
        TItem1 Item1 { get; set; }

        [Parameter]
        TItem2 Item2 { get; set; }

        [Parameter]
        TItem3 Item3 { get; set; }
    }
}
");

        internal override bool UseTwoPhaseCompilation => true;

        [Fact]
        public void Render_GenericComponent_WithChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(GenericContextComponent);
            
            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
<GenericContext TItem=int Items=""@(new List<int>() { 1, 2, })"">
  <div>@(context.Item * context.Index)</div>
</GenericContext>");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            var genericComponentType = component.GetType().Assembly.DefinedTypes
                .Where(t => t.Name == "GenericContext`1")
                .Single()
                .MakeGenericType(typeof(int));

            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, genericComponentType.FullName, 3, 0),
                frame => AssertFrame.Attribute(frame, "Items", typeof(List<int>), 1),
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, 2),
                frame => AssertFrame.Whitespace(frame, 3),
                frame => AssertFrame.Element(frame, "div", 2, 4),
                frame => AssertFrame.Text(frame, "0", 5),
                frame => AssertFrame.Whitespace(frame, 6),
                frame => AssertFrame.Whitespace(frame, 3),
                frame => AssertFrame.Element(frame, "div", 2, 4),
                frame => AssertFrame.Text(frame, "2", 5),
                frame => AssertFrame.Whitespace(frame, 6));
        }

        [Fact]
        public void Render_GenericComponent_MultipleParameters_WithChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(MultipleGenericParameterComponent);

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
<MultipleGenericParameter
  TItem1=""int""
  TItem2=""string""
  TItem3=long
  Item1=3
  Item2=""@(""FOO"")""
  Item3=39L/>");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            var genericComponentType = component.GetType().Assembly.DefinedTypes
                .Where(t => t.Name == "MultipleGenericParameter`3")
                .Single()
                .MakeGenericType(typeof(int), typeof(string), typeof(long));

            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, genericComponentType.FullName, 4, 0),
                frame => AssertFrame.Attribute(frame, "Item1", 3, 1),
                frame => AssertFrame.Attribute(frame, "Item2", "FOO", 2),
                frame => AssertFrame.Attribute(frame, "Item3", 39L, 3),
                frame => AssertFrame.Text(frame, "3", 0),
                frame => AssertFrame.Text(frame, "FOO", 1),
                frame => AssertFrame.Text(frame, "39", 2));
        }

        [Fact]
        public void GenericComponent_WithoutAnyTypeParameters_TriggersDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(GenericContextComponent);

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<GenericContext />");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(BlazorDiagnosticFactory.GenericComponentMissingTypeArgument.Id, diagnostic.Id);
            Assert.Equal(
                "The component 'GenericContext' is missing required type arguments. Specify the missing types using the attributes: 'TItem'.",
                diagnostic.GetMessage());
        }

        [Fact]
        public void GenericComponent_WithMissingTypeParameters_TriggersDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(MultipleGenericParameterComponent);

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MultipleGenericParameter TItem1=int />");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(BlazorDiagnosticFactory.GenericComponentMissingTypeArgument.Id, diagnostic.Id);
            Assert.Equal(
                "The component 'MultipleGenericParameter' is missing required type arguments. " +
                "Specify the missing types using the attributes: 'TItem2', 'TItem3'.",
                diagnostic.GetMessage());
        }
    }
}
