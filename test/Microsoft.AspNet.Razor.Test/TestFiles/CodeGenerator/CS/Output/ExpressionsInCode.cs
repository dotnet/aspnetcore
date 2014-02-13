namespace TestOutput
{
    using System;

    public class ExpressionsInCode
    {
        #line hidden
        public ExpressionsInCode()
        {
        }

        public override void Execute()
        {
#line 1 "ExpressionsInCode.cshtml"
  
    object foo = null;
    string bar = "Foo";
#line default
#line hidden

            WriteLiteral("\r\n\r\n");
#line 6 "ExpressionsInCode.cshtml"
 if(foo != null) {
    
#line default
#line hidden

            Write(
#line 7 "ExpressionsInCode.cshtml"
     foo
#line default
#line hidden
            );
#line 7 "ExpressionsInCode.cshtml"
        
} else {
#line default
#line hidden

            WriteLiteral("    <p>Foo is Null!</p>\r\n");
#line 10 "ExpressionsInCode.cshtml"
}
#line default
#line hidden

            WriteLiteral("\r\n<p>\r\n");
#line 13 "ExpressionsInCode.cshtml"
 if(!String.IsNullOrEmpty(bar)) {
    
#line default
#line hidden

            Write(
#line 14 "ExpressionsInCode.cshtml"
      bar.Replace("F", "B")
#line default
#line hidden
            );
#line 14 "ExpressionsInCode.cshtml"
                            
}
#line default
#line hidden

            WriteLiteral("</p>");
        }
    }
}
