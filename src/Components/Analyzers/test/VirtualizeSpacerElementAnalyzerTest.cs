// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class VirtualizeSpacerElementAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new VirtualizeSpacerElementAnalyzer();

    private static readonly string ComponentDeclarations = @"
    namespace Microsoft.AspNetCore.Components.Rendering
    {
        public class RenderTreeBuilder
        {
            public void OpenElement(int sequence, string elementName) { }
            public void CloseElement() { }
            public void OpenComponent<TComponent>(int sequence) where TComponent : IComponent { }
            public void AddAttribute(int sequence, string name, object value) { }
            public void AddComponentParameter(int sequence, string name, object value) { }
            public void AddMultipleAttributes(int sequence, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>> attributes) { }
            public void CloseComponent() { }
        }
    }

    namespace Microsoft.AspNetCore.Components
    {
        public interface IComponent { }
        public class TestContainer : IComponent { }
    }

    namespace Microsoft.AspNetCore.Components.Web.Virtualization
    {
        public class Virtualize<TItem> : Microsoft.AspNetCore.Components.IComponent
        {
            public string SpacerElement { get; set; }
        }
    }
";

    [Theory]
    [InlineData("tbody", "tr")]
    [InlineData("thead", "tr")]
    [InlineData("tfoot", "tr")]
    [InlineData("ul", "li")]
    [InlineData("ol", "li")]
    [InlineData("tr", "td", "th")]
    [InlineData("select", "option")]
    public void RestrictedParentWithoutSpacerElement_ReportsDiagnostic(string parentElement, params string[] allowedSpacerElements)
    {
        var allowedSpacerElementsMessage = string.Join(" or ", allowedSpacerElements.Select(element => $"SpacerElement=\"{element}\""));
        var test = $@"
    namespace TestApp
    {{
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {{
            void BuildRenderTree(RenderTreeBuilder builder)
            {{
                builder.OpenElement(0, ""{parentElement}"");
                builder.OpenComponent<Virtualize<string>>(1);
                builder.CloseComponent();
                builder.CloseElement();
            }}
        }}
    }}" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.VirtualizeSpacerElementIsInvalid.Id,
                Message = $"Virtualize inside '{parentElement}' cannot use spacer element 'div'. Use {allowedSpacerElementsMessage} instead.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 12, 17)
                }
            });
    }

    [Theory]
    [InlineData("tbody", "tr")]
    [InlineData("thead", "tr")]
    [InlineData("tfoot", "tr")]
    [InlineData("ul", "li")]
    [InlineData("ol", "li")]
    [InlineData("tr", "td")]
    [InlineData("tr", "th")]
    [InlineData("select", "option")]
    public void RestrictedParentWithCompatibleSpacerElement_NoDiagnostic(string parentElement, string spacerElement)
    {
        var test = @"
    namespace TestApp
    {
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, ""PARENT_ELEMENT"");
                builder.OpenComponent<Virtualize<string>>(1);
                builder.AddComponentParameter(2, ""SpacerElement"", ""SPACER_ELEMENT"");
                builder.CloseComponent();
                builder.CloseElement();
            }
        }
    }".Replace("PARENT_ELEMENT", parentElement).Replace("SPACER_ELEMENT", spacerElement) + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Theory]
    [InlineData("tbody", "td", "SpacerElement=\"tr\"")]
    [InlineData("thead", "th", "SpacerElement=\"tr\"")]
    [InlineData("tfoot", "div", "SpacerElement=\"tr\"")]
    [InlineData("ul", "div", "SpacerElement=\"li\"")]
    [InlineData("ol", "span", "SpacerElement=\"li\"")]
    [InlineData("tr", "div", "SpacerElement=\"td\" or SpacerElement=\"th\"")]
    [InlineData("select", "div", "SpacerElement=\"option\"")]
    public void RestrictedParentWithIncompatibleSpacerElement_ReportsDiagnostic(
        string parentElement,
        string spacerElement,
        string allowedSpacerElementsMessage)
    {
        var test = @"
    namespace TestApp
    {
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, ""PARENT_ELEMENT"");
                builder.OpenComponent<Virtualize<string>>(1);
                builder.AddComponentParameter(2, ""SpacerElement"", ""SPACER_ELEMENT"");
                builder.CloseComponent();
                builder.CloseElement();
            }
        }
    }".Replace("PARENT_ELEMENT", parentElement).Replace("SPACER_ELEMENT", spacerElement) + ComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.VirtualizeSpacerElementIsInvalid.Id,
                Message = $"Virtualize inside '{parentElement}' cannot use spacer element '{spacerElement}'. Use {allowedSpacerElementsMessage} instead.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 12, 17)
                }
            });
    }

    [Fact]
    public void UnrestrictedParentWithoutSpacerElement_NoDiagnostic()
    {
        var test = @"
    namespace TestApp
    {
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, ""div"");
                builder.OpenComponent<Virtualize<string>>(1);
                builder.CloseComponent();
                builder.CloseElement();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void ComponentParent_NoDiagnostic()
    {
        var test = @"
    namespace TestApp
    {
        using Microsoft.AspNetCore.Components;
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, ""tbody"");
                builder.OpenComponent<TestContainer>(1);
                builder.OpenComponent<Virtualize<string>>(2);
                builder.CloseComponent();
                builder.CloseComponent();
                builder.CloseElement();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void DynamicSpacerElement_NoDiagnostic()
    {
        var test = @"
    namespace TestApp
    {
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            string SpacerElement { get; set; }

            void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, ""tbody"");
                builder.OpenComponent<Virtualize<string>>(1);
                builder.AddComponentParameter(2, ""SpacerElement"", SpacerElement);
                builder.CloseComponent();
                builder.CloseElement();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void RazorTypeInferenceHelperUnderRestrictedParent_ReportsDiagnostic()
    {
        var test = @"
    namespace TestApp
    {
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, ""tbody"");
                TypeInference.CreateVirtualize_0<string>(builder, 1);
                builder.CloseElement();
            }
        }

        static class TypeInference
        {
            public static void CreateVirtualize_0<TItem>(RenderTreeBuilder builder, int sequence)
            {
                builder.OpenComponent<Virtualize<TItem>>(sequence);
                builder.CloseComponent();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.VirtualizeSpacerElementIsInvalid.Id,
                Message = "Virtualize inside 'tbody' cannot use spacer element 'div'. Use SpacerElement=\"tr\" instead.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 12, 17)
                }
            });
    }

    [Fact]
    public void RazorTypeInferenceHelperWithCompatibleSpacer_NoDiagnostic()
    {
        var test = @"
    namespace TestApp
    {
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, ""tbody"");
                TypeInference.CreateVirtualize_0<string>(builder, 1, ""tr"");
                builder.CloseElement();
            }
        }

        static class TypeInference
        {
            public static void CreateVirtualize_0<TItem>(RenderTreeBuilder builder, int sequence, string spacerElement)
            {
                builder.OpenComponent<Virtualize<TItem>>(sequence);
                builder.AddComponentParameter(2, ""SpacerElement"", spacerElement);
                builder.CloseComponent();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void UserAuthoredHelperUnderRestrictedParent_NoDiagnostic()
    {
        var test = @"
    namespace TestApp
    {
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, ""tbody"");
                CreateVirtualize(builder);
                builder.CloseElement();
            }

            static void CreateVirtualize(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, ""div"");
                builder.OpenComponent<Virtualize<string>>(1);
                builder.CloseComponent();
                builder.CloseElement();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void VirtualizeOpenedOnDifferentBuilder_NoDiagnostic()
    {
        var test = @"
    namespace TestApp
    {
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder builder, RenderTreeBuilder otherBuilder)
            {
                builder.OpenElement(0, ""tbody"");
                otherBuilder.OpenComponent<Virtualize<string>>(1);
                otherBuilder.CloseComponent();
                builder.CloseElement();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void VirtualizeInNestedRenderFragment_NoDiagnostic()
    {
        var test = @"
    namespace TestApp
    {
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, ""tbody"");
                System.Action<RenderTreeBuilder> fragment = nestedBuilder =>
                {
                    nestedBuilder.OpenComponent<Virtualize<string>>(1);
                    nestedBuilder.CloseComponent();
                };
                builder.CloseElement();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void RazorTypeInferenceHelperWithWrappedVirtualize_NoDiagnostic()
    {
        var test = @"
    namespace TestApp
    {
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, ""tbody"");
                TypeInference.CreateVirtualize_0<string>(builder, 1);
                builder.CloseElement();
            }
        }

        static class TypeInference
        {
            public static void CreateVirtualize_0<TItem>(RenderTreeBuilder builder, int sequence)
            {
                builder.OpenElement(0, ""div"");
                builder.OpenComponent<Virtualize<TItem>>(sequence);
                builder.CloseComponent();
                builder.CloseElement();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void UnknownSplatAfterExplicitSpacerElement_NoDiagnostic()
    {
        var test = @"
    namespace TestApp
    {
        using System.Collections.Generic;
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder builder, IEnumerable<KeyValuePair<string, object>> attributes)
            {
                builder.OpenElement(0, ""tbody"");
                builder.OpenComponent<Virtualize<string>>(1);
                builder.AddAttribute(2, ""SpacerElement"", ""td"");
                builder.AddMultipleAttributes(3, attributes);
                builder.CloseComponent();
                builder.CloseElement();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void ExplicitSpacerElementAfterUnknownSplat_ReportsDiagnostic()
    {
        var test = @"
    namespace TestApp
    {
        using System.Collections.Generic;
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder builder, IEnumerable<KeyValuePair<string, object>> attributes)
            {
                builder.OpenElement(0, ""tbody"");
                builder.OpenComponent<Virtualize<string>>(1);
                builder.AddMultipleAttributes(2, attributes);
                builder.AddComponentParameter(3, ""SpacerElement"", ""td"");
                builder.CloseComponent();
                builder.CloseElement();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.VirtualizeSpacerElementIsInvalid.Id,
                Message = "Virtualize inside 'tbody' cannot use spacer element 'td'. Use SpacerElement=\"tr\" instead.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 13, 17)
                }
            });
    }

    [Fact]
    public void AddAttributeWithInvalidSpacerElement_ReportsDiagnostic()
    {
        var test = @"
    namespace TestApp
    {
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, ""tbody"");
                builder.OpenComponent<Virtualize<string>>(1);
                builder.AddAttribute(2, ""SpacerElement"", ""td"");
                builder.CloseComponent();
                builder.CloseElement();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.VirtualizeSpacerElementIsInvalid.Id,
                Message = "Virtualize inside 'tbody' cannot use spacer element 'td'. Use SpacerElement=\"tr\" instead.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 12, 17)
                }
            });
    }

    [Fact]
    public void DiagnosticLocationMapsToRazorSource()
    {
        var test = @"
    namespace TestApp
    {
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, ""tbody"");
#line hidden
                TypeInference.CreateVirtualize_0<string>(builder, 1,
#line 42 ""Pages/VirtualizeTest.razor""
                    ""items"");
#line default
                builder.CloseComponent();
                builder.CloseElement();
            }
        }

        static class TypeInference
        {
            public static void CreateVirtualize_0<TItem>(RenderTreeBuilder builder, int sequence, string items)
            {
                builder.OpenComponent<Virtualize<TItem>>(sequence);
                builder.CloseComponent();
            }
        }
    }" + ComponentDeclarations;

        var document = CreateDocument(test);
        var diagnostic = Assert.Single(GetSortedDiagnosticsFromDocuments(
            GetCSharpDiagnosticAnalyzer(),
            new[] { document }));
        var mappedLineSpan = diagnostic.Location.GetMappedLineSpan();

        Assert.Equal("Pages/VirtualizeTest.razor", mappedLineSpan.Path);
        Assert.Equal(41, mappedLineSpan.StartLinePosition.Line);
    }

    [Fact]
    public void NullSplatAfterExplicitSpacerElement_ReportsDiagnostic()
    {
        var test = @"
    namespace TestApp
    {
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, ""tbody"");
                builder.OpenComponent<Virtualize<string>>(1);
                builder.AddComponentParameter(2, ""SpacerElement"", ""td"");
                builder.AddMultipleAttributes(3, null);
                builder.CloseComponent();
                builder.CloseElement();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.VirtualizeSpacerElementIsInvalid.Id,
                Message = "Virtualize inside 'tbody' cannot use spacer element 'td'. Use SpacerElement=\"tr\" instead.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 12, 17)
                }
            });
    }

    [Fact]
    public void RazorTypeInferenceHelperWithUnknownSplatAfterSpacerElement_NoDiagnostic()
    {
        var test = @"
    namespace TestApp
    {
        using System.Collections.Generic;
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder builder, IEnumerable<KeyValuePair<string, object>> attributes)
            {
                builder.OpenElement(0, ""tbody"");
                TypeInference.CreateVirtualize_0<string>(builder, 1, ""td"", attributes);
                builder.CloseElement();
            }
        }

        static class TypeInference
        {
            public static void CreateVirtualize_0<TItem>(RenderTreeBuilder builder, int sequence, string spacerElement, IEnumerable<KeyValuePair<string, object>> attributes)
            {
                builder.OpenComponent<Virtualize<TItem>>(sequence);
                builder.AddComponentParameter(2, ""SpacerElement"", spacerElement);
                builder.AddMultipleAttributes(3, attributes);
                builder.CloseComponent();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void RazorTypeInferenceHelperWithSpacerElementAfterUnknownSplat_ReportsDiagnostic()
    {
        var test = @"
    namespace TestApp
    {
        using System.Collections.Generic;
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder builder, IEnumerable<KeyValuePair<string, object>> attributes)
            {
                builder.OpenElement(0, ""tbody"");
                TypeInference.CreateVirtualize_0<string>(builder, 1, attributes, ""td"");
                builder.CloseElement();
            }
        }

        static class TypeInference
        {
            public static void CreateVirtualize_0<TItem>(RenderTreeBuilder builder, int sequence, IEnumerable<KeyValuePair<string, object>> attributes, string spacerElement)
            {
                builder.OpenComponent<Virtualize<TItem>>(sequence);
                builder.AddMultipleAttributes(2, attributes);
                builder.AddComponentParameter(3, ""SpacerElement"", spacerElement);
                builder.CloseComponent();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.VirtualizeSpacerElementIsInvalid.Id,
                Message = "Virtualize inside 'tbody' cannot use spacer element 'td'. Use SpacerElement=\"tr\" instead.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 13, 17)
                }
            });
    }

    [Fact]
    public void RazorTypeInferenceHelperWithNullSplatAfterSpacerElement_ReportsDiagnostic()
    {
        var test = @"
    namespace TestApp
    {
        using System.Collections.Generic;
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, ""tbody"");
                TypeInference.CreateVirtualize_0<string>(builder, 1, ""td"", null);
                builder.CloseElement();
            }
        }

        static class TypeInference
        {
            public static void CreateVirtualize_0<TItem>(RenderTreeBuilder builder, int sequence, string spacerElement, IEnumerable<KeyValuePair<string, object>> attributes)
            {
                builder.OpenComponent<Virtualize<TItem>>(sequence);
                builder.AddComponentParameter(2, ""SpacerElement"", spacerElement);
                builder.AddMultipleAttributes(3, attributes);
                builder.CloseComponent();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.VirtualizeSpacerElementIsInvalid.Id,
                Message = "Virtualize inside 'tbody' cannot use spacer element 'td'. Use SpacerElement=\"tr\" instead.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 13, 17)
                }
            });
    }
}