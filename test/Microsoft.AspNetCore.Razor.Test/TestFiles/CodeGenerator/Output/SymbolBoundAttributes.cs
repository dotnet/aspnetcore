#pragma checksum "SymbolBoundAttributes.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "85cdc137d6a8c95fa926441de44d5187d5c8051e"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class SymbolBoundAttributes
    {
        #line hidden
        #pragma warning disable 0414
        private global::Microsoft.AspNet.Razor.TagHelperContent __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private global::Microsoft.AspNet.Razor.Runtime.TagHelperExecutionContext __tagHelperExecutionContext = null;
        private global::Microsoft.AspNet.Razor.Runtime.TagHelperRunner __tagHelperRunner = null;
        private global::Microsoft.AspNet.Razor.Runtime.TagHelperScopeManager __tagHelperScopeManager = new global::Microsoft.AspNet.Razor.Runtime.TagHelperScopeManager();
        private global::TestNamespace.CatchAllTagHelper __TestNamespace_CatchAllTagHelper = null;
        #line hidden
        public SymbolBoundAttributes()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new global::Microsoft.AspNet.Razor.Runtime.TagHelperRunner();
            Instrumentation.BeginContext(23, 253, true);
            WriteLiteral("\r\n<ul [item]=\"items\"></ul>\r\n<ul [(item)]=\"items\"></ul>\r\n<button (click)=\"doSomething()\">Click Me</button>\r\n<button (^click)=\"doSomething()\">Click Me</button>\r\n<template *something=\"value\">\r\n</template>\r\n<div #local></div>\r\n<div #local=\"value\"></div>\r\n\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("ul", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            __tagHelperExecutionContext.AddMinimizedHtmlAttribute("bound");
#line 12 "SymbolBoundAttributes.cshtml"
__TestNamespace_CatchAllTagHelper.ListItems = items;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("[item]", __TestNamespace_CatchAllTagHelper.ListItems);
            __tagHelperExecutionContext.AddHtmlAttribute("[item]", Html.Raw("items"));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(276, 45, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(321, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("ul", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            __tagHelperExecutionContext.AddMinimizedHtmlAttribute("bound");
#line 13 "SymbolBoundAttributes.cshtml"
__TestNamespace_CatchAllTagHelper.ArrayItems = items;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("[(item)]", __TestNamespace_CatchAllTagHelper.ArrayItems);
            __tagHelperExecutionContext.AddHtmlAttribute("[(item)]", Html.Raw("items"));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(323, 49, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(372, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("button", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(436, 8, true);
                WriteLiteral("Click Me");
                Instrumentation.EndContext();
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            __tagHelperExecutionContext.AddMinimizedHtmlAttribute("bound");
#line 14 "SymbolBoundAttributes.cshtml"
__TestNamespace_CatchAllTagHelper.Event1 = doSomething();

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("(click)", __TestNamespace_CatchAllTagHelper.Event1);
            __tagHelperExecutionContext.AddHtmlAttribute("(click)", Html.Raw("doSomething()"));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                __tagHelperExecutionContext.Output.Content = await __tagHelperExecutionContext.Output.GetChildContentAsync();
            }
            Instrumentation.BeginContext(374, 79, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(453, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("button", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(519, 8, true);
                WriteLiteral("Click Me");
                Instrumentation.EndContext();
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            __tagHelperExecutionContext.AddMinimizedHtmlAttribute("bound");
#line 15 "SymbolBoundAttributes.cshtml"
__TestNamespace_CatchAllTagHelper.Event2 = doSomething();

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("(^click)", __TestNamespace_CatchAllTagHelper.Event2);
            __tagHelperExecutionContext.AddHtmlAttribute("(^click)", Html.Raw("doSomething()"));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                __tagHelperExecutionContext.Output.Content = await __tagHelperExecutionContext.Output.GetChildContentAsync();
            }
            Instrumentation.BeginContext(455, 81, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(536, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("template", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(592, 2, true);
                WriteLiteral("\r\n");
                Instrumentation.EndContext();
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            __tagHelperExecutionContext.AddMinimizedHtmlAttribute("bound");
            __TestNamespace_CatchAllTagHelper.StringProperty1 = "value";
            __tagHelperExecutionContext.AddTagHelperAttribute("*something", __TestNamespace_CatchAllTagHelper.StringProperty1);
            __tagHelperExecutionContext.AddHtmlAttribute("*something", Html.Raw("value"));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                __tagHelperExecutionContext.Output.Content = await __tagHelperExecutionContext.Output.GetChildContentAsync();
            }
            Instrumentation.BeginContext(538, 67, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(605, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("div", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            __tagHelperExecutionContext.AddMinimizedHtmlAttribute("bound");
            __tagHelperExecutionContext.AddMinimizedHtmlAttribute("#localminimized");
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(607, 33, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(640, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("div", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            __tagHelperExecutionContext.AddMinimizedHtmlAttribute("bound");
            __TestNamespace_CatchAllTagHelper.StringProperty2 = "value";
            __tagHelperExecutionContext.AddTagHelperAttribute("#local", __TestNamespace_CatchAllTagHelper.StringProperty2);
            __tagHelperExecutionContext.AddHtmlAttribute("#local", Html.Raw("value"));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(642, 47, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
        }
        #pragma warning restore 1998
    }
}
