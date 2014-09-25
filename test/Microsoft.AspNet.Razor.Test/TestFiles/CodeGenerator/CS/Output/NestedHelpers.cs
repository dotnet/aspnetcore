#pragma checksum "NestedHelpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "e8232b2b30af7dadb0698abf8ba08851f401963d"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

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

            Instrumentation.BeginContext(142, 8, true);
            WriteLiteralTo(__razor_helper_writer, "    <em>");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(151, 7, false);
#line 7 "NestedHelpers.cshtml"
WriteTo(__razor_helper_writer, Bold(s));

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(158, 7, true);
            WriteLiteralTo(__razor_helper_writer, "</em>\r\n");
            Instrumentation.EndContext();
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

            Instrumentation.BeginContext(106, 16, true);
            WriteLiteralTo(__razor_helper_writer, "        <strong>");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(123, 1, false);
#line 5 "NestedHelpers.cshtml"
WriteTo(__razor_helper_writer, s);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(124, 11, true);
            WriteLiteralTo(__razor_helper_writer, "</strong>\r\n");
            Instrumentation.EndContext();
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

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            Instrumentation.BeginContext(168, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(171, 15, false);
#line 10 "NestedHelpers.cshtml"
Write(Italic("Hello"));

#line default
#line hidden
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
