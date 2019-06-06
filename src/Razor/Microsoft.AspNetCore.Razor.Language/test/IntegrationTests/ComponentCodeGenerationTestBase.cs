// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Components;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public abstract class ComponentCodeGenerationTestBase : RazorBaselineIntegrationTestBase
    {
        internal override string FileKind => FileKinds.Component;

        internal override bool UseTwoPhaseCompilation => true;

        protected ComponentCodeGenerationTestBase()
            : base(generateBaselines: null)
        {
        }

        #region Basics

        [Fact]
        public void SingleLineControlFlowStatements_InCodeDirective()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.RenderTree;

@code {
    void RenderChildComponent(RenderTreeBuilder builder)
    {
        var output = string.Empty;
        if (builder == null) output = ""Builder is null!"";
        else output = ""Builder is not null!"";
        <p>Output: @output</p>
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void SingleLineControlFlowStatements_InCodeBlock()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.RenderTree;

@{
    var output = string.Empty;
    if (builder == null) output = ""Builder is null!"";
    else output = ""Builder is not null!"";
    <p>Output: @output</p>
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }
        [Fact]
        public void ChildComponent_InFunctionsDirective()
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
@using Microsoft.AspNetCore.Components.RenderTree;

@{ RenderChildComponent(builder); }

@code {
    void RenderChildComponent(RenderTreeBuilder builder)
    {
        <MyComponent />
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_InLocalFunction()
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
@using Microsoft.AspNetCore.Components.RenderTree;
@{
    void RenderChildComponent()
    {
        <MyComponent />
    }
}

@{ RenderChildComponent(); }
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

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
@code {
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

        [Fact]
        public void DataDashAttribute_ImplicitExpression()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@{ 
  var myValue = ""Expression value"";
}
<elem data-abc=""Literal value"" data-def=""@myValue"" />");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void DataDashAttribute_ExplicitExpression()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@{ 
  var myValue = ""Expression value"";
}
<elem data-abc=""Literal value"" data-def=""@(myValue)"" />");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void MarkupComment_IsNotIncluded()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@{ 
  var myValue = ""Expression value"";
}
<div>@myValue <!-- @myValue --> </div>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void Component_WithFullyQualifiedTagNames()
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

namespace Test2
{
    public class MyComponent2 : ComponentBase
    {
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent />
<Test.MyComponent />
<Test2.MyComponent2 />");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
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
<MyComponent @bind-Value=""ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToComponent_WithStringAttribute_DoesNotUseStringSyntax()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class InputText : ComponentBase
    {
        [Parameter]
        string Value { get; set; }

        [Parameter]
        Action<string> ValueChanged { get; set; }
    }
}"));

            AdditionalSyntaxTrees.Add(Parse(@"
using System;

namespace Test
{
    public class Person
    {
        public string Name { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
<InputText @bind-Value=""person.Name"" />

@functions 
{
    Person person = new Person();
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
<MyComponent @bind-Value=""ParentValue"" />
@code {
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
        public void BindToComponent_EventCallback_SpecifiesValue_WithMatchingProperties()
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
        EventCallback<int> ValueChanged { get; set; }
    }
}"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent @bind-Value=""ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToComponent_EventCallback_TypeChecked_WithMatchingProperties()
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
        EventCallback<int> ValueChanged { get; set; }
    }
}"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent @bind-Value=""ParentValue"" />
@code {
    public string ParentValue { get; set; } = ""42"";
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

            var assembly = CompileToAssembly(generated, throwOnFailure: false);
            // This has some errors
            Assert.Collection(
                assembly.Diagnostics.OrderBy(d => d.Id),
                d => Assert.Equal("CS1503", d.Id),
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
<MyComponent @bind-Value=""ParentValue"" />
@code {
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
<MyComponent @bind-Value=""ParentValue"" @bind-Value:event=""OnChanged"" />
@code {
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
<MyComponent @bind-Value=""ParentValue"" @bind-Value:event=""OnChanged"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToComponent_SpecifiesValueAndExpression()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        int Value { get; set; }

        [Parameter]
        Action<int> ValueChanged { get; set; }

        [Parameter]
        Expression<Func<int>> ValueExpression { get; set; }
    }
}"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent @bind-Value=""ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToComponent_EventCallback_SpecifiesValueAndExpression()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        int Value { get; set; }

        [Parameter]
        EventCallback<int> ValueChanged { get; set; }

        [Parameter]
        Expression<Func<int>> ValueExpression { get; set; }
    }
}"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent @bind-Value=""ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToComponent_SpecifiesValueAndExpression_TypeChecked()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        int Value { get; set; }

        [Parameter]
        Action<int> ValueChanged { get; set; }

        [Parameter]
        Expression<Func<string>> ValueExpression { get; set; }
    }
}"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent @bind-Value=""ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

            var assembly = CompileToAssembly(generated, throwOnFailure: false);
            // This has some errors
            Assert.Collection(
                assembly.Diagnostics.OrderBy(d => d.Id),
                d => Assert.Equal("CS0029", d.Id),
                d => Assert.Equal("CS1662", d.Id));
        }

        [Fact]
        public void BindToComponent_SpecifiesValueAndExpression_Generic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<T> : ComponentBase
    {
        [Parameter]
        T SomeParam { get; set; }

        [Parameter]
        Action<T> SomeParamChanged { get; set; }

        [Parameter]
        Expression<Func<T>> SomeParamExpression { get; set; }
    }
}"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent @bind-SomeParam=""ParentValue"" />
@code {
    public DateTime ParentValue { get; set; } = DateTime.Now;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToComponent_EventCallback_SpecifiesValueAndExpression_Generic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<T> : ComponentBase
    {
        [Parameter]
        T SomeParam { get; set; }

        [Parameter]
        EventCallback<T> SomeParamChanged { get; set; }

        [Parameter]
        Expression<Func<T>> SomeParamExpression { get; set; }
    }
}"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent @bind-SomeParam=""ParentValue"" />
@code {
    public DateTime ParentValue { get; set; } = DateTime.Now;
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
<div @bind=""@ParentValue"" />
@code {
    public string ParentValue { get; set; } = ""hi"";
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToElement_WithStringAttribute_WritesAttributes()
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
<div @bind-value=""ParentValue"" />
@code {
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
<div @bind-value=""@ParentValue"" />
@code {
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
<input @bind=""@ParentValue"" />
@code {
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
<input type=""text"" @bind=""@CurrentDate"" @bind:format=""MM/dd/yyyy""/>
@code {
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
<input type=""text"" @bind=""@CurrentDate"" @bind:format=""@Format""/>
@code {
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
<input type=""text"" @bind=""@ParentValue"" />
@code {
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
<input type=""checkbox"" @bind=""@Enabled"" />
@code {
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
<input type=""text"" @bind-value=""@ParentValue"" @bind-value:event=""onchange"" />
@code {
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
<input type=""text"" @bind-value=""@CurrentDate"" @bind-value:event=""onchange"" @bind-value:format=""MM/dd"" />
@code {
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
@page ""/MyPage""
@page ""/AnotherRoute/{id}""
<MyComponent />");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void Component_WithUsingDirectives()
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

namespace Test2
{
    public class MyComponent2 : ComponentBase
    {
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@page ""/MyPage""
@page ""/AnotherRoute/{id}""
@using Test2
<MyComponent />
<MyComponent2 />");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void Component_WithUsingDirectives_AmbiguousImport()
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

namespace Test2
{
    public class SomeComponent : ComponentBase
    {
    }
}

namespace Test3
{
    public class SomeComponent : ComponentBase
    {
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@using Test2
@using Test3
<MyComponent />
<SomeComponent />");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            var result = CompileToAssembly(generated, throwOnFailure: !DesignTime);

            if (DesignTime)
            {
                Assert.Collection(result.Diagnostics, d =>
                {
                    Assert.Equal("CS0104", d.Id);
                    Assert.Equal(CodeAnalysis.DiagnosticSeverity.Error, d.Severity);
                    Assert.Equal("'SomeComponent' is an ambiguous reference between 'Test2.SomeComponent' and 'Test3.SomeComponent'", d.GetMessage());
                });
            }
        }

        [Fact]
        public void Component_IgnoresStaticAndAliasUsings()
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

namespace Test2
{
    public class SomeComponent : ComponentBase
    {
    }
}

namespace Test3
{
    public class SomeComponent : ComponentBase
    {
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@using static Test2.SomeComponent
@using Foo = Test3
<MyComponent />
<SomeComponent /> <!-- Not a component -->");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            var result = CompileToAssembly(generated);
        }

        [Fact]
        public void ChildContent_FromAnotherNamespace()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class HeaderComponent : ComponentBase
    {
        [Parameter]
        RenderFragment Header { get; set; }
    }
}

namespace AnotherTest
{
    public class FooterComponent : ComponentBase
    {
        [Parameter]
        RenderFragment<DateTime> Footer { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@using AnotherTest

<HeaderComponent>
    <Header>Hi!</Header>
</HeaderComponent>
<FooterComponent>
    <Footer>@context</Footer>
</FooterComponent>
<Test.HeaderComponent>
    <Header>Hi!</Header>
</Test.HeaderComponent>
<AnotherTest.FooterComponent>
    <Footer>@context</Footer>
</AnotherTest.FooterComponent>
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void Component_WithNamespaceDirective()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class HeaderComponent : ComponentBase
    {
        [Parameter]
        string Header { get; set; }
    }
}

namespace AnotherTest
{
    public class FooterComponent : ComponentBase
    {
        [Parameter]
        string Footer { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@using Test
@namespace AnotherTest

<HeaderComponent Header='head'>
</HeaderComponent>
<FooterComponent Footer='feet'>
</FooterComponent>
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        #endregion

        #region EventCallback

        [Fact]
        public void EventCallback_CanPassEventCallback_Explicitly()
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
        EventCallback OnClick { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent OnClick=""@(EventCallback.Factory.Create(this, Increment))""/>

@code {
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
        public void EventCallback_CanPassEventCallbackOfT_Explicitly()
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
        EventCallback<UIMouseEventArgs> OnClick { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent OnClick=""@(EventCallback.Factory.Create<UIMouseEventArgs>(this, Increment))""/>

@code {
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
        public void EventCallback_CanPassEventCallback_Implicitly_Action()
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
        EventCallback OnClick { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent OnClick=""@Increment""/>

@code {
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
        public void EventCallback_CanPassEventCallback_Implicitly_ActionOfObject()
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
        EventCallback OnClick { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent OnClick=""@Increment""/>

@code {
    private int counter;
    private void Increment(object e) {
        counter++;
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void EventCallback_CanPassEventCallback_Implicitly_FuncOfTask()
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
        EventCallback OnClick { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent OnClick=""@Increment""/>

@code {
    private int counter;
    private Task Increment() {
        counter++;
        return Task.CompletedTask;
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void EventCallback_CanPassEventCallback_Implicitly_FuncOfobjectTask()
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
        EventCallback OnClick { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent OnClick=""@Increment""/>

@code {
    private int counter;
    private Task Increment(object e) {
        counter++;
        return Task.CompletedTask;
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void EventCallback_CanPassEventCallbackOfT_Implicitly_Action()
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
        EventCallback<UIMouseEventArgs> OnClick { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent OnClick=""@Increment""/>

@code {
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
        public void EventCallback_CanPassEventCallbackOfT_Implicitly_ActionOfT()
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
        EventCallback<UIMouseEventArgs> OnClick { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent OnClick=""@Increment""/>

@code {
    private int counter;
    private void Increment(UIMouseEventArgs e) {
        counter++;
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void EventCallback_CanPassEventCallbackOfT_Implicitly_FuncOfTask()
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
        EventCallback<UIMouseEventArgs> OnClick { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent OnClick=""@Increment""/>

@code {
    private int counter;
    private Task Increment() {
        counter++;
        return Task.CompletedTask;
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void EventCallback_CanPassEventCallbackOfT_Implicitly_FuncOfTTask()
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
        EventCallback<UIMouseEventArgs> OnClick { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent OnClick=""@Increment""/>

@code {
    private int counter;
    private Task Increment(UIMouseEventArgs e) {
        counter++;
        return Task.CompletedTask;
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void EventCallback_CanPassEventCallbackOfT_Implicitly_TypeMismatch()
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
        EventCallback<UIMouseEventArgs> OnClick { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent OnClick=""@Increment""/>

@code {
    private int counter;
    private void Increment(UIChangeEventArgs e) {
        counter++;
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

            var result = CompileToAssembly(generated, throwOnFailure: false);

            // Cannot convert from method group to Action - this isn't a great error message, but it's
            // what the compiler gives us.
            Assert.Collection(result.Diagnostics, d => { Assert.Equal("CS1503", d.Id); });
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
<MyComponent OnClick=""@(e => { Increment(); })""/>

@code {
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
<DynamicElement @onclick=""@OnClick"" />

@code {
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
<MyComponent OnClick=""@Increment""/>

@code {
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
<input @onclick=""foo"" />");

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
<input @onclick=""@(() => { })"" />");

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
<input @onclick=""@(x => { })"" />");

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
<input @onclick=""@OnClick"" />
@code {
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
<input @onclick=""@OnClick"" />
@code {
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
<input @onclick=""@OnClick"" />
@code {
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
<input @onclick=""@OnClick"" />
@code {
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
<input @onclick=""@OnClick"" />
@code {
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
<input @onclick=""@(async () => await Task.Delay(10))"" />
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
<input @onclick=""@(async (e) => await Task.Delay(10))"" />
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
<input @onclick=""@(x => { })"" />");

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
<input @onclick=""@OnClick"" />
@code {
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
<MyComponent TItem=string @bind-Item=Value/>
@code {
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
<MyComponent @bind-Item=Value/>
<MyComponent @bind-Item=Value/>
@code {
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
<MyComponent TItem=string @bind-Item=Value/>
@code {
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
<MyComponent @bind-Item=Value Value=@(18)/>
@code {
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
<MyComponent Item=""@(""hi"")"">
  <div>@context.ToLower()</div>
</MyComponent>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_NonGenericParameterizedChildContent_TypeInference()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] TItem Item { get; set; }

        [Parameter] RenderFragment<TItem> GenericFragment { get; set; }

        [Parameter] RenderFragment<int> IntFragment { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
<MyComponent Item=""@(""hi"")"">
  <GenericFragment>@context.ToLower()</GenericFragment>
  <IntFragment>@context</IntFragment>
</MyComponent>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void GenericComponent_WithFullyQualifiedTagName()
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
<Test.MyComponent Item=""@(""hi"")"">
  <div>@context.ToLower()</div>
</Test.MyComponent>");

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
        public void GenericComponent_WithKey()
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
<MyComponent TItem=int Item=""3"" @key=""_someKey"" />

@code {
    private object _someKey = new object();
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void GenericComponent_WithKey_TypeInference()
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
<MyComponent Item=""3"" @key=""_someKey"" />

@code {
    private object _someKey = new object();
}
");

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
<MyComponent TItem=int Item=""3"" @ref=""_my"" />

@code {
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
<MyComponent Item=""3"" @ref=""_my"" />

@code {
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
        public void GenericComponent_NonGenericParameter_TypeInference()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;
using Test.Shared;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] TItem Item { get; set; }
        [Parameter] MyClass Foo { get; set; }
    }
}

namespace Test.Shared
{
    public class MyClass
    {
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@using Test.Shared
<MyComponent Item=""3"" Foo=""@Hello"" />

@code {
    MyClass Hello = new MyClass();
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        #endregion

        #region Key

        [Fact]
        public void Element_WithKey()
        {
            // Arrange/Act
            var generated = CompileToCSharp(@"
<elem attributebefore=""before"" @key=""someObject"" attributeafter=""after"">Hello</elem>

@code {
    private object someObject = new object();
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void Element_WithKey_AndOtherAttributes()
        {
            // Arrange/Act
            var generated = CompileToCSharp(@"
<input type=""text"" data-slider-min=""@Min"" @key=""@someObject"" />

@code {
        private object someObject = new object();

        [Parameter] protected int Min { get; set; }
    }
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void Component_WithKey()
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
<MyComponent ParamBefore=""before"" @key=""someDate.Day"" ParamAfter=""after"" />

@code {
    private DateTime someDate = DateTime.Now;
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void Component_WithKey_WithChildContent()
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
<MyComponent @key=""123 + 456"" SomeProp=""val"">
    Some <el>further</el> content
</MyComponent>
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
<elem attributebefore=""before"" @ref=""myElem"" attributeafter=""after"">Hello</elem>

@code {
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
        public void Element_WithRef_AndOtherAttributes()
        {
            // Arrange/Act
            var generated = CompileToCSharp(@"
<input type=""text"" data-slider-min=""@Min"" @ref=""@_element"" />

@code {
        private ElementRef _element;

        [Parameter] protected int Min { get; set; }
        public void Foo() { System.GC.KeepAlive(_element); }
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
<MyComponent ParamBefore=""before"" @ref=""myInstance"" ParamAfter=""after"" />

@code {
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
<MyComponent @ref=""myInstance"" SomeProp=""val"">
    Some <el>further</el> content
</MyComponent>

@code {
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
@code {
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
@code {
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
@code {
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
@code {
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
@{
    RenderFragment<Person> p = (person) => @<div><MyComponent Name=""@person.Name""/></div>;
}
@code {
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
@{
    RenderFragment<Person> p = (person) => @<div><MyComponent Name=""@person.Name""/></div>;
}
<MyComponent>
@(""hello, world!"")
</MyComponent>

@code {
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
<h1>Hello</h1>

<SomeOtherComponent />

");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void Whitespace_BetweenElementAndFunctions()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
    <elem attr=@Foo />
    @code {
        int Foo = 18;
    }
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void WhiteSpace_InsideAttribute_InMarkupBlock()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"<div class=""first second"">Hello</div>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        #endregion

        #region Imports
        [Fact]
        public void Component_WithImportsFile()
        {
            // Arrange
            var importContent = @"
@using System.Text
@using System.Reflection
@attribute [Serializable]
";
            var importItem = CreateProjectItem("_Imports.razor", importContent, FileKinds.ComponentImport);
            ImportItems.Add(importItem);
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
<Counter />
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ComponentImports()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
namespace Test
{
    public class MainLayout : ComponentBase, ILayoutComponent
    {
        public RenderFragment Body { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp("_Imports.razor", @"
@using System.Text
@using System.Reflection

@layout MainLayout
@Foo
<div>Hello</div>
", throwOnFailure: false, fileKind: FileKinds.ComponentImport);

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated, throwOnFailure: false);
        }

        [Fact]
        public void Component_NamespaceDirective_InImports()
        {
            // Arrange
            var importContent = @"
@using System.Text
@using System.Reflection
@namespace New.Test
";
            var importItem = CreateProjectItem("_Imports.razor", importContent, FileKinds.ComponentImport);
            ImportItems.Add(importItem);
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace New.Test
{
    public class Counter : ComponentBase
    {
        public int Count { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
<Counter />
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void Component_NamespaceDirective_OverrideImports()
        {
            // Arrange
            var importContent = @"
@using System.Text
@using System.Reflection
@namespace Import.Test
";
            var importItem = CreateProjectItem("_Imports.razor", importContent, FileKinds.ComponentImport);
            ImportItems.Add(importItem);
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace New.Test
{
    public class Counter2 : ComponentBase
    {
        public int Count { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp("Pages/Counter.razor", @"
@namespace New.Test
<Counter2 />
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
        public void DuplicateMarkupAttributes_IsAnError()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<div>
  <a href=""/cool-url"" style="""" disabled href=""/even-cooler-url"">Learn the ten cool tricks your compiler author will hate!</a>
</div>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.DuplicateMarkupAttribute.Id, diagnostic.Id);
        }

        [Fact]
        public void DuplicateMarkupAttributes_IsAnError_EventHandler()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<div>
  <a @onclick=""test()"" onclick=""@(() => {})"">Learn the ten cool tricks your compiler author will hate!</a>
</div>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.DuplicateMarkupAttributeDirective.Id, diagnostic.Id);
        }

        [Fact]
        public void DuplicateMarkupAttributes_Multiple_IsAnError()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<div>
  <a href=""/cool-url"" style="""" disabled href=""/even-cooler-url"" href>Learn the ten cool tricks your compiler author will hate!</a>
</div>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

            Assert.All(generated.Diagnostics, d =>
            {
                Assert.Same(ComponentDiagnosticFactory.DuplicateMarkupAttribute.Id, d.Id);
            });
        }

        [Fact]
        public void DuplicateMarkupAttributes_IsAnError_BindValue()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<div>
  <input type=""text"" value=""17"" @bind=""@text""></input>
</div>
@functions {
    private string text = ""hi"";
}
");


            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.DuplicateMarkupAttributeDirective.Id, diagnostic.Id);
        }

        [Fact]
        public void DuplicateMarkupAttributes_IsAnError_BindOnInput()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<div>
  <input type=""text"" @bind-value=""@text"" @bind-value:event=""oninput"" @oninput=""@(() => {})""></input>
</div>
@functions {
    private string text = ""hi"";
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.DuplicateMarkupAttributeDirective.Id, diagnostic.Id);
        }

        [Fact]
        public void DuplicateComponentParameters_IsAnError()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public string Message { get; private set; }
    }
}
"));
            // Act
            var generated = CompileToCSharp(@"
<MyComponent Message=""test"" mESSAGE=""test"" />
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.DuplicateComponentParameter.Id, diagnostic.Id);
        }

        [Fact]
        public void DuplicateComponentParameters_IsAnError_Multiple()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public string Message { get; private set; }
    }
}
"));
            // Act
            var generated = CompileToCSharp(@"
<MyComponent Message=""test"" mESSAGE=""test"" Message=""anotherone"" />
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

            Assert.All(generated.Diagnostics, d =>
            {
                Assert.Same(ComponentDiagnosticFactory.DuplicateComponentParameter.Id, d.Id);
            });
        }

        [Fact]
        public void DuplicateComponentParameters_IsAnError_WeaklyTyped()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public string Message { get; private set; }
    }
}
"));
            // Act
            var generated = CompileToCSharp(@"
<MyComponent Foo=""test"" foo=""test"" />
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.DuplicateComponentParameter.Id, diagnostic.Id);
        }

        [Fact]
        public void DuplicateComponentParameters_IsAnError_BindMessage()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public string Message { get; private set; }
        [Parameter] public EventCallback<string> MessageChanged { get; private set; }
        [Parameter] public Expression<Action<string>> MessageExpression { get; private set; }
    }
}
"));
            // Act
            var generated = CompileToCSharp(@"
<MyComponent Message=""@message"" @bind-Message=""@message"" />
@functions {
    string message = ""hi"";
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.DuplicateComponentParameterDirective.Id, diagnostic.Id);
        }

        [Fact]
        public void DuplicateComponentParameters_IsAnError_BindMessageChanged()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public string Message { get; private set; }
        [Parameter] public EventCallback<string> MessageChanged { get; private set; }
        [Parameter] public Expression<Action<string>> MessageExpression { get; private set; }
    }
}
"));
            // Act
            var generated = CompileToCSharp(@"
<MyComponent MessageChanged=""@((s) => {})"" @bind-Message=""@message"" />
@functions {
    string message = ""hi"";
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.DuplicateComponentParameterDirective.Id, diagnostic.Id);
        }

        [Fact]
        public void DuplicateComponentParameters_IsAnError_BindMessageExpression()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public string Message { get; private set; }
        [Parameter] public EventCallback<string> MessageChanged { get; private set; }
        [Parameter] public Expression<Action<string>> MessageExpression { get; private set; }
    }
}
"));
            // Act
            var generated = CompileToCSharp(@"
<MyComponent @bind-Message=""@message"" MessageExpression=""@((s) => {})"" />
@functions {
    string message = ""hi"";
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.DuplicateComponentParameterDirective.Id, diagnostic.Id);
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
<Counter @bind-v=""y"" />
@code {
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
<User @bind-Name=""@UserName"" @bind-IsActive=""@UserIsActive"" />

@code {
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
<p @onmouseover=""@OnComponentHover"" style=""background: @ParentBgColor;"" />
@code {
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
<input @onfocus='alert(""Test"");' />
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void Component_TextTagsAreNotRendered()
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
<Counter />
@if (true)
{
    <text>This text is rendered</text>
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        #endregion
    }
}
