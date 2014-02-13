namespace TestOutput
{
    using System;

    public class HelpersMissingOpenParen
    {
        public static Template 
#line 1 "HelpersMissingOpenParen.cshtml"
Bold(string s) {
#line default
#line hidden
        return new Template((__razor_helper_writer) => {
#line 1 "HelpersMissingOpenParen.cshtml"
                        
    s = s.ToUpper();
#line default
#line hidden

            WriteLiteralTo(__razor_helper_writer, "    <strong>");
            WriteTo(__razor_helper_writer, 
#line 3 "HelpersMissingOpenParen.cshtml"
             s
#line default
#line hidden
            );
            WriteLiteralTo(__razor_helper_writer, "</strong>\r\n");
#line 4 "HelpersMissingOpenParen.cshtml"
#line default
#line hidden

        }
        );
#line 4 "HelpersMissingOpenParen.cshtml"
}
#line default
#line hidden

        public static Template 
#line 6 "HelpersMissingOpenParen.cshtml"
Italic
#line default
#line hidden

        #line hidden
        public HelpersMissingOpenParen()
        {
        }

        public override void Execute()
        {
            WriteLiteral("\r\n");
            Write(
#line 7 "HelpersMissingOpenParen.cshtml"
 Bold("Hello")
#line default
#line hidden
            );
        }
    }
}
