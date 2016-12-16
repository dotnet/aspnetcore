#pragma checksum "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/PrefixedAttributeTagHelpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "5ae668a393146e4a06179eb37952603907a9b825"
namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_PrefixedAttributeTagHelpers_Runtime
    {
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
        private global::TestNamespace.InputTagHelper1 __TestNamespace_InputTagHelper1 = null;
        private global::TestNamespace.InputTagHelper2 __TestNamespace_InputTagHelper2 = null;
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral("\r\n");
#line 3 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/PrefixedAttributeTagHelpers.cshtml"
  
    var literate = "or illiterate";
    var intDictionary = new Dictionary<string, int>
    {
        { "three", 3 },
    };
    var stringDictionary = new SortedDictionary<string, string>
    {
        { "name", "value" },
    };


#line default
#line hidden
            WriteLiteral("\r\n<div class=\"randomNonTagHelperAttribute\">\r\n    ");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.Evolution.Legacy.TagMode.SelfClosing, "test", async() => {
            }
            );
            __TestNamespace_InputTagHelper1 = CreateTagHelper<global::TestNamespace.InputTagHelper1>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper1);
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
            BeginWriteTagHelperAttribute();
            WriteLiteral("checkbox");
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute("type", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
#line 16 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/PrefixedAttributeTagHelpers.cshtml"
__TestNamespace_InputTagHelper1.IntDictionaryProperty = intDictionary;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("int-dictionary", __TestNamespace_InputTagHelper1.IntDictionaryProperty, global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            __TestNamespace_InputTagHelper2.IntDictionaryProperty = __TestNamespace_InputTagHelper1.IntDictionaryProperty;
#line 16 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/PrefixedAttributeTagHelpers.cshtml"
__TestNamespace_InputTagHelper1.StringDictionaryProperty = stringDictionary;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("string-dictionary", __TestNamespace_InputTagHelper1.StringDictionaryProperty, global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            __TestNamespace_InputTagHelper2.StringDictionaryProperty = __TestNamespace_InputTagHelper1.StringDictionaryProperty;
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n    ");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.Evolution.Legacy.TagMode.SelfClosing, "test", async() => {
            }
            );
            __TestNamespace_InputTagHelper1 = CreateTagHelper<global::TestNamespace.InputTagHelper1>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper1);
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
            BeginWriteTagHelperAttribute();
            WriteLiteral("password");
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute("type", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
#line 17 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/PrefixedAttributeTagHelpers.cshtml"
__TestNamespace_InputTagHelper1.IntDictionaryProperty = intDictionary;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("int-dictionary", __TestNamespace_InputTagHelper1.IntDictionaryProperty, global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            __TestNamespace_InputTagHelper2.IntDictionaryProperty = __TestNamespace_InputTagHelper1.IntDictionaryProperty;
            if (__TestNamespace_InputTagHelper1.IntDictionaryProperty == null)
            {
                throw new InvalidOperationException(FormatInvalidIndexerAssignment("int-prefix-garlic", "TestNamespace.InputTagHelper1", "IntDictionaryProperty"));
            }
#line 17 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/PrefixedAttributeTagHelpers.cshtml"
__TestNamespace_InputTagHelper1.IntDictionaryProperty["garlic"] = 37;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("int-prefix-garlic", __TestNamespace_InputTagHelper1.IntDictionaryProperty["garlic"], global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            __TestNamespace_InputTagHelper2.IntDictionaryProperty["garlic"] = __TestNamespace_InputTagHelper1.IntDictionaryProperty["garlic"];
#line 17 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/PrefixedAttributeTagHelpers.cshtml"
__TestNamespace_InputTagHelper1.IntProperty = 42;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("int-prefix-grabber", __TestNamespace_InputTagHelper1.IntProperty, global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            __TestNamespace_InputTagHelper2.IntDictionaryProperty["grabber"] = __TestNamespace_InputTagHelper1.IntProperty;
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n    ");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.Evolution.Legacy.TagMode.SelfClosing, "test", async() => {
            }
            );
            __TestNamespace_InputTagHelper1 = CreateTagHelper<global::TestNamespace.InputTagHelper1>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper1);
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
            BeginWriteTagHelperAttribute();
            WriteLiteral("radio");
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute("type", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
#line 19 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/PrefixedAttributeTagHelpers.cshtml"
__TestNamespace_InputTagHelper1.IntProperty = 42;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("int-prefix-grabber", __TestNamespace_InputTagHelper1.IntProperty, global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            if (__TestNamespace_InputTagHelper2.IntDictionaryProperty == null)
            {
                throw new InvalidOperationException(FormatInvalidIndexerAssignment("int-prefix-grabber", "TestNamespace.InputTagHelper2", "IntDictionaryProperty"));
            }
            __TestNamespace_InputTagHelper2.IntDictionaryProperty["grabber"] = __TestNamespace_InputTagHelper1.IntProperty;
#line 19 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/PrefixedAttributeTagHelpers.cshtml"
__TestNamespace_InputTagHelper1.IntDictionaryProperty["salt"] = 37;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("int-prefix-salt", __TestNamespace_InputTagHelper1.IntDictionaryProperty["salt"], global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            __TestNamespace_InputTagHelper2.IntDictionaryProperty["salt"] = __TestNamespace_InputTagHelper1.IntDictionaryProperty["salt"];
#line 19 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/PrefixedAttributeTagHelpers.cshtml"
__TestNamespace_InputTagHelper1.IntDictionaryProperty["pepper"] = 98;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("int-prefix-pepper", __TestNamespace_InputTagHelper1.IntDictionaryProperty["pepper"], global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            __TestNamespace_InputTagHelper2.IntDictionaryProperty["pepper"] = __TestNamespace_InputTagHelper1.IntDictionaryProperty["pepper"];
            BeginWriteTagHelperAttribute();
            WriteLiteral("8");
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute("int-prefix-salt", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            BeginWriteTagHelperAttribute();
            WriteLiteral("string");
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __TestNamespace_InputTagHelper1.StringProperty = __tagHelperStringValueBuffer;
            __tagHelperExecutionContext.AddTagHelperAttribute("string-prefix-grabber", __TestNamespace_InputTagHelper1.StringProperty, global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            if (__TestNamespace_InputTagHelper2.StringDictionaryProperty == null)
            {
                throw new InvalidOperationException(FormatInvalidIndexerAssignment("string-prefix-grabber", "TestNamespace.InputTagHelper2", "StringDictionaryProperty"));
            }
            __TestNamespace_InputTagHelper2.StringDictionaryProperty["grabber"] = __TestNamespace_InputTagHelper1.StringProperty;
            BeginWriteTagHelperAttribute();
            WriteLiteral("another string");
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __TestNamespace_InputTagHelper1.StringDictionaryProperty["paprika"] = __tagHelperStringValueBuffer;
            __tagHelperExecutionContext.AddTagHelperAttribute("string-prefix-paprika", __TestNamespace_InputTagHelper1.StringDictionaryProperty["paprika"], global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            __TestNamespace_InputTagHelper2.StringDictionaryProperty["paprika"] = __TestNamespace_InputTagHelper1.StringDictionaryProperty["paprika"];
            BeginWriteTagHelperAttribute();
            WriteLiteral("literate ");
#line 21 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/PrefixedAttributeTagHelpers.cshtml"
                             WriteLiteral(literate);

#line default
#line hidden
            WriteLiteral("?");
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __TestNamespace_InputTagHelper1.StringDictionaryProperty["cumin"] = __tagHelperStringValueBuffer;
            __tagHelperExecutionContext.AddTagHelperAttribute("string-prefix-cumin", __TestNamespace_InputTagHelper1.StringDictionaryProperty["cumin"], global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            __TestNamespace_InputTagHelper2.StringDictionaryProperty["cumin"] = __TestNamespace_InputTagHelper1.StringDictionaryProperty["cumin"];
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n    ");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.Evolution.Legacy.TagMode.SelfClosing, "test", async() => {
            }
            );
            __TestNamespace_InputTagHelper1 = CreateTagHelper<global::TestNamespace.InputTagHelper1>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper1);
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
            if (__TestNamespace_InputTagHelper1.IntDictionaryProperty == null)
            {
                throw new InvalidOperationException(FormatInvalidIndexerAssignment("int-prefix-value", "TestNamespace.InputTagHelper1", "IntDictionaryProperty"));
            }
#line 22 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/PrefixedAttributeTagHelpers.cshtml"
__TestNamespace_InputTagHelper1.IntDictionaryProperty["value"] = 37;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("int-prefix-value", __TestNamespace_InputTagHelper1.IntDictionaryProperty["value"], global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            __TestNamespace_InputTagHelper2.IntDictionaryProperty["value"] = __TestNamespace_InputTagHelper1.IntDictionaryProperty["value"];
            if (__TestNamespace_InputTagHelper1.StringDictionaryProperty == null)
            {
                throw new InvalidOperationException(FormatInvalidIndexerAssignment("string-prefix-thyme", "TestNamespace.InputTagHelper1", "StringDictionaryProperty"));
            }
            BeginWriteTagHelperAttribute();
            WriteLiteral("string");
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __TestNamespace_InputTagHelper1.StringDictionaryProperty["thyme"] = __tagHelperStringValueBuffer;
            __tagHelperExecutionContext.AddTagHelperAttribute("string-prefix-thyme", __TestNamespace_InputTagHelper1.StringDictionaryProperty["thyme"], global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            __TestNamespace_InputTagHelper2.StringDictionaryProperty["thyme"] = __TestNamespace_InputTagHelper1.StringDictionaryProperty["thyme"];
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n</div>");
        }
        #pragma warning restore 1998
    }
}
