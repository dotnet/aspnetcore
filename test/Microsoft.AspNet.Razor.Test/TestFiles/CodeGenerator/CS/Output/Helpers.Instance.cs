namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class Helpers
    {
public  Template 
#line 1 "Helpers.cshtml"
Bold(string s) {

#line default
#line hidden
        return new Template((__razor_helper_writer) => {
#line 1 "Helpers.cshtml"
                        
    s = s.ToUpper();

#line default
#line hidden

            WriteLiteralTo(__razor_helper_writer, "    <strong>");
            WriteTo(__razor_helper_writer, 
#line 3 "Helpers.cshtml"
             s

#line default
#line hidden
            );

            WriteLiteralTo(__razor_helper_writer, "</strong>\r\n");
#line 4 "Helpers.cshtml"

#line default
#line hidden

        }
        );
#line 4 "Helpers.cshtml"
}

#line default
#line hidden

public  Template 
#line 6 "Helpers.cshtml"
Italic(string s) {

#line default
#line hidden
        return new Template((__razor_helper_writer) => {
#line 6 "Helpers.cshtml"
                          
    s = s.ToUpper();

#line default
#line hidden

            WriteLiteralTo(__razor_helper_writer, "    <em>");
            WriteTo(__razor_helper_writer, 
#line 8 "Helpers.cshtml"
         s

#line default
#line hidden
            );

            WriteLiteralTo(__razor_helper_writer, "</em>\r\n");
#line 9 "Helpers.cshtml"

#line default
#line hidden

        }
        );
#line 9 "Helpers.cshtml"
}

#line default
#line hidden

        #line hidden
        public Helpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            WriteLiteral("\r\n");
            WriteLiteral("\r\n");
            Write(
#line 11 "Helpers.cshtml"
 Bold("Hello")

#line default
#line hidden
            );

        }
        #pragma warning restore 1998
    }
}
