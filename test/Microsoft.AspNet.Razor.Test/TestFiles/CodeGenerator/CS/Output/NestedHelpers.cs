namespace TestOutput
{
    using System;

    public class NestedHelpers
    {
public static Template 
#line 1 "NestedHelpers.cshtml"
Italic(string s) {
#line default
#line hidden
        return new Template((__razor_helper_writer) => {
#line 1 "NestedHelpers.cshtml"
                          
    s = s.ToUpper();
    
#line default
#line hidden

#line 6 "NestedHelpers.cshtml"
     
#line default
#line hidden

            WriteLiteralTo(__razor_helper_writer, "    <em>");
            WriteTo(__razor_helper_writer, 
#line 7 "NestedHelpers.cshtml"
         Bold(s)
#line default
#line hidden
            );
            WriteLiteralTo(__razor_helper_writer, "</em>\r\n");
#line 8 "NestedHelpers.cshtml"
#line default
#line hidden

        }
        );
#line 8 "NestedHelpers.cshtml"
}
#line default
#line hidden

public static Template 
#line 3 "NestedHelpers.cshtml"
Bold(string s) {
#line default
#line hidden
        return new Template((__razor_helper_writer) => {
#line 3 "NestedHelpers.cshtml"
                            
        s = s.ToUpper();
#line default
#line hidden

            WriteLiteralTo(__razor_helper_writer, "        <strong>");
            WriteTo(__razor_helper_writer, 
#line 5 "NestedHelpers.cshtml"
                 s
#line default
#line hidden
            );
            WriteLiteralTo(__razor_helper_writer, "</strong>\r\n");
#line 6 "NestedHelpers.cshtml"
    
#line default
#line hidden

        }
        );
#line 6 "NestedHelpers.cshtml"
}
#line default
#line hidden

        #line hidden
        public NestedHelpers()
        {
        }

        public override void Execute()
        {
            WriteLiteral("\r\n");
            Write(
#line 10 "NestedHelpers.cshtml"
 Italic("Hello")
#line default
#line hidden
            );
        }
    }
}
