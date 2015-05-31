#pragma checksum "CodeBlockWithTextElement.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "13e48ff59aab8106ceb68dd4a10b0bdf10c322fc"
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

            Instrumentation.BeginContext(25, 3, true);
            WriteLiteral("foo");
            Instrumentation.EndContext();
#line 2 "CodeBlockWithTextElement.cshtml"
                               		

#line default
#line hidden

            Instrumentation.BeginContext(38, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 3 "CodeBlockWithTextElement.cshtml"
    var b = 1;

#line default
#line hidden

            Instrumentation.BeginContext(63, 4, true);
            WriteLiteral("bar ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(69, 3, false);
#line 3 "CodeBlockWithTextElement.cshtml"
                    Write(a+b);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(80, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 4 "CodeBlockWithTextElement.cshtml"

#line default
#line hidden

            Instrumentation.BeginContext(83, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
