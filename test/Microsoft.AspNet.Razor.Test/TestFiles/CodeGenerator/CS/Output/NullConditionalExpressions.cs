#pragma checksum "NullConditionalExpressions.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "c8c4f34e0768aea12ef6ce8e3fe0e384ad023faf"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class NullConditionalExpressions
    {
        #line hidden
        public NullConditionalExpressions()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 1 "NullConditionalExpressions.cshtml"
  
    

#line default
#line hidden

            Instrumentation.BeginContext(9, 13, false);
#line 2 "NullConditionalExpressions.cshtml"
Write(ViewBag?.Data);

#line default
#line hidden
            Instrumentation.EndContext();
#line 2 "NullConditionalExpressions.cshtml"
                  
    

#line default
#line hidden

            Instrumentation.BeginContext(29, 22, false);
#line 3 "NullConditionalExpressions.cshtml"
Write(ViewBag.IntIndexer?[0]);

#line default
#line hidden
            Instrumentation.EndContext();
#line 3 "NullConditionalExpressions.cshtml"
                           
    

#line default
#line hidden

            Instrumentation.BeginContext(58, 26, false);
#line 4 "NullConditionalExpressions.cshtml"
Write(ViewBag.StrIndexer?["key"]);

#line default
#line hidden
            Instrumentation.EndContext();
#line 4 "NullConditionalExpressions.cshtml"
                               
    

#line default
#line hidden

            Instrumentation.BeginContext(91, 41, false);
#line 5 "NullConditionalExpressions.cshtml"
Write(ViewBag?.Method(Value?[23]?.More)?["key"]);

#line default
#line hidden
            Instrumentation.EndContext();
#line 5 "NullConditionalExpressions.cshtml"
                                              

#line default
#line hidden

            Instrumentation.BeginContext(135, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(140, 13, false);
#line 8 "NullConditionalExpressions.cshtml"
Write(ViewBag?.Data);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(153, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(156, 22, false);
#line 9 "NullConditionalExpressions.cshtml"
Write(ViewBag.IntIndexer?[0]);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(178, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(181, 26, false);
#line 10 "NullConditionalExpressions.cshtml"
Write(ViewBag.StrIndexer?["key"]);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(207, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(210, 41, false);
#line 11 "NullConditionalExpressions.cshtml"
Write(ViewBag?.Method(Value?[23]?.More)?["key"]);

#line default
#line hidden
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
