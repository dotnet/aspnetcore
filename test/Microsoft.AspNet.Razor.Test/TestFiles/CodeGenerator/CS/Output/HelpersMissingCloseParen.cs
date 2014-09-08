namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class HelpersMissingCloseParen
    {
public static Template 
#line 1 "HelpersMissingCloseParen.cshtml"
Bold(string s) {

#line default
#line hidden
        return new Template((__razor_helper_writer) => {
#line 1 "HelpersMissingCloseParen.cshtml"
                        
    s = s.ToUpper();

#line default
#line hidden

            Instrumentation.BeginContext(48, 12, true);
            WriteLiteralTo(__razor_helper_writer, "    <strong>");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(61, 1, false);
            WriteTo(__razor_helper_writer, 
#line 3 "HelpersMissingCloseParen.cshtml"
             s

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(62, 11, true);
            WriteLiteralTo(__razor_helper_writer, "</strong>\r\n");
            Instrumentation.EndContext();
#line 4 "HelpersMissingCloseParen.cshtml"

#line default
#line hidden

        }
        );
#line 4 "HelpersMissingCloseParen.cshtml"
}

#line default
#line hidden

public static Template 
#line 6 "HelpersMissingCloseParen.cshtml"
Italic(string s
@Bold("Hello")

#line default
#line hidden

        #line hidden
        public HelpersMissingCloseParen()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            Instrumentation.BeginContext(76, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
