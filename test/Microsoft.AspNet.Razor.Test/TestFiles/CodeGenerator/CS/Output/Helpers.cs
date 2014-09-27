#pragma checksum "Helpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "228b0ea0de0f06806d10a9768bb4afd7e0ecb878"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class Helpers
    {
public static Template 
#line 1 "Helpers.cshtml"
Bold(string s) {

#line default
#line hidden
        return new Template((__razor_helper_writer) => {
#line 1 "Helpers.cshtml"
                        
    s = s.ToUpper();

#line default
#line hidden

            Instrumentation.BeginContext(48, 12, true);
            WriteLiteralTo(__razor_helper_writer, "    <strong>");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(61, 1, false);
            WriteTo(__razor_helper_writer, 
#line 3 "Helpers.cshtml"
             s

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(62, 11, true);
            WriteLiteralTo(__razor_helper_writer, "</strong>\r\n");
            Instrumentation.EndContext();
#line 4 "Helpers.cshtml"

#line default
#line hidden

        }
        );
#line 4 "Helpers.cshtml"
}

#line default
#line hidden

public static Template 
#line 6 "Helpers.cshtml"
Italic(string s) {

#line default
#line hidden
        return new Template((__razor_helper_writer) => {
#line 6 "Helpers.cshtml"
                          
    s = s.ToUpper();

#line default
#line hidden

            Instrumentation.BeginContext(128, 8, true);
            WriteLiteralTo(__razor_helper_writer, "    <em>");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(137, 1, false);
            WriteTo(__razor_helper_writer, 
#line 8 "Helpers.cshtml"
         s

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(138, 7, true);
            WriteLiteralTo(__razor_helper_writer, "</em>\r\n");
            Instrumentation.EndContext();
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
            Instrumentation.BeginContext(76, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(148, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(151, 13, false);
            Write(
#line 11 "Helpers.cshtml"
 Bold("Hello")

#line default
#line hidden
            );

            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
