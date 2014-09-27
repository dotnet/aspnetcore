#pragma checksum "ExpressionsInCode.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "3ccb5b16f61b84dd82d7402e4a17870a39d09ca9"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class ExpressionsInCode
    {
        #line hidden
        public ExpressionsInCode()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 1 "ExpressionsInCode.cshtml"
  
    object foo = null;
    string bar = "Foo";

#line default
#line hidden

            Instrumentation.BeginContext(54, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
#line 6 "ExpressionsInCode.cshtml"
 if(foo != null) {
    

#line default
#line hidden

            Instrumentation.BeginContext(83, 3, false);
            Write(
#line 7 "ExpressionsInCode.cshtml"
     foo

#line default
#line hidden
            );

            Instrumentation.EndContext();
#line 7 "ExpressionsInCode.cshtml"
        
} else {

#line default
#line hidden

            Instrumentation.BeginContext(98, 25, true);
            WriteLiteral("    <p>Foo is Null!</p>\r\n");
            Instrumentation.EndContext();
#line 10 "ExpressionsInCode.cshtml"
}

#line default
#line hidden

            Instrumentation.BeginContext(126, 7, true);
            WriteLiteral("\r\n<p>\r\n");
            Instrumentation.EndContext();
#line 13 "ExpressionsInCode.cshtml"
 if(!String.IsNullOrEmpty(bar)) {
    

#line default
#line hidden

            Instrumentation.BeginContext(174, 21, false);
            Write(
#line 14 "ExpressionsInCode.cshtml"
      bar.Replace("F", "B")

#line default
#line hidden
            );

            Instrumentation.EndContext();
#line 14 "ExpressionsInCode.cshtml"
                            
}

#line default
#line hidden

            Instrumentation.BeginContext(201, 4, true);
            WriteLiteral("</p>");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
