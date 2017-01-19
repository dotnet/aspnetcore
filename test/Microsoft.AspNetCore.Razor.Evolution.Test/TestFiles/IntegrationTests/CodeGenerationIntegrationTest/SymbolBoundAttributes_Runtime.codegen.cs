#pragma checksum "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/SymbolBoundAttributes.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "10f840b503550891681c6569925ce1a183d07e75"
namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_SymbolBoundAttributes_Runtime
    {
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_0 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("[item]", new global::Microsoft.AspNetCore.Html.HtmlString("items"), global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_1 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("[(item)]", new global::Microsoft.AspNetCore.Html.HtmlString("items"), global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_2 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("(click)", new global::Microsoft.AspNetCore.Html.HtmlString("doSomething()"), global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_3 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("(^click)", new global::Microsoft.AspNetCore.Html.HtmlString("doSomething()"), global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_4 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("*something", "value", global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_5 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("*something", new global::Microsoft.AspNetCore.Html.HtmlString("value"), global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_6 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("#local", "value", global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_7 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("#local", new global::Microsoft.AspNetCore.Html.HtmlString("value"), global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
        #line hidden
        #pragma warning disable 0414
        private string __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::TestNamespace.CatchAllTagHelper __TestNamespace_CatchAllTagHelper = null;
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral("\r\n<ul [item]=\"items\"></ul>\r\n<ul [(item)]=\"items\"></ul>\r\n<button (click)=\"doSomething()\">Click Me</button>\r\n<button (^click)=\"doSomething()\">Click Me</button>\r\n<template *something=\"value\">\r\n</template>\r\n<div #local></div>\r\n<div #local=\"value\"></div>\r\n\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("ul", global::Microsoft.AspNetCore.Razor.Evolution.TagMode.StartTagAndEndTag, "test", async() => {
            }
            );
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            BeginWriteTagHelperAttribute();
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute("bound", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.Minimized);
#line 12 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/SymbolBoundAttributes.cshtml"
__TestNamespace_CatchAllTagHelper.ListItems = items;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("[item]", __TestNamespace_CatchAllTagHelper.ListItems, global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_0);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("ul", global::Microsoft.AspNetCore.Razor.Evolution.TagMode.StartTagAndEndTag, "test", async() => {
            }
            );
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            BeginWriteTagHelperAttribute();
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute("bound", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.Minimized);
#line 13 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/SymbolBoundAttributes.cshtml"
__TestNamespace_CatchAllTagHelper.ArrayItems = items;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("[(item)]", __TestNamespace_CatchAllTagHelper.ArrayItems, global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_1);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("button", global::Microsoft.AspNetCore.Razor.Evolution.TagMode.StartTagAndEndTag, "test", async() => {
                WriteLiteral("Click Me");
            }
            );
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            BeginWriteTagHelperAttribute();
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute("bound", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.Minimized);
#line 14 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/SymbolBoundAttributes.cshtml"
__TestNamespace_CatchAllTagHelper.Event1 = doSomething();

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("(click)", __TestNamespace_CatchAllTagHelper.Event1, global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_2);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("button", global::Microsoft.AspNetCore.Razor.Evolution.TagMode.StartTagAndEndTag, "test", async() => {
                WriteLiteral("Click Me");
            }
            );
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            BeginWriteTagHelperAttribute();
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute("bound", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.Minimized);
#line 15 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/SymbolBoundAttributes.cshtml"
__TestNamespace_CatchAllTagHelper.Event2 = doSomething();

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("(^click)", __TestNamespace_CatchAllTagHelper.Event2, global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_3);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("template", global::Microsoft.AspNetCore.Razor.Evolution.TagMode.StartTagAndEndTag, "test", async() => {
                WriteLiteral("\r\n");
            }
            );
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            BeginWriteTagHelperAttribute();
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute("bound", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.Minimized);
            __TestNamespace_CatchAllTagHelper.StringProperty1 = (string)__tagHelperAttribute_4.Value;
            __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_4);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_5);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("div", global::Microsoft.AspNetCore.Razor.Evolution.TagMode.StartTagAndEndTag, "test", async() => {
            }
            );
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            BeginWriteTagHelperAttribute();
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute("bound", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.Minimized);
            BeginWriteTagHelperAttribute();
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute("#localminimized", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.Minimized);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("div", global::Microsoft.AspNetCore.Razor.Evolution.TagMode.StartTagAndEndTag, "test", async() => {
            }
            );
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            BeginWriteTagHelperAttribute();
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute("bound", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.Minimized);
            __TestNamespace_CatchAllTagHelper.StringProperty2 = (string)__tagHelperAttribute_6.Value;
            __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_6);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_7);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
        }
        #pragma warning restore 1998
    }
}
