// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    public class GenericComponentRazorIntegrationTest : RazorIntegrationTestBase
    {
        private readonly CSharpSyntaxTree GenericContextComponent = Parse(@"
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
namespace Test
{
    public class GenericContext<TItem> : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var items = (IReadOnlyList<TItem>)Items ?? Array.Empty<TItem>();
            for (var i = 0; i < items.Count; i++)
            {
                if (ChildContent == null)
                {
                    builder.AddContent(i, Items[i]);
                }
                else
                {
                    builder.AddContent(i, ChildContent, new Context() { Index = i, Item = items[i], });
                }
            }
        }

        [Parameter]
        public List<TItem> Items { get; set; }

        [Parameter]
        public RenderFragment<Context> ChildContent { get; set; }

        public class Context
        {
            public int Index { get; set; }
            public TItem Item { get; set; }
        }
    }
}
");

        private readonly CSharpSyntaxTree MultipleGenericParameterComponent = Parse(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
namespace Test
{
    public class MultipleGenericParameter<TItem1, TItem2, TItem3> : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Item1);
            builder.AddContent(1, Item2);
            builder.AddContent(2, Item3);
        }

        [Parameter]
        public TItem1 Item1 { get; set; }

        [Parameter]
        public TItem2 Item2 { get; set; }

        [Parameter]
        public TItem3 Item3 { get; set; }
    }
}
");

        public GenericComponentRazorIntegrationTest(ITestOutputHelper output)
            : base(output)
        {
        }

        internal override bool UseTwoPhaseCompilation => true;

        [Fact]
        public void Render_GenericComponent_WithoutChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(GenericContextComponent);

            var component = CompileToComponent(@"
<GenericContext TItem=int Items=""@(new List<int>() { 1, 2, })"" />");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            var genericComponentType = component.GetType().Assembly.DefinedTypes
                .Where(t => t.Name == "GenericContext`1")
                .Single()
                .MakeGenericType(typeof(int));

            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, genericComponentType.FullName, 2, 0),
                frame => AssertFrame.Attribute(frame, "Items", typeof(List<int>), 1),
                frame => AssertFrame.Text(frame, "1", 0),
                frame => AssertFrame.Text(frame, "2", 1));
        }

        [Fact]
        public void Render_GenericComponent_WithRef()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(GenericContextComponent);

            var component = CompileToComponent(@"
<GenericContext TItem=int Items=""@(new List<int>() { 1, 2, })"" @ref=""_my"" />

@code {
    GenericContext<int> _my;
    void Foo() { GC.KeepAlive(_my); }
}");

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
                frame => AssertFrame.ComponentReferenceCapture(frame, 2),
                frame => AssertFrame.Text(frame, "1", 0),
                frame => AssertFrame.Text(frame, "2", 1));
        }

        [Fact]
        public void Render_GenericComponent_WithChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(GenericContextComponent);

            var component = CompileToComponent(@"
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
                frame => AssertFrame.Attribute(frame, "ChildContent", 2),
                frame => AssertFrame.MarkupWhitespace(frame, 3),
                frame => AssertFrame.Element(frame, "div", 2, 4),
                frame => AssertFrame.Text(frame, "0", 5),
                frame => AssertFrame.MarkupWhitespace(frame, 6),
                frame => AssertFrame.MarkupWhitespace(frame, 3),
                frame => AssertFrame.Element(frame, "div", 2, 4),
                frame => AssertFrame.Text(frame, "2", 5),
                frame => AssertFrame.MarkupWhitespace(frame, 6));
        }

        [Fact]
        public void Render_GenericComponent_TypeInference_WithRef()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(GenericContextComponent);

            var component = CompileToComponent(@"
<GenericContext Items=""@(new List<int>() { 1, 2, })"" @ref=""_my"" />

@code {
    GenericContext<int> _my;
    void Foo() { GC.KeepAlive(_my); }
}");

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
                frame => AssertFrame.ComponentReferenceCapture(frame, 2),
                frame => AssertFrame.Text(frame, "1", 0),
                frame => AssertFrame.Text(frame, "2", 1));
        }

        [Fact]
        public void Render_GenericComponent_TypeInference_WithRef_Recursive()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(GenericContextComponent);

            var assembly = CompileToAssembly("Test.cshtml", @"
@typeparam TItem
<GenericContext Items=""@MyItems"" @ref=""_my"" />

@code {
    [Parameter] public List<TItem> MyItems { get; set; }
    GenericContext<TItem> _my;
    void Foo() { GC.KeepAlive(_my); }
}");

            var componentType = assembly.Assembly.DefinedTypes
                .Where(t => t.Name == "Test`1")
                .Single()
                .MakeGenericType(typeof(int));
            var component = (IComponent)Activator.CreateInstance(componentType);

            // Act
            var frames = GetRenderTree(component);

            // Assert
            var genericComponentType = assembly.Assembly.DefinedTypes
                .Where(t => t.Name == "GenericContext`1")
                .Single()
                .MakeGenericType(typeof(int));

            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, genericComponentType.FullName, 3, 0),
                frame => AssertFrame.Attribute(frame, "Items", 1),
                frame => AssertFrame.ComponentReferenceCapture(frame, 2));
        }

        [Fact]
        public void Render_GenericComponent_TypeInference_WithoutChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(GenericContextComponent);

            var component = CompileToComponent(@"
<GenericContext Items=""@(new List<int>() { 1, 2, })"" />");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            var genericComponentType = component.GetType().Assembly.DefinedTypes
                .Where(t => t.Name == "GenericContext`1")
                .Single()
                .MakeGenericType(typeof(int));

            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, genericComponentType.FullName, 2, 0),
                frame => AssertFrame.Attribute(frame, "Items", typeof(List<int>), 1),
                frame => AssertFrame.Text(frame, "1", 0),
                frame => AssertFrame.Text(frame, "2", 1));
        }

        [Fact]
        public void Render_GenericComponent_MultipleParameters_WithChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(MultipleGenericParameterComponent);

            var component = CompileToComponent(@"
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
    }
}
