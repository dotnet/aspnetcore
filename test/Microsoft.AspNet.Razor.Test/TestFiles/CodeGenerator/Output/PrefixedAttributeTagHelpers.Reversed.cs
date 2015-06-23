#pragma checksum "PrefixedAttributeTagHelpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "4e7fe9697b745af1a07d41f6a8532fdc288fa046"
namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class PrefixedAttributeTagHelpers
    {
        #line hidden
        #pragma warning disable 0414
        private TagHelperContent __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private TagHelperExecutionContext __tagHelperExecutionContext = null;
        private TagHelperRunner __tagHelperRunner = null;
        private TagHelperScopeManager __tagHelperScopeManager = new TagHelperScopeManager();
        private InputTagHelper2 __InputTagHelper2 = null;
        private InputTagHelper1 __InputTagHelper1 = null;
        #line hidden
        public PrefixedAttributeTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new TagHelperRunner();
            Instrumentation.BeginContext(33, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 3 "PrefixedAttributeTagHelpers.cshtml"
  
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

            Instrumentation.BeginContext(280, 51, true);
            WriteLiteral("\r\n\r\n<div class=\"randomNonTagHelperAttribute\">\r\n    ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", true, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
            __tagHelperExecutionContext.Add(__InputTagHelper2);
            __InputTagHelper1 = CreateTagHelper<InputTagHelper1>();
            __tagHelperExecutionContext.Add(__InputTagHelper1);
            __tagHelperExecutionContext.AddHtmlAttribute("type", Html.Raw("checkbox"));
#line 16 "PrefixedAttributeTagHelpers.cshtml"
 __InputTagHelper2.IntDictionaryProperty = intDictionary;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("int-dictionary", __InputTagHelper2.IntDictionaryProperty);
            __InputTagHelper1.IntDictionaryProperty = __InputTagHelper2.IntDictionaryProperty;
#line 16 "PrefixedAttributeTagHelpers.cshtml"
                                __InputTagHelper2.StringDictionaryProperty = stringDictionary;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("string-dictionary", __InputTagHelper2.StringDictionaryProperty);
            __InputTagHelper1.StringDictionaryProperty = __InputTagHelper2.StringDictionaryProperty;
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(331, 92, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(423, 6, true);
            WriteLiteral("\r\n    ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", true, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
            __tagHelperExecutionContext.Add(__InputTagHelper2);
            if (__InputTagHelper2.IntDictionaryProperty == null)
            {
                throw new InvalidOperationException(FormatInvalidIndexerAssignment("int-prefix-garlic", "InputTagHelper2", "IntDictionaryProperty"));
            }
            __InputTagHelper1 = CreateTagHelper<InputTagHelper1>();
            __tagHelperExecutionContext.Add(__InputTagHelper1);
            if (__InputTagHelper1.IntDictionaryProperty == null)
            {
                throw new InvalidOperationException(FormatInvalidIndexerAssignment("int-prefix-garlic", "InputTagHelper1", "IntDictionaryProperty"));
            }
            __tagHelperExecutionContext.AddHtmlAttribute("type", Html.Raw("password"));
#line 17 "PrefixedAttributeTagHelpers.cshtml"
 __InputTagHelper2.IntDictionaryProperty = intDictionary;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("int-dictionary", __InputTagHelper2.IntDictionaryProperty);
            __InputTagHelper1.IntDictionaryProperty = __InputTagHelper2.IntDictionaryProperty;
#line 17 "PrefixedAttributeTagHelpers.cshtml"
                         __InputTagHelper2.IntDictionaryProperty["garlic"] = 37;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("int-prefix-garlic", __InputTagHelper2.IntDictionaryProperty["garlic"]);
            __InputTagHelper1.IntDictionaryProperty["garlic"] = __InputTagHelper2.IntDictionaryProperty["garlic"];
#line 17 "PrefixedAttributeTagHelpers.cshtml"
                                                __InputTagHelper2.IntDictionaryProperty["grabber"] = 42;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("int-prefix-grabber", __InputTagHelper2.IntDictionaryProperty["grabber"]);
            __InputTagHelper1.IntProperty = __InputTagHelper2.IntDictionaryProperty["grabber"];
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(429, 103, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(532, 6, true);
            WriteLiteral("\r\n    ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", true, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
            __tagHelperExecutionContext.Add(__InputTagHelper2);
            if (__InputTagHelper2.IntDictionaryProperty == null)
            {
                throw new InvalidOperationException(FormatInvalidIndexerAssignment("int-prefix-grabber", "InputTagHelper2", "IntDictionaryProperty"));
            }
            if (__InputTagHelper2.StringDictionaryProperty == null)
            {
                throw new InvalidOperationException(FormatInvalidIndexerAssignment("string-prefix-grabber", "InputTagHelper2", "StringDictionaryProperty"));
            }
            __InputTagHelper1 = CreateTagHelper<InputTagHelper1>();
            __tagHelperExecutionContext.Add(__InputTagHelper1);
            if (__InputTagHelper1.IntDictionaryProperty == null)
            {
                throw new InvalidOperationException(FormatInvalidIndexerAssignment("int-prefix-salt", "InputTagHelper1", "IntDictionaryProperty"));
            }
            if (__InputTagHelper1.StringDictionaryProperty == null)
            {
                throw new InvalidOperationException(FormatInvalidIndexerAssignment("string-prefix-paprika", "InputTagHelper1", "StringDictionaryProperty"));
            }
            __tagHelperExecutionContext.AddHtmlAttribute("type", Html.Raw("radio"));
#line 19 "PrefixedAttributeTagHelpers.cshtml"
__InputTagHelper2.IntDictionaryProperty["grabber"] = 42;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("int-prefix-grabber", __InputTagHelper2.IntDictionaryProperty["grabber"]);
            __InputTagHelper1.IntProperty = __InputTagHelper2.IntDictionaryProperty["grabber"];
#line 19 "PrefixedAttributeTagHelpers.cshtml"
  __InputTagHelper2.IntDictionaryProperty["salt"] = 37;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("int-prefix-salt", __InputTagHelper2.IntDictionaryProperty["salt"]);
            __InputTagHelper1.IntDictionaryProperty["salt"] = __InputTagHelper2.IntDictionaryProperty["salt"];
#line 19 "PrefixedAttributeTagHelpers.cshtml"
                       __InputTagHelper2.IntDictionaryProperty["pepper"] = 98;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("int-prefix-pepper", __InputTagHelper2.IntDictionaryProperty["pepper"]);
            __InputTagHelper1.IntDictionaryProperty["pepper"] = __InputTagHelper2.IntDictionaryProperty["pepper"];
            __tagHelperExecutionContext.AddHtmlAttribute("int-prefix-salt", Html.Raw("8"));
            __InputTagHelper2.StringDictionaryProperty["grabber"] = "string";
            __tagHelperExecutionContext.AddTagHelperAttribute("string-prefix-grabber", __InputTagHelper2.StringDictionaryProperty["grabber"]);
            __InputTagHelper1.StringProperty = __InputTagHelper2.StringDictionaryProperty["grabber"];
            __InputTagHelper2.StringDictionaryProperty["paprika"] = "another string";
            __tagHelperExecutionContext.AddTagHelperAttribute("string-prefix-paprika", __InputTagHelper2.StringDictionaryProperty["paprika"]);
            __InputTagHelper1.StringDictionaryProperty["paprika"] = __InputTagHelper2.StringDictionaryProperty["paprika"];
            StartTagHelperWritingScope();
            WriteLiteral("literate ");
#line 21 "PrefixedAttributeTagHelpers.cshtml"
WriteLiteral(literate);

#line default
#line hidden
            WriteLiteral("?");
            __tagHelperStringValueBuffer = EndTagHelperWritingScope();
            __InputTagHelper2.StringDictionaryProperty["cumin"] = __tagHelperStringValueBuffer.ToString();
            __tagHelperExecutionContext.AddTagHelperAttribute("string-prefix-cumin", __InputTagHelper2.StringDictionaryProperty["cumin"]);
            __InputTagHelper1.StringDictionaryProperty["cumin"] = __InputTagHelper2.StringDictionaryProperty["cumin"];
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(538, 257, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(795, 6, true);
            WriteLiteral("\r\n    ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", true, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
            __tagHelperExecutionContext.Add(__InputTagHelper2);
            if (__InputTagHelper2.IntDictionaryProperty == null)
            {
                throw new InvalidOperationException(FormatInvalidIndexerAssignment("int-prefix-value", "InputTagHelper2", "IntDictionaryProperty"));
            }
            if (__InputTagHelper2.StringDictionaryProperty == null)
            {
                throw new InvalidOperationException(FormatInvalidIndexerAssignment("string-prefix-thyme", "InputTagHelper2", "StringDictionaryProperty"));
            }
            __InputTagHelper1 = CreateTagHelper<InputTagHelper1>();
            __tagHelperExecutionContext.Add(__InputTagHelper1);
            if (__InputTagHelper1.IntDictionaryProperty == null)
            {
                throw new InvalidOperationException(FormatInvalidIndexerAssignment("int-prefix-value", "InputTagHelper1", "IntDictionaryProperty"));
            }
            if (__InputTagHelper1.StringDictionaryProperty == null)
            {
                throw new InvalidOperationException(FormatInvalidIndexerAssignment("string-prefix-thyme", "InputTagHelper1", "StringDictionaryProperty"));
            }
#line 22 "PrefixedAttributeTagHelpers.cshtml"
__InputTagHelper2.IntDictionaryProperty["value"] = 37;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("int-prefix-value", __InputTagHelper2.IntDictionaryProperty["value"]);
            __InputTagHelper1.IntDictionaryProperty["value"] = __InputTagHelper2.IntDictionaryProperty["value"];
            __InputTagHelper2.StringDictionaryProperty["thyme"] = "string";
            __tagHelperExecutionContext.AddTagHelperAttribute("string-prefix-thyme", __InputTagHelper2.StringDictionaryProperty["thyme"]);
            __InputTagHelper1.StringDictionaryProperty["thyme"] = __InputTagHelper2.StringDictionaryProperty["thyme"];
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(801, 60, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(861, 8, true);
            WriteLiteral("\r\n</div>");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
