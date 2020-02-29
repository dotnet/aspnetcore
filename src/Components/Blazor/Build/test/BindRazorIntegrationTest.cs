// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    public class BindRazorIntegrationTest : RazorIntegrationTestBase
    {
        public BindRazorIntegrationTest(ITestOutputHelper output)
            : base(output)
        {
        }

        internal override bool UseTwoPhaseCompilation => true;

        [Fact]
        public void Render_BindToComponent_SpecifiesValue_WithMatchingProperties()
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
        public int Value { get; set; }

        [Parameter]
        public Action<int> ValueChanged { get; set; }
    }
}"));

            var component = CompileToComponent(@"
<MyComponent @bind-Value=""ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 3, 0),
                frame => AssertFrame.Attribute(frame, "Value", 42, 1),
                frame => AssertFrame.Attribute(frame, "ValueChanged", typeof(Action<int>), 2));
        }

        [Fact]
        public void Render_BindToComponent_SpecifiesValue_WithoutMatchingProperties()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase, IComponent
    {
        Task IComponent.SetParametersAsync(ParameterView parameters)
        {
            return Task.CompletedTask;
        }
    }
}"));

            var component = CompileToComponent(@"
<MyComponent @bind-Value=""ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 3, 0),
                frame => AssertFrame.Attribute(frame, "Value", 42, 1),
                frame => AssertFrame.Attribute(frame, "ValueChanged", typeof(EventCallback<int>), 2));
        }

        [Fact]
        public void Render_BindToComponent_SpecifiesValueAndChangeEvent_WithMatchingProperties()
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
        public int Value { get; set; }

        [Parameter]
        public Action<int> OnChanged { get; set; }
    }
}"));

            var component = CompileToComponent(@"
<MyComponent @bind-Value=""ParentValue"" @bind-Value:event=""OnChanged"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 3, 0),
                frame => AssertFrame.Attribute(frame, "Value", 42, 1),
                frame => AssertFrame.Attribute(frame, "OnChanged", typeof(Action<int>), 2));
        }

        [Fact]
        public void Render_BindToComponent_SpecifiesValueAndChangeEvent_WithoutMatchingProperties()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase, IComponent
    {
        Task IComponent.SetParametersAsync(ParameterView parameters)
        {
            return Task.CompletedTask;
        }
    }
}"));

            var component = CompileToComponent(@"
<MyComponent @bind-Value=""ParentValue"" @bind-Value:event=""OnChanged"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 3, 0),
                frame => AssertFrame.Attribute(frame, "Value", 42, 1),
                frame => AssertFrame.Attribute(frame, "OnChanged", typeof(EventCallback<int>), 2));
        }

        [Fact]
        public void Render_BindToElement_WritesAttributes()
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

            var component = CompileToComponent(@"
<div @bind=""@ParentValue"" />
@code {
    public string ParentValue { get; set; } = ""hi"";
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "div", 3, 0),
                frame => AssertFrame.Attribute(frame, "myvalue", "hi", 1),
                frame => AssertFrame.Attribute(frame, "myevent", typeof(EventCallback), 2));
        }

        [Fact]
        public void Render_BindToElementWithSuffix_WritesAttributes()
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

            var component = CompileToComponent(@"
<div @bind-value=""@ParentValue"" />
@code {
    public string ParentValue { get; set; } = ""hi"";
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "div", 3, 0),
                frame => AssertFrame.Attribute(frame, "myvalue", "hi", 1),
                frame => AssertFrame.Attribute(frame, "myevent", typeof(EventCallback), 2));
        }

        [Fact]
        public void Render_BindDuplicates_ReportsDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", ""value"", ""myvalue2"", ""myevent2"")]
    [BindElement(""div"", ""value"", ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));

            // Act
            var result = CompileToCSharp(@"
<div @bind-value=""@ParentValue"" />
@code {
    public string ParentValue { get; set; } = ""hi"";
}");

            // Assert
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal("RZ9989", diagnostic.Id);
            Assert.Equal(
                "The attribute '@bind-value' was matched by multiple bind attributes. Duplicates:" + Environment.NewLine +
                "Test.BindAttributes" + Environment.NewLine +
                "Test.BindAttributes",
                diagnostic.GetMessage());
        }

        [Fact]
        public void Render_BuiltIn_BindToInputWithoutType_WritesAttributes()
        {
            // Arrange
            var component = CompileToComponent(@"
@using Microsoft.AspNetCore.Components.Web
<input @bind=""@ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "input", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", "42", 1),
                frame => AssertFrame.Attribute(frame, "onchange", typeof(EventCallback), 2));
        }

        [Fact]
        public void Render_BuiltIn_BindToInputText_WithFormat_WritesAttributes()
        {
            // Arrange
            var component = CompileToComponent(@"
@using Microsoft.AspNetCore.Components.Web
<input type=""text"" @bind=""@CurrentDate"" @bind:format=""MM/dd/yyyy""/>
@code {
    public DateTime CurrentDate { get; set; } = new DateTime(2018, 1, 1);
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "input", 4, 0),
                frame => AssertFrame.Attribute(frame, "type", "text", 1),
                frame => AssertFrame.Attribute(frame, "value", new DateTime(2018, 1, 1).ToString("MM/dd/yyyy"), 2),
                frame => AssertFrame.Attribute(frame, "onchange", typeof(EventCallback), 3));
        }

        [Fact]
        public void Render_BuiltIn_BindToInputText_WithFormatFromProperty_WritesAttributes()
        {
            // Arrange
            var component = CompileToComponent(@"
@using Microsoft.AspNetCore.Components.Web
<input type=""text"" @bind=""@CurrentDate"" @bind:format=""@Format""/>
@code {
    public DateTime CurrentDate { get; set; } = new DateTime(2018, 1, 1);

    public string Format { get; set; } = ""MM/dd/yyyy"";
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "input", 4, 0),
                frame => AssertFrame.Attribute(frame, "type", "text", 1),
                frame => AssertFrame.Attribute(frame, "value", new DateTime(2018, 1, 1).ToString("MM/dd/yyyy"), 2),
                frame => AssertFrame.Attribute(frame, "onchange", typeof(EventCallback), 3));
        }

        [Fact]
        public void Render_BuiltIn_BindToInputText_WritesAttributes()
        {
            // Arrange
            var component = CompileToComponent(@"
@using Microsoft.AspNetCore.Components.Web
<input type=""text"" @bind=""@ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "input", 4, 0),
                frame => AssertFrame.Attribute(frame, "type", "text", 1),
                frame => AssertFrame.Attribute(frame, "value", "42", 2),
                frame => AssertFrame.Attribute(frame, "onchange", typeof(EventCallback), 3));
        }

        [Fact]
        public void Render_BuiltIn_BindToInputCheckbox_WritesAttributes()
        {
            // Arrange
            var component = CompileToComponent(@"
@using Microsoft.AspNetCore.Components.Web
<input type=""checkbox"" @bind=""@Enabled"" />
@code {
    public bool Enabled { get; set; }
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "input", 3, 0),
                frame => AssertFrame.Attribute(frame, "type", "checkbox", 1),
                frame => AssertFrame.Attribute(frame, "onchange", typeof(EventCallback), 3));
        }

        [Fact]
        public void Render_BindToElementFallback_WritesAttributes()
        {
            // Arrange
            var component = CompileToComponent(@"
<input type=""text"" @bind-value=""@ParentValue"" @bind-value:event=""onchange"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "input", 4, 0),
                frame => AssertFrame.Attribute(frame, "type", "text", 1),
                frame => AssertFrame.Attribute(frame, "value", "42", 2),
                frame => AssertFrame.Attribute(frame, "onchange", typeof(EventCallback), 3));
        }

        [Fact]
        public void Render_BindToElementFallback_WithFormat_WritesAttributes()
        {
            // Arrange
            var component = CompileToComponent(@"
<input type=""text"" @bind-value=""@CurrentDate"" @bind-value:event=""onchange"" @bind-value:format=""MM/dd"" />
@code {
    public DateTime CurrentDate { get; set; } = new DateTime(2018, 1, 1);
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "input", 4, 0),
                frame => AssertFrame.Attribute(frame, "type", "text", 1),
                frame => AssertFrame.Attribute(frame, "value", new DateTime(2018, 1, 1).ToString("MM/dd"), 2),
                frame => AssertFrame.Attribute(frame, "onchange", typeof(EventCallback), 3));
        }

        [Fact] // Additional coverage of OrphanTagHelperLoweringPass
        public void Render_BindToElementFallback_SpecifiesValueAndChangeEvent_WithCSharpAttribute()
        {
            // Arrange
            var component = CompileToComponent(@"
<input type=""@(""text"")"" @bind-value=""@ParentValue"" @bind-value:event=""onchange"" visible />
@code {
    public int ParentValue { get; set; } = 42;
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "input", 5, 0),
                frame => AssertFrame.Attribute(frame, "type", "text", 1),
                frame => AssertFrame.Attribute(frame, "visible", 2),
                frame => AssertFrame.Attribute(frame, "value", "42", 3),
                frame => AssertFrame.Attribute(frame, "onchange", typeof(EventCallback), 4));
        }

        [Fact] // See https://github.com/dotnet/blazor/issues/703
        public void Workaround_703()
        {
            // Arrange
            var component = CompileToComponent(@"
<input @bind-value=""@ParentValue"" @bind-value:event=""onchange"" type=""text"" visible />
@code {
    public int ParentValue { get; set; } = 42;
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            //
            // The workaround for 703 is that the value attribute MUST be after the type
            // attribute.
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "input", 5, 0),
                frame => AssertFrame.Attribute(frame, "type", "text", 1),
                frame => AssertFrame.Attribute(frame, "visible", 2),
                frame => AssertFrame.Attribute(frame, "value", "42", 3),
                frame => AssertFrame.Attribute(frame, "onchange", typeof(EventCallback), 4));
        }

        [Fact] // Additional coverage of OrphanTagHelperLoweringPass
        public void Render_BindToElementFallback_SpecifiesValueAndChangeEvent_BodyContent()
        {
            // Arrange
            var component = CompileToComponent(@"
<div @bind-value=""@ParentValue"" @bind-value:event=""onchange"">
  <span>@(42.ToString())</span>
</div>
@code {
    public int ParentValue { get; set; } = 42;
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "div", 7, 0),
                frame => AssertFrame.Attribute(frame, "value", "42", 1),
                frame => AssertFrame.Attribute(frame, "onchange", typeof(EventCallback), 2),
                frame => AssertFrame.MarkupWhitespace(frame, 3),
                frame => AssertFrame.Element(frame, "span", 2, 4),
                frame => AssertFrame.Text(frame, "42", 5),
                frame => AssertFrame.MarkupWhitespace(frame, 6));
        }

        [Fact]
        public void Render_BindFallback_InvalidSyntax_TooManyParts()
        {
            // Arrange & Act
            var generated = CompileToCSharp(@"
<input type=""text"" @bind-first-second-third=""Text"" />
@code {
    public string Text { get; set; } = ""text"";
}");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Equal("RZ9991", diagnostic.Id);
        }

        [Fact]
        public void Render_BindFallback_InvalidSyntax_TrailingDash()
        {
            // Arrange & Act
            var generated = CompileToCSharp(@"
<input type=""text"" @bind-first-=""Text"" />
@code {
    public string Text { get; set; } = ""text"";
}");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Equal("RZ9991", diagnostic.Id);
        }
    }
}
