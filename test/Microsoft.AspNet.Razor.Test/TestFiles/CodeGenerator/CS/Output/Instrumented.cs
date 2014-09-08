namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class Instrumented
    {
public static Template 
#line 1 "Instrumented.cshtml"
Strong(string s) {

#line default
#line hidden
        return new Template((__razor_helper_writer) => {
#line 1 "Instrumented.cshtml"
                          

#line default
#line hidden

            Instrumentation.BeginContext(28, 12, true);
            WriteLiteralTo(__razor_helper_writer, "    <strong>");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(41, 1, false);
            WriteTo(__razor_helper_writer, 
#line 2 "Instrumented.cshtml"
             s

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(42, 11, true);
            WriteLiteralTo(__razor_helper_writer, "</strong>\r\n");
            Instrumentation.EndContext();
#line 3 "Instrumented.cshtml"

#line default
#line hidden

        }
        );
#line 3 "Instrumented.cshtml"
}

#line default
#line hidden

        #line hidden
        public Instrumented()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            Instrumentation.BeginContext(56, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 5 "Instrumented.cshtml"
  
    int i = 1;
    var foo = 

#line default
#line hidden

            item => new Template((__razor_template_writer) => {
                Instrumentation.BeginContext(93, 10, true);
                WriteLiteralTo(__razor_template_writer, "<p>Bar</p>");
                Instrumentation.EndContext();
            }
            )
#line 7 "Instrumented.cshtml"
                         ;

#line default
#line hidden

            Instrumentation.BeginContext(106, 43, true);
            WriteLiteral("    Hello, World\r\n    <p>Hello, World</p>\r\n");
            Instrumentation.EndContext();
#line 10 "Instrumented.cshtml"

#line default
#line hidden

            Instrumentation.BeginContext(152, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
#line 12 "Instrumented.cshtml"
 while(i <= 10) {

#line default
#line hidden

            Instrumentation.BeginContext(175, 23, true);
            WriteLiteral("    <p>Hello from C#, #");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(200, 1, false);
            Write(
#line 13 "Instrumented.cshtml"
                         i

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(202, 6, true);
            WriteLiteral("</p>\r\n");
            Instrumentation.EndContext();
#line 14 "Instrumented.cshtml"
    i += 1;
}

#line default
#line hidden

            Instrumentation.BeginContext(224, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 17 "Instrumented.cshtml"
 if(i == 11) {

#line default
#line hidden

            Instrumentation.BeginContext(242, 31, true);
            WriteLiteral("    <p>We wrote 10 lines!</p>\r\n");
            Instrumentation.EndContext();
#line 19 "Instrumented.cshtml"
}

#line default
#line hidden

            Instrumentation.BeginContext(276, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 21 "Instrumented.cshtml"
 switch(i) {
    case 11:

#line default
#line hidden

            Instrumentation.BeginContext(306, 46, true);
            WriteLiteral("        <p>No really, we wrote 10 lines!</p>\r\n");
            Instrumentation.EndContext();
#line 24 "Instrumented.cshtml"
        break;
    default:

#line default
#line hidden

            Instrumentation.BeginContext(382, 39, true);
            WriteLiteral("        <p>Actually, we didn\'t...</p>\r\n");
            Instrumentation.EndContext();
#line 27 "Instrumented.cshtml"
        break;
}

#line default
#line hidden

            Instrumentation.BeginContext(440, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 30 "Instrumented.cshtml"
 for(int j = 1; j <= 10; j += 2) {

#line default
#line hidden

            Instrumentation.BeginContext(478, 29, true);
            WriteLiteral("    <p>Hello again from C#, #");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(509, 1, false);
            Write(
#line 31 "Instrumented.cshtml"
                               j

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(511, 6, true);
            WriteLiteral("</p>\r\n");
            Instrumentation.EndContext();
#line 32 "Instrumented.cshtml"
}

#line default
#line hidden

            Instrumentation.BeginContext(520, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 34 "Instrumented.cshtml"
 try {

#line default
#line hidden

            Instrumentation.BeginContext(530, 41, true);
            WriteLiteral("    <p>That time, we wrote 5 lines!</p>\r\n");
            Instrumentation.EndContext();
#line 36 "Instrumented.cshtml"
} catch(Exception ex) {

#line default
#line hidden

            Instrumentation.BeginContext(596, 33, true);
            WriteLiteral("    <p>Oh no! An error occurred: ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(631, 10, false);
            Write(
#line 37 "Instrumented.cshtml"
                                   ex.Message

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(642, 6, true);
            WriteLiteral("</p>\r\n");
            Instrumentation.EndContext();
#line 38 "Instrumented.cshtml"
}

#line default
#line hidden

            Instrumentation.BeginContext(651, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 40 "Instrumented.cshtml"
 lock(new object()) {

#line default
#line hidden

            Instrumentation.BeginContext(676, 53, true);
            WriteLiteral("    <p>This block is locked, for your security!</p>\r\n");
            Instrumentation.EndContext();
#line 42 "Instrumented.cshtml"
}

#line default
#line hidden

        }
        #pragma warning restore 1998
    }
}
