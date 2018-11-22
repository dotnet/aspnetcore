// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Components.Build.Test
{
    public abstract class CodeGenerationTestBase : RazorBaselineIntegrationTestBase
    {
        internal override bool UseTwoPhaseCompilation => true;

        public CodeGenerationTestBase()
            : base(generateBaselines: false)
        {
        }

        #region Basics

        [Fact]
        public void ChildComponent_Simple()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
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
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class SomeType
    {
    }

    public class MyComponent : ComponentBase
    {
        [Parameter] int IntProperty { get; set; }
        [Parameter] bool BoolProperty { get; set; }
        [Parameter] string StringProperty { get; set; }
        [Parameter] SomeType ObjectProperty { get; set; }
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
        public void ComponentWithTypeParameters()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;
@typeparam TItem1
@typeparam TItem2

<h1>Item1</h1>
@foreach (var item2 in Items2)
{
    <p>
    @ChildContent(item2);
    </p>
}
@functions {
    [Parameter] TItem1 Item1 { get; set; }
    [Parameter] List<TItem2> Items2 { get; set; }
    [Parameter] RenderFragment<TItem2> ChildContent { get; set; }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_WithExplicitStringParameter()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        string StringProperty { get; set; }
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
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
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
        public void ComponentParameter_TypeMismatch_ReportsDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class CoolnessMeter : ComponentBase
    {
        [Parameter] private int Coolness { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<CoolnessMeter Coolness=""@(""very-cool"")"" />
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

            var assembly = CompileToAssembly(generated, throwOnFailure: false);
            // This has some errors
            Assert.Collection(
                assembly.Diagnostics.OrderBy(d => d.Id),
                d => Assert.Equal("CS1503", d.Id));
        }

        #endregion

        #region Bind

        [Fact]
        public void BindToComponent_SpecifiesValue_WithMatchingProperties()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        int Value { get; set; }

        [Parameter]
        Action<int> ValueChanged { get; set; }
    }
}"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent bind-Value=""ParentValue"" />
@functions {
    public int ParentValue { get; set; } = 42;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToComponent_TypeChecked_WithMatchingProperties()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        int Value { get; set; }

        [Parameter]
        Action<int> ValueChanged { get; set; }
    }
}"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent bind-Value=""ParentValue"" />
@functions {
    public string ParentValue { get; set; } = ""42"";
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

            var assembly = CompileToAssembly(generated, throwOnFailure: false);
            // This has some errors
            Assert.Collection(
                assembly.Diagnostics.OrderBy(d => d.Id),
                d => Assert.Equal("CS0029", d.Id),
                d => Assert.Equal("CS1503", d.Id));
        }

        [Fact]
        public void BindToComponent_SpecifiesValue_WithoutMatchingProperties()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase, IComponent
    {
        void IComponent.SetParameters(ParameterCollection parameters)
        {
        }
    }
}"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent bind-Value=""ParentValue"" />
@functions {
    public int ParentValue { get; set; } = 42;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToComponent_SpecifiesValueAndChangeEvent_WithMatchingProperties()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        int Value { get; set; }

        [Parameter]
        Action<int> OnChanged { get; set; }
    }
}"));
            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent bind-Value-OnChanged=""ParentValue"" />
@functions {
    public int ParentValue { get; set; } = 42;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToComponent_SpecifiesValueAndChangeEvent_WithoutMatchingProperties()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase, IComponent
    {
        void IComponent.SetParameters(ParameterCollection parameters)
        {
        }
    }
}"));

            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent bind-Value-OnChanged=""ParentValue"" />
@functions {
    public int ParentValue { get; set; } = 42;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToElement_WritesAttributes()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", null, ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<div bind=""@ParentValue"" />
@functions {
    public string ParentValue { get; set; } = ""hi"";
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToElementWithSuffix_WritesAttributes()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", ""value"", ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));
            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<div bind-value=""@ParentValue"" />
@functions {
    public string ParentValue { get; set; } = ""hi"";
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BuiltIn_BindToInputWithoutType_WritesAttributes()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<input bind=""@ParentValue"" />
@functions {
    public int ParentValue { get; set; } = 42;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BuiltIn_BindToInputText_WithFormat_WritesAttributes()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<input type=""text"" bind=""@CurrentDate"" format-value=""MM/dd/yyyy""/>
@functions {
    public DateTime CurrentDate { get; set; } = new DateTime(2018, 1, 1);
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BuiltIn_BindToInputText_WithFormatFromProperty_WritesAttributes()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<input type=""text"" bind=""@CurrentDate"" format-value=""@Format""/>
@functions {
    public DateTime CurrentDate { get; set; } = new DateTime(2018, 1, 1);

    public string Format { get; set; } = ""MM/dd/yyyy"";
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BuiltIn_BindToInputText_WritesAttributes()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<input type=""text"" bind=""@ParentValue"" />
@functions {
    public int ParentValue { get; set; } = 42;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BuiltIn_BindToInputCheckbox_WritesAttributes()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<input type=""checkbox"" bind=""@Enabled"" />
@functions {
    public bool Enabled { get; set; }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToElementFallback_WritesAttributes()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<input type=""text"" bind-value-onchange=""@ParentValue"" />
@functions {
    public int ParentValue { get; set; } = 42;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToElementFallback_WithFormat_WritesAttributes()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<input type=""text"" bind-value-onchange=""@CurrentDate"" format-value=""MM/dd"" />
@functions {
    public DateTime CurrentDate { get; set; } = new DateTime(2018, 1, 1);
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        #endregion

        #region Child Content

        [Fact]
        public void ChildComponent_WithChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        string MyAttr { get; set; }

        [Parameter]
        RenderFragment ChildContent { get; set; }
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
        public void ChildComponent_WithGenericChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        string MyAttr { get; set; }

        [Parameter]
        RenderFragment<string> ChildContent { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent MyAttr=""abc"">Some text<some-child a='1'>@context.ToLowerInvariant()</some-child></MyComponent>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }


        [Fact]
        public void ChildComponent_WithGenericChildContent_SetsParameterName()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        string MyAttr { get; set; }

        [Parameter]
        RenderFragment<string> ChildContent { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent MyAttr=""abc"">
  <ChildContent Context=""item"">
    Some text<some-child a='1'>@item.ToLowerInvariant()</some-child>
  </ChildContent>
</MyComponent>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_WithGenericChildContent_SetsParameterNameOnComponent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        string MyAttr { get; set; }

        [Parameter]
        RenderFragment<string> ChildContent { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent MyAttr=""abc"" Context=""item"">
  <ChildContent>
    Some text<some-child a='1'>@item.ToLowerInvariant()</some-child>
  </ChildContent>
</MyComponent>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_WithElementOnlyChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        RenderFragment ChildContent { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent><child>hello</child></MyComponent>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_WithExplicitChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        RenderFragment ChildContent { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent><ChildContent>hello</ChildContent></MyComponent>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_WithExplicitGenericChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        RenderFragment<string> ChildContent { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent><ChildContent>@context</ChildContent></MyComponent>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void MultipleExplictChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        RenderFragment Header { get; set; }

        [Parameter]
        RenderFragment Footer { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent>
    <Header>Hi!</Header>
    <Footer>@(""bye!"")</Footer>
</MyComponent>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BodyAndAttributeChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        RenderFragment<string> Header { get; set; }

        RenderFragment ChildContent { get; set; }

        [Parameter]
        RenderFragment Footer { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
@{ RenderFragment<string> header = (context) => @<div>@context.ToLowerInvariant()</div>; }
<MyComponent Header=@header>
    Some Content
</MyComponent>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BodyAndExplicitChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        RenderFragment<string> Header { get; set; }

        [Parameter]
        RenderFragment ChildContent { get; set; }

        [Parameter]
        RenderFragment Footer { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
@{ RenderFragment<string> header = (context) => @<div>@context.ToLowerInvariant()</div>; }
<MyComponent Header=@header>
  <ChildContent>Some Content</ChildContent>
  <Footer>Bye!</Footer>
</MyComponent>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        #endregion

        #region Directives

        [Fact]
        public void ChildComponent_WithPageDirective()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
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

        #endregion

        #region Event Handlers

        [Fact]
        public void ChildComponent_WithLambdaEventHandler()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        Action<UIEventArgs> OnClick { get; set; }
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

        // Regression test for #954 - we need to allow arbitrary event handler
        // attributes with weak typing.
        [Fact]
        public void ChildComponent_WithWeaklyTypeEventHandler()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class DynamicElement : ComponentBase
    {
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<DynamicElement onclick=""@OnClick"" />

@functions {
    private Action<UIMouseEventArgs> OnClick { get; set; }
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
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        Action<UIEventArgs> OnClick { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
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
        public void EventHandler_OnElement_WithString()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<input onclick=""foo"" />");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void EventHandler_OnElement_WithNoArgsLambdaDelegate()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<input onclick=""@(() => { })"" />");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void EventHandler_OnElement_WithEventArgsLambdaDelegate()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<input onclick=""@(x => { })"" />");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void EventHandler_OnElement_WithNoArgMethodGroup()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<input onclick=""@OnClick"" />
@functions {
    void OnClick() {
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void EventHandler_OnElement_WithEventArgsMethodGroup()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<input onclick=""@OnClick"" />
@functions {
    void OnClick(UIMouseEventArgs e) {
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void EventHandler_OnElement_ArbitraryEventName_WithEventArgsMethodGroup()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<input onclick=""@OnClick"" />
@functions {
    void OnClick(UIEventArgs e) {
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void AsyncEventHandler_OnElement_Action_MethodGroup()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@using System.Threading.Tasks
<input onclick=""@OnClick"" />
@functions {
    Task OnClick() 
    {
        return Task.CompletedTask;
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void AsyncEventHandler_OnElement_ActionEventArgs_MethodGroup()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@using System.Threading.Tasks
<input onclick=""@OnClick"" />
@functions {
    Task OnClick(UIMouseEventArgs e) 
    {
        return Task.CompletedTask;
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void AsyncEventHandler_OnElement_Action_Lambda()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@using System.Threading.Tasks
<input onclick=""async (e) => await Task.Delay(10)"" />
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void AsyncEventHandler_OnElement_ActionEventArgs_Lambda()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@using System.Threading.Tasks
<input onclick=""async (e) => await Task.Delay(10)"" />
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void EventHandler_OnElement_WithLambdaDelegate()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<input onclick=""@(x => { })"" />");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void EventHandler_OnElement_WithDelegate()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<input onclick=""@OnClick"" />
@functions {
    void OnClick(UIMouseEventArgs e) {
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        #endregion

        #region Generics

        [Fact]
        public void ChildComponent_Generic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] TItem Item { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent TItem=string Item=""@(""hi"")""/>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_Generic_TypeInference()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] TItem Item { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent Item=""@(""hi"")""/>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_Generic_TypeInference_Multiple()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] TItem Item { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent Item=""@(""hi"")""/>
<MyComponent Item=""@(""how are you?"")""/>
<MyComponent Item=""@(""bye!"")""/>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_GenericWeaklyTypedAttribute()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] TItem Item { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent TItem=string Item=""@(""hi"")"" Other=""@(17)""/>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_GenericWeaklyTypedAttribute_TypeInference()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] TItem Item { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent Item=""@(""hi"")"" Other=""@(17)""/>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_GenericBind()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter]
        TItem Item { get; set; }

        [Parameter]
        Action<TItem> ItemChanged { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent TItem=string bind-Item=Value/>
@functions {
    string Value;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_GenericBind_TypeInference()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter]
        TItem Item { get; set; }

        [Parameter]
        Action<TItem> ItemChanged { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent bind-Item=Value/>
@functions {
    string Value;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_GenericBindWeaklyTyped()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent TItem=string bind-Item=Value/>
@functions {
    string Value;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_GenericBindWeaklyTyped_TypeInference()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] TItem Value { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent bind-Item=Value Value=@(18)/>
@functions {
    string Value;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_GenericChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] TItem Item { get; set; }

        [Parameter] RenderFragment<TItem> ChildContent { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent TItem=string Item=""@(""hi"")"">
  <div>@context.ToLower()</div>
</MyComponent>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_GenericChildContent_TypeInference()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] TItem Item { get; set; }

        [Parameter] RenderFragment<TItem> ChildContent { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent Item=""@(""hi"")"">
  <div>@context.ToLower()</div>
</MyComponent>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_MultipleGenerics()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem1, TItem2> : ComponentBase
    {
        [Parameter] TItem1 Item { get; set; }

        [Parameter] RenderFragment<TItem1> ChildContent { get; set; }

        [Parameter] RenderFragment<Context> AnotherChildContent { get; set; }

        public class Context
        {
            public TItem2 Item { get; set; }
        }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent TItem1=string TItem2=int Item=""@(""hi"")"">
  <ChildContent><div>@context.ToLower()</div></ChildContent>
<AnotherChildContent Context=""item"">
  @System.Math.Max(0, item.Item);
</AnotherChildContent>
</MyComponent>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_MultipleGenerics_TypeInference()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem1, TItem2> : ComponentBase
    {
        [Parameter] TItem1 Item { get; set; }

        [Parameter] List<TItem2> Items { get; set; }

        [Parameter] RenderFragment<TItem1> ChildContent { get; set; }

        [Parameter] RenderFragment<Context> AnotherChildContent { get; set; }

        public class Context
        {
            public TItem2 Item { get; set; }
        }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent Item=""@(""hi"")"" Items=@(new List<long>())>
  <ChildContent><div>@context.ToLower()</div></ChildContent>
<AnotherChildContent Context=""item"">
  @System.Math.Max(0, item.Item);
</AnotherChildContent>
</MyComponent>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void GenericComponent_WithComponentRef()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] TItem Item { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent TItem=int Item=""3"" ref=""_my"" />

@functions {
    private MyComponent<int> _my;
    public void Foo() { System.GC.KeepAlive(_my); }
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void GenericComponent_WithComponentRef_TypeInference()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] TItem Item { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent Item=""3"" ref=""_my"" />

@functions {
    private MyComponent<int> _my;
    public void Foo() { System.GC.KeepAlive(_my); }
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        #endregion

        #region Ref

        [Fact]
        public void Element_WithRef()
        {
            // Arrange/Act
            var generated = CompileToCSharp(@"
<elem attributebefore=""before"" ref=""myElem"" attributeafter=""after"">Hello</elem>

@functions {
    private Microsoft.AspNetCore.Components.ElementRef myElem;
    public void Foo() { System.GC.KeepAlive(myElem); }
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void Component_WithRef()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

            // Arrange/Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent ParamBefore=""before"" ref=""myInstance"" ParamAfter=""after"" />

@functions {
    private Test.MyComponent myInstance;
    public void Foo() { System.GC.KeepAlive(myInstance); }
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void Component_WithRef_WithChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

            // Arrange/Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent ref=""myInstance"" SomeProp=""val"">
    Some <el>further</el> content
</MyComponent>

@functions {
    private Test.MyComponent myInstance;
    public void Foo() { System.GC.KeepAlive(myInstance); }
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        #endregion

        #region Templates

        [Fact]
        public void RazorTemplate_InCodeBlock()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@{
    RenderFragment<Person> p = (person) => @<div>@person.Name</div>;
}
@functions {
    class Person
    {
        public string Name { get; set; }
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void RazorTemplate_InExplicitExpression()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@(RenderPerson((person) => @<div>@person.Name</div>))
@functions {
    class Person
    {
        public string Name { get; set; }
    }

    object RenderPerson(RenderFragment<Person> p) => null;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void RazorTemplate_NonGeneric_InImplicitExpression()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@RenderPerson(@<div>HI</div>)
@functions {
    object RenderPerson(RenderFragment p) => null;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void RazorTemplate_Generic_InImplicitExpression()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@RenderPerson((person) => @<div>@person.Name</div>)
@functions {
    class Person
    {
        public string Name { get; set; }
    }

    object RenderPerson(RenderFragment<Person> p) => null;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void RazorTemplate_ContainsComponent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] string Name { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper ""*, TestAssembly""
@{
    RenderFragment<Person> p = (person) => @<div><MyComponent Name=""@person.Name""/></div>;
}
@functions {
    class Person
    {
        public string Name { get; set; }
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        // Targeted at the logic that assigns 'builder' names
        [Fact]
        public void RazorTemplate_FollowedByComponent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] string Name { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper ""*, TestAssembly""
@{
    RenderFragment<Person> p = (person) => @<div><MyComponent Name=""@person.Name""/></div>;
}
<MyComponent>
@(""hello, world!"")
</MyComponent>

@functions {
    class Person
    {
        public string Name { get; set; }
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void RazorTemplate_NonGeneric_AsComponentParameter()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] RenderFragment Template { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper ""*, TestAssembly""
@{ RenderFragment template = @<div>Joey</div>; }
<MyComponent Person=""@template""/>
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void RazorTemplate_Generic_AsComponentParameter()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] RenderFragment<Person> PersonTemplate { get; set; }
    }

    public class Person
    {
        public string Name { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper ""*, TestAssembly""
@{ RenderFragment<Person> template = (person) => @<div>@person.Name</div>; }
<MyComponent PersonTemplate=""@template""/>
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void RazorTemplate_AsComponentParameter_MixedContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] RenderFragment<Context> Template { get; set; }
    }

    public class Context
    {
        public int Index { get; set; }
        public string Item { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper ""*, TestAssembly""
@{ RenderFragment<Test.Context> template = (context) => @<li>#@context.Index - @context.Item.ToLower()</li>; }
<MyComponent Template=""@template""/>
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        #endregion

        #region Whitespace

        [Fact]
        public void LeadingWhiteSpace_WithDirective()
        {
            // Arrange/Act
            var generated = CompileToCSharp(@"

@using System

<h1>Hello</h1>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void LeadingWhiteSpace_WithCSharpExpression()
        {
            // Arrange/Act
            var generated = CompileToCSharp(@"
   
@(""My value"")

<h1>Hello</h1>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void LeadingWhiteSpace_WithComponent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class SomeOtherComponent : ComponentBase
    {
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
   
<SomeOtherComponent />

<h1>Hello</h1>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void TrailingWhiteSpace_WithDirective()
        {
            // Arrange/Act
            var generated = CompileToCSharp(@"
<h1>Hello</h1>

@page ""/my/url""

");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void TrailingWhiteSpace_WithCSharpExpression()
        {
            // Arrange/Act
            var generated = CompileToCSharp(@"
<h1>Hello</h1>

@(""My value"")

");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void TrailingWhiteSpace_WithComponent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class SomeOtherComponent : ComponentBase
    {
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<h1>Hello</h1>

<SomeOtherComponent />

");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        #endregion

        #region Misc

        [Fact] // We don't process <!DOCTYPE ...> - we just skip them
        public void Component_WithDocType()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<!DOCTYPE html>
<div>
</div>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ScriptTag_WithErrorSuppressed()
        {
            // Arrange/Act
            var generated = CompileToCSharp(@"
<div>
    <script src='some/url.js' anotherattribute suppress-error='BL9992'>
        some text
        some more text
    </script>
</div>
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact] // https://github.com/aspnet/Blazor/issues/597
        public void Regression_597()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class Counter : ComponentBase
    {
        public int Count { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<Counter bind-v=""y"" />
@functions {
    string y = null;
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void Regression_609()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class User : ComponentBase
    {
        public string Name { get; set; }
        public Action<string> NameChanged { get; set; }
        public bool IsActive { get; set; }
        public Action<bool> IsActiveChanged { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<User bind-Name=""@UserName"" bind-IsActive=""@UserIsActive"" />

@functions {
    public string UserName { get; set; }
    public bool UserIsActive { get; set; }
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact] // https://github.com/aspnet/Blazor/issues/772
        public void Regression_772()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class SurveyPrompt : ComponentBase
    {
        [Parameter] private string Title { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
@page ""/""

<h1>Hello, world!</h1>

Welcome to your new app.

<SurveyPrompt Title=""
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

            // This has some errors
            Assert.Collection(
                generated.Diagnostics.OrderBy(d => d.Id),
                d => Assert.Equal("RZ1034", d.Id),
                d => Assert.Equal("RZ1035", d.Id));
        }

        [Fact] // https://github.com/aspnet/Blazor/issues/773
        public void Regression_773()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class SurveyPrompt : ComponentBase
    {
        [Parameter] private string Title { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
@page ""/""

<h1>Hello, world!</h1>

Welcome to your new app.

<SurveyPrompt Title=""<div>Test!</div>"" />
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void Regression_784()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<p onmouseover=""@OnComponentHover"" style=""background: @ParentBgColor;"" />
@functions {
    public string ParentBgColor { get; set; } = ""#FFFFFF"";

    public void OnComponentHover(UIMouseEventArgs e)
    {
    }
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void EventHandlerTagHelper_EscapeQuotes()
        {
            // Act
            var generated = CompileToCSharp(@"
<input onfocus='alert(""Test"");' />
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        #endregion
    }
}
