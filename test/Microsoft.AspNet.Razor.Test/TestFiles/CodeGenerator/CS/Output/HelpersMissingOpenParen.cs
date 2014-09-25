#pragma checksum "HelpersMissingOpenParen.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "dc407d9349ea9a1595c65660d41a63970de65729"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

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

            Instrumentation.BeginContext(48, 12, true);
            WriteLiteralTo(__razor_helper_writer, "    <strong>");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(61, 1, false);
#line 3 "HelpersMissingOpenParen.cshtml"
WriteTo(__razor_helper_writer, s);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(62, 11, true);
            WriteLiteralTo(__razor_helper_writer, "</strong>\r\n");
            Instrumentation.EndContext();
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

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            Instrumentation.BeginContext(76, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(95, 13, false);
#line 7 "HelpersMissingOpenParen.cshtml"
Write(Bold("Hello"));

#line default
#line hidden
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
