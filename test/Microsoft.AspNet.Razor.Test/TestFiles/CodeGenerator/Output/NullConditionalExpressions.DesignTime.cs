namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class NullConditionalExpressions
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            #pragma warning restore 219
        }
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

#line 2 "NullConditionalExpressions.cshtml"
__o = ViewBag?.Data;

#line default
#line hidden
#line 2 "NullConditionalExpressions.cshtml"
                  
    

#line default
#line hidden

#line 3 "NullConditionalExpressions.cshtml"
__o = ViewBag.IntIndexer?[0];

#line default
#line hidden
#line 3 "NullConditionalExpressions.cshtml"
                           
    

#line default
#line hidden

#line 4 "NullConditionalExpressions.cshtml"
__o = ViewBag.StrIndexer?["key"];

#line default
#line hidden
#line 4 "NullConditionalExpressions.cshtml"
                               
    

#line default
#line hidden

#line 5 "NullConditionalExpressions.cshtml"
__o = ViewBag?.Method(Value?[23]?.More)?["key"];

#line default
#line hidden
#line 5 "NullConditionalExpressions.cshtml"
                                              

#line default
#line hidden

#line 8 "NullConditionalExpressions.cshtml"
__o = ViewBag?.Data;

#line default
#line hidden
#line 9 "NullConditionalExpressions.cshtml"
__o = ViewBag.IntIndexer?[0];

#line default
#line hidden
#line 10 "NullConditionalExpressions.cshtml"
__o = ViewBag.StrIndexer?["key"];

#line default
#line hidden
#line 11 "NullConditionalExpressions.cshtml"
__o = ViewBag?.Method(Value?[23]?.More)?["key"];

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
