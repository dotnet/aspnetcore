#pragma checksum "ComplexTagHelpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "7540f76029963ad4e3c0a02077a63d0fe2cb0157"
namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class ComplexTagHelpers
    {
        #line hidden
        private System.IO.TextWriter __tagHelperStringValueBuffer = null;
        private TagHelperExecutionContext __tagHelperExecutionContext = null;
        private TagHelperRunner __tagHelperRunner = new TagHelperRunner();
        private TagHelperScopeManager __tagHelperScopeManager = new TagHelperScopeManager();
        private PTagHelper __PTagHelper = null;
        private InputTagHelper __InputTagHelper = null;
        private InputTagHelper2 __InputTagHelper2 = null;
        #line hidden
        public ComplexTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 1 "ComplexTagHelpers.cshtml"
 if (true)
{
    var checkbox = "checkbox";


#line default
#line hidden

            Instrumentation.BeginContext(49, 55, true);
            WriteLiteral("    <div class=\"randomNonTagHelperAttribute\">\r\n        ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p");
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
            StartWritingScope();
            WriteLiteral("Current Time: ");
#line 6 "ComplexTagHelpers.cshtml"
Write(DateTime.Now);

#line default
#line hidden
            __tagHelperStringValueBuffer = EndWritingScope();
            __tagHelperExecutionContext.AddHtmlAttribute("time", __tagHelperStringValueBuffer.ToString());
            __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateStartTag());
            Instrumentation.BeginContext(142, 34, true);
            WriteLiteral("\r\n            <h1>Set Time:</h1>\r\n");
            Instrumentation.EndContext();
#line 8 "ComplexTagHelpers.cshtml"
            

#line default
#line hidden

#line 8 "ComplexTagHelpers.cshtml"
             if (false)
            {

#line default
#line hidden

            Instrumentation.BeginContext(216, 16, true);
            WriteLiteral("                ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p");
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
            __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateStartTag());
            Instrumentation.BeginContext(235, 10, true);
            WriteLiteral("New Time: ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input");
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __tagHelperExecutionContext.Add(__InputTagHelper);
            __InputTagHelper.Type = "text";
            __tagHelperExecutionContext.AddTagHelperAttribute("type", __InputTagHelper.Type);
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
            __tagHelperExecutionContext.Add(__InputTagHelper2);
            __InputTagHelper2.Type = __InputTagHelper.Type;
            __tagHelperExecutionContext.AddHtmlAttribute("value", "");
            __tagHelperExecutionContext.AddHtmlAttribute("placeholder", "Enter in a new time...");
            __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateStartTag());
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateEndTag());
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateEndTag());
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(315, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 11 "ComplexTagHelpers.cshtml"
            }
            else
            {

#line default
#line hidden

            Instrumentation.BeginContext(365, 16, true);
            WriteLiteral("                ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p");
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
            __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateStartTag());
            Instrumentation.BeginContext(384, 14, true);
            WriteLiteral("Current Time: ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input");
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __tagHelperExecutionContext.Add(__InputTagHelper);
            StartWritingScope();
#line 14 "ComplexTagHelpers.cshtml"
Write(checkbox);

#line default
#line hidden
            __tagHelperStringValueBuffer = EndWritingScope();
            __InputTagHelper.Type = __tagHelperStringValueBuffer.ToString();
            __tagHelperExecutionContext.AddTagHelperAttribute("type", __InputTagHelper.Type);
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
            __tagHelperExecutionContext.Add(__InputTagHelper2);
            __InputTagHelper2.Type = __InputTagHelper.Type;
            __InputTagHelper2.Checked = true;
            __tagHelperExecutionContext.AddTagHelperAttribute("checked", __InputTagHelper2.Checked);
            __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateStartTag());
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateEndTag());
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateEndTag());
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(439, 18, true);
            WriteLiteral("\r\n                ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input");
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __tagHelperExecutionContext.Add(__InputTagHelper);
            StartWritingScope();
#line 15 "ComplexTagHelpers.cshtml"
Write(true ? "checkbox" : "anything");

#line default
#line hidden
            __tagHelperStringValueBuffer = EndWritingScope();
            __InputTagHelper.Type = __tagHelperStringValueBuffer.ToString();
            __tagHelperExecutionContext.AddTagHelperAttribute("type", __InputTagHelper.Type);
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
            __tagHelperExecutionContext.Add(__InputTagHelper2);
            __InputTagHelper2.Type = __InputTagHelper.Type;
            __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateStartTag());
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateEndTag());
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(507, 18, true);
            WriteLiteral("\r\n                ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input");
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __tagHelperExecutionContext.Add(__InputTagHelper);
            StartWritingScope();
#line 16 "ComplexTagHelpers.cshtml"
if(true) {

#line default
#line hidden

            WriteLiteral(" checkbox ");
#line 16 "ComplexTagHelpers.cshtml"
} else {

#line default
#line hidden

            WriteLiteral(" anything ");
#line 16 "ComplexTagHelpers.cshtml"
}

#line default
#line hidden

            __tagHelperStringValueBuffer = EndWritingScope();
            __InputTagHelper.Type = __tagHelperStringValueBuffer.ToString();
            __tagHelperExecutionContext.AddTagHelperAttribute("type", __InputTagHelper.Type);
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
            __tagHelperExecutionContext.Add(__InputTagHelper2);
            __InputTagHelper2.Type = __InputTagHelper.Type;
            __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateStartTag());
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateEndTag());
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(608, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 17 "ComplexTagHelpers.cshtml"
            }

#line default
#line hidden

            Instrumentation.BeginContext(625, 8, true);
            WriteLiteral("        ");
            Instrumentation.EndContext();
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateEndTag());
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(637, 14, true);
            WriteLiteral("\r\n    </div>\r\n");
            Instrumentation.EndContext();
#line 20 "ComplexTagHelpers.cshtml"
}

#line default
#line hidden

        }
        #pragma warning restore 1998
    }
}
