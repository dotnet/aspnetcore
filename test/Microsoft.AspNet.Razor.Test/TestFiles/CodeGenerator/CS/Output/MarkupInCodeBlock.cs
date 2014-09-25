#pragma checksum "MarkupInCodeBlock.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "539d5763b7091d29df375085f623b9086e7f8ad6"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class MarkupInCodeBlock
    {
        #line hidden
        public MarkupInCodeBlock()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 1 "MarkupInCodeBlock.cshtml"
  
    for(int i = 1; i <= 10; i++) {

#line default
#line hidden

            Instrumentation.BeginContext(40, 27, true);
            WriteLiteral("        <p>Hello from C#, #");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(69, 12, false);
#line 3 "MarkupInCodeBlock.cshtml"
                       Write(i.ToString());

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(82, 6, true);
            WriteLiteral("</p>\r\n");
            Instrumentation.EndContext();
#line 4 "MarkupInCodeBlock.cshtml"
    }

#line default
#line hidden

            Instrumentation.BeginContext(96, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
