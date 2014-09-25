#pragma checksum "HelpersMissingOpenBrace.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "f81212c85e39fa08cb4c95e2339817caa725397c"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class HelpersMissingOpenBrace
    {
public static Template 
#line 1 "HelpersMissingOpenBrace.cshtml"
Bold(string s) {

#line default
#line hidden
        return new Template((__razor_helper_writer) => {
#line 1 "HelpersMissingOpenBrace.cshtml"
                        
    s = s.ToUpper();

#line default
#line hidden

            Instrumentation.BeginContext(48, 12, true);
            WriteLiteralTo(__razor_helper_writer, "    <strong>");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(61, 1, false);
#line 3 "HelpersMissingOpenBrace.cshtml"
WriteTo(__razor_helper_writer, s);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(62, 11, true);
            WriteLiteralTo(__razor_helper_writer, "</strong>\r\n");
            Instrumentation.EndContext();
#line 4 "HelpersMissingOpenBrace.cshtml"

#line default
#line hidden

        }
        );
#line 4 "HelpersMissingOpenBrace.cshtml"
}

#line default
#line hidden

public static Template 
#line 6 "HelpersMissingOpenBrace.cshtml"
Italic(string s) 

#line default
#line hidden

        #line hidden
        public HelpersMissingOpenBrace()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            Instrumentation.BeginContext(76, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(106, 9, false);
#line 7 "HelpersMissingOpenBrace.cshtml"
Write(Italic(s));

#line default
#line hidden
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
