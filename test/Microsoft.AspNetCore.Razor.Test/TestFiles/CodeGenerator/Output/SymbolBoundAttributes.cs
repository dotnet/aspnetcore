#pragma checksum "SymbolBoundAttributes.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "85cdc137d6a8c95fa926441de44d5187d5c8051e"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class SymbolBoundAttributes
    {
        #line hidden
        #pragma warning disable 0414
        private string __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelperExecutionContext __tagHelperExecutionContext = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelperRunner __tagHelperRunner = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelperScopeManager __tagHelperScopeManager = null;
        private global::TestNamespace.CatchAllTagHelper __TestNamespace_CatchAllTagHelper = null;
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_0 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("bound");
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_1 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("[item]", new global::Microsoft.AspNetCore.Html.HtmlString("items"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_2 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("[(item)]", new global::Microsoft.AspNetCore.Html.HtmlString("items"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_3 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("(click)", new global::Microsoft.AspNetCore.Html.HtmlString("doSomething()"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_4 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("(^click)", new global::Microsoft.AspNetCore.Html.HtmlString("doSomething()"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_5 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("*something", "value", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_6 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("*something", new global::Microsoft.AspNetCore.Html.HtmlString("value"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_7 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("#localminimized");
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_8 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("#local", "value", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_9 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("#local", new global::Microsoft.AspNetCore.Html.HtmlString("value"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        #line hidden
        public SymbolBoundAttributes()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new global::Microsoft.AspNetCore.Razor.Runtime.TagHelperRunner();
            __tagHelperScopeManager = __tagHelperScopeManager ?? new global::Microsoft.AspNetCore.Razor.Runtime.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
            Instrumentation.BeginContext(23, 253, true);
            WriteLiteral("\r\n<ul [item]=\"items\"></ul>\r\n<ul [(item)]=\"items\"></ul>\r\n<button (click)=\"doSomething()\">Click Me</button>\r\n<button (^click)=\"doSomething()\">Click Me</button>\r\n<template *something=\"value\">\r\n</template>\r\n<div #local></div>\r\n<div #local=\"value\"></div>\r\n\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("ul", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
            }
            );
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_0);
#line 12 "SymbolBoundAttributes.cshtml"
__TestNamespace_CatchAllTagHelper.ListItems = items;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("[item]", __TestNamespace_CatchAllTagHelper.ListItems, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_1);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(276, 45, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(321, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("ul", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
            }
            );
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_0);
#line 13 "SymbolBoundAttributes.cshtml"
__TestNamespace_CatchAllTagHelper.ArrayItems = items;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("[(item)]", __TestNamespace_CatchAllTagHelper.ArrayItems, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_2);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(323, 49, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(372, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("button", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(436, 8, true);
                WriteLiteral("Click Me");
                Instrumentation.EndContext();
            }
            );
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_0);
#line 14 "SymbolBoundAttributes.cshtml"
__TestNamespace_CatchAllTagHelper.Event1 = doSomething();

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("(click)", __TestNamespace_CatchAllTagHelper.Event1, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_3);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Instrumentation.BeginContext(374, 79, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(453, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("button", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(519, 8, true);
                WriteLiteral("Click Me");
                Instrumentation.EndContext();
            }
            );
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_0);
#line 15 "SymbolBoundAttributes.cshtml"
__TestNamespace_CatchAllTagHelper.Event2 = doSomething();

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("(^click)", __TestNamespace_CatchAllTagHelper.Event2, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_4);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Instrumentation.BeginContext(455, 81, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(536, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("template", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(592, 2, true);
                WriteLiteral("\r\n");
                Instrumentation.EndContext();
            }
            );
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_0);
            __TestNamespace_CatchAllTagHelper.StringProperty1 = (string)__tagHelperAttribute_5.Value;
            __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_5);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_6);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Instrumentation.BeginContext(538, 67, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(605, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("div", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
            }
            );
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_0);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_7);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(607, 33, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(640, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("div", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
            }
            );
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_0);
            __TestNamespace_CatchAllTagHelper.StringProperty2 = (string)__tagHelperAttribute_8.Value;
            __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_8);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_9);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(642, 47, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
        }
        #pragma warning restore 1998
    }
}
