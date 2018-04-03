// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    public class RuntimeCodeGenerationTest : RazorBaselineIntegrationTestBase
    {
        internal override bool UseTwoPhaseCompilation => true;

        [Fact]
        public void ChildComponent_Simple()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(CSharpSyntaxTree.ParseText(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent />");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_WithParameters()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(CSharpSyntaxTree.ParseText(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class SomeType
    {
    }

    public class MyComponent : BlazorComponent
    {
        public int IntProperty { get; set; }
        public bool BoolProperty { get; set; }
        public string StringProperty { get; set; }
        public SomeType ObjectProperty { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent 
    IntProperty=""123""
    BoolProperty=""true""
    StringProperty=""My string""
    ObjectProperty=""new SomeType()""/>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_WithExplicitStringParameter()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(CSharpSyntaxTree.ParseText(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        public string StringProperty { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent StringProperty=""@(42.ToString())"" />");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_WithNonPropertyAttributes()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(CSharpSyntaxTree.ParseText(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent some-attribute=""foo"" another-attribute=""@(43.ToString())""/>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }


        [Fact]
        public void ChildComponent_WithLambdaEventHandler()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(CSharpSyntaxTree.ParseText(@"
using System;
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        public UIEventHandler OnClick { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent OnClick=""@(e => { Increment(); })""/>

@functions {
    private int counter;
    private void Increment() {
        counter++;
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_WithExplicitEventHandler()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(CSharpSyntaxTree.ParseText(@"
using System;
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        public UIEventHandler OnClick { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
@using Microsoft.AspNetCore.Blazor
<MyComponent OnClick=""@Increment""/>

@functions {
    private int counter;
    private void Increment(UIEventArgs e) {
        counter++;
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_WithChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(CSharpSyntaxTree.ParseText(@"
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        public string MyAttr { get; set; }

        public RenderFragment ChildContent { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent MyAttr=""abc"">Some text<some-child a='1'>Nested text</some-child></MyComponent>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_WithPageDirective()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(CSharpSyntaxTree.ParseText(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
@page ""/MyPage""
@page ""/AnotherRoute/{id}""
<MyComponent />");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }
    }
}
