#pragma checksum "DynamicAttributeTagHelpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "782463195265ee647cc2fc63fd5095a80090845b"
namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class DynamicAttributeTagHelpers
    {
        #line hidden
        #pragma warning disable 0414
        private TagHelperContent __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private TagHelperExecutionContext __tagHelperExecutionContext = null;
        private TagHelperRunner __tagHelperRunner = null;
        private TagHelperScopeManager __tagHelperScopeManager = new TagHelperScopeManager();
        private InputTagHelper __InputTagHelper = null;
        #line hidden
        public DynamicAttributeTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new TagHelperRunner();
            Instrumentation.BeginContext(33, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", true, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __tagHelperExecutionContext.Add(__InputTagHelper);
            AddHtmlAttributeValues("unbound", __tagHelperExecutionContext, Tuple.Create(Tuple.Create("", 51), Tuple.Create("prefix", 51), true), 
            Tuple.Create(Tuple.Create(" ", 57), Tuple.Create<System.Object, System.Int32>(DateTime.Now, 58), false));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(35, 40, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(75, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", true, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __tagHelperExecutionContext.Add(__InputTagHelper);
            AddHtmlAttributeValues("unbound", __tagHelperExecutionContext, 
            Tuple.Create(Tuple.Create("", 95), Tuple.Create<System.Object, System.Int32>(new Template((__razor_attribute_value_writer) => {
#line 5 "DynamicAttributeTagHelpers.cshtml"
if (true) { 

#line default
#line hidden

                Instrumentation.BeginContext(109, 12, false);
#line 5 "DynamicAttributeTagHelpers.cshtml"
WriteTo(__razor_attribute_value_writer, string.Empty);

#line default
#line hidden
                Instrumentation.EndContext();
#line 5 "DynamicAttributeTagHelpers.cshtml"
 } else { 

#line default
#line hidden

                Instrumentation.BeginContext(132, 5, false);
#line 5 "DynamicAttributeTagHelpers.cshtml"
WriteTo(__razor_attribute_value_writer, false);

#line default
#line hidden
                Instrumentation.EndContext();
#line 5 "DynamicAttributeTagHelpers.cshtml"
 }

#line default
#line hidden

            }
            ), 95), false), Tuple.Create(Tuple.Create(" ", 139), Tuple.Create("suffix", 140), true));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(79, 71, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(150, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", true, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __tagHelperExecutionContext.Add(__InputTagHelper);
            StartTagHelperWritingScope();
            WriteLiteral("prefix ");
#line 7 "DynamicAttributeTagHelpers.cshtml"
WriteLiteral(DateTime.Now);

#line default
#line hidden
            WriteLiteral(" suffix");
            __tagHelperStringValueBuffer = EndTagHelperWritingScope();
            __InputTagHelper.Bound = __tagHelperStringValueBuffer.ToString();
            __tagHelperExecutionContext.AddTagHelperAttribute("bound", __InputTagHelper.Bound);
            AddHtmlAttributeValues("unbound", __tagHelperExecutionContext, Tuple.Create(Tuple.Create("", 206), Tuple.Create("prefix", 206), true), 
            Tuple.Create(Tuple.Create(" ", 212), Tuple.Create<System.Object, System.Int32>(DateTime.Now, 213), false), Tuple.Create(Tuple.Create(" ", 226), Tuple.Create("suffix", 227), true));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(154, 83, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(237, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", true, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __tagHelperExecutionContext.Add(__InputTagHelper);
            StartTagHelperWritingScope();
#line 9 "DynamicAttributeTagHelpers.cshtml"
WriteLiteral(long.MinValue);

#line default
#line hidden
            WriteLiteral(" ");
#line 9 "DynamicAttributeTagHelpers.cshtml"
if (true) { 

#line default
#line hidden

#line 9 "DynamicAttributeTagHelpers.cshtml"
WriteLiteral(string.Empty);

#line default
#line hidden
#line 9 "DynamicAttributeTagHelpers.cshtml"
 } else { 

#line default
#line hidden

#line 9 "DynamicAttributeTagHelpers.cshtml"
WriteLiteral(false);

#line default
#line hidden
#line 9 "DynamicAttributeTagHelpers.cshtml"
 }

#line default
#line hidden

            WriteLiteral(" ");
#line 9 "DynamicAttributeTagHelpers.cshtml"
WriteLiteral(int.MaxValue);

#line default
#line hidden
            __tagHelperStringValueBuffer = EndTagHelperWritingScope();
            __InputTagHelper.Bound = __tagHelperStringValueBuffer.ToString();
            __tagHelperExecutionContext.AddTagHelperAttribute("bound", __InputTagHelper.Bound);
            AddHtmlAttributeValues("unbound", __tagHelperExecutionContext, 
            Tuple.Create(Tuple.Create("", 347), Tuple.Create<System.Object, System.Int32>(long.MinValue, 347), false), 
            Tuple.Create(Tuple.Create(" ", 361), Tuple.Create<System.Object, System.Int32>(new Template((__razor_attribute_value_writer) => {
#line 10 "DynamicAttributeTagHelpers.cshtml"
if (true) { 

#line default
#line hidden

                Instrumentation.BeginContext(376, 12, false);
#line 10 "DynamicAttributeTagHelpers.cshtml"
WriteTo(__razor_attribute_value_writer, string.Empty);

#line default
#line hidden
                Instrumentation.EndContext();
#line 10 "DynamicAttributeTagHelpers.cshtml"
 } else { 

#line default
#line hidden

                Instrumentation.BeginContext(399, 5, false);
#line 10 "DynamicAttributeTagHelpers.cshtml"
WriteTo(__razor_attribute_value_writer, false);

#line default
#line hidden
                Instrumentation.EndContext();
#line 10 "DynamicAttributeTagHelpers.cshtml"
 }

#line default
#line hidden

            }
            ), 362), false), 
            Tuple.Create(Tuple.Create(" ", 406), Tuple.Create<System.Object, System.Int32>(int.MaxValue, 407), false));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(241, 183, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(424, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", true, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __tagHelperExecutionContext.Add(__InputTagHelper);
            AddHtmlAttributeValues("unbound", __tagHelperExecutionContext, 
            Tuple.Create(Tuple.Create("", 444), Tuple.Create<System.Object, System.Int32>(long.MinValue, 444), false), 
            Tuple.Create(Tuple.Create(" ", 458), Tuple.Create<System.Object, System.Int32>(DateTime.Now, 459), false), Tuple.Create(Tuple.Create(" ", 472), Tuple.Create("static", 473), true), Tuple.Create(Tuple.Create("    ", 479), Tuple.Create("content", 483), true), 
            Tuple.Create(Tuple.Create(" ", 490), Tuple.Create<System.Object, System.Int32>(int.MaxValue, 491), false));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(428, 80, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(508, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", true, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __tagHelperExecutionContext.Add(__InputTagHelper);
            AddHtmlAttributeValues("unbound", __tagHelperExecutionContext, 
            Tuple.Create(Tuple.Create("", 528), Tuple.Create<System.Object, System.Int32>(new Template((__razor_attribute_value_writer) => {
#line 14 "DynamicAttributeTagHelpers.cshtml"
if (true) { 

#line default
#line hidden

                Instrumentation.BeginContext(542, 12, false);
#line 14 "DynamicAttributeTagHelpers.cshtml"
WriteTo(__razor_attribute_value_writer, string.Empty);

#line default
#line hidden
                Instrumentation.EndContext();
#line 14 "DynamicAttributeTagHelpers.cshtml"
 } else { 

#line default
#line hidden

                Instrumentation.BeginContext(565, 5, false);
#line 14 "DynamicAttributeTagHelpers.cshtml"
WriteTo(__razor_attribute_value_writer, false);

#line default
#line hidden
                Instrumentation.EndContext();
#line 14 "DynamicAttributeTagHelpers.cshtml"
 }

#line default
#line hidden

            }
            ), 528), false));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(512, 64, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
        }
        #pragma warning restore 1998
    }
}
