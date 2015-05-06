#pragma checksum "CodeBlockWithTextElement.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "740b5af27dd6c6ff0e88b39a02d4bf1a38fcdc0b"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class CodeBlockWithTextElement
    {
        #line hidden
        public CodeBlockWithTextElement()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 1 "CodeBlockWithTextElement.cshtml"
  
    var a = 1;

#line default
#line hidden

            Instrumentation.BeginContext(26, 1, false);
#line 2 "CodeBlockWithTextElement.cshtml"
               Write(a);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(34, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 3 "CodeBlockWithTextElement.cshtml"
    var b = 1;

#line default
#line hidden

            Instrumentation.BeginContext(60, 1, false);
#line 3 "CodeBlockWithTextElement.cshtml"
               Write(b);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(68, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 4 "CodeBlockWithTextElement.cshtml"

#line default
#line hidden

            Instrumentation.BeginContext(71, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
