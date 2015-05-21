#pragma checksum "Instrumented.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "9b521264e3e64710635c0f0490a368845d90da66"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class Instrumented
    {
        #line hidden
        public Instrumented()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 1 "Instrumented.cshtml"
  
    int i = 1;
    var foo = 

#line default
#line hidden

            item => new Template((__razor_template_writer) => {
                Instrumentation.BeginContext(35, 10, true);
                WriteLiteralTo(__razor_template_writer, "<p>Bar</p>");
                Instrumentation.EndContext();
            }
            )
#line 3 "Instrumented.cshtml"
                         ;

#line default
#line hidden

            Instrumentation.BeginContext(48, 43, true);
            WriteLiteral("    Hello, World\r\n    <p>Hello, World</p>\r\n");
            Instrumentation.EndContext();
#line 6 "Instrumented.cshtml"

#line default
#line hidden

            Instrumentation.BeginContext(94, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
#line 8 "Instrumented.cshtml"
 while(i <= 10) {

#line default
#line hidden

            Instrumentation.BeginContext(117, 23, true);
            WriteLiteral("    <p>Hello from C#, #");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(142, 1, false);
#line 9 "Instrumented.cshtml"
                   Write(i);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(144, 6, true);
            WriteLiteral("</p>\r\n");
            Instrumentation.EndContext();
#line 10 "Instrumented.cshtml"
    i += 1;
}

#line default
#line hidden

            Instrumentation.BeginContext(166, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 13 "Instrumented.cshtml"
 if(i == 11) {

#line default
#line hidden

            Instrumentation.BeginContext(184, 31, true);
            WriteLiteral("    <p>We wrote 10 lines!</p>\r\n");
            Instrumentation.EndContext();
#line 15 "Instrumented.cshtml"
}

#line default
#line hidden

            Instrumentation.BeginContext(218, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 17 "Instrumented.cshtml"
 switch(i) {
    case 11:

#line default
#line hidden

            Instrumentation.BeginContext(248, 46, true);
            WriteLiteral("        <p>No really, we wrote 10 lines!</p>\r\n");
            Instrumentation.EndContext();
#line 20 "Instrumented.cshtml"
        break;
    default:

#line default
#line hidden

            Instrumentation.BeginContext(324, 39, true);
            WriteLiteral("        <p>Actually, we didn\'t...</p>\r\n");
            Instrumentation.EndContext();
#line 23 "Instrumented.cshtml"
        break;
}

#line default
#line hidden

            Instrumentation.BeginContext(382, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 26 "Instrumented.cshtml"
 for(int j = 1; j <= 10; j += 2) {

#line default
#line hidden

            Instrumentation.BeginContext(420, 29, true);
            WriteLiteral("    <p>Hello again from C#, #");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(451, 1, false);
#line 27 "Instrumented.cshtml"
                         Write(j);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(453, 6, true);
            WriteLiteral("</p>\r\n");
            Instrumentation.EndContext();
#line 28 "Instrumented.cshtml"
}

#line default
#line hidden

            Instrumentation.BeginContext(462, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 30 "Instrumented.cshtml"
 try {

#line default
#line hidden

            Instrumentation.BeginContext(472, 41, true);
            WriteLiteral("    <p>That time, we wrote 5 lines!</p>\r\n");
            Instrumentation.EndContext();
#line 32 "Instrumented.cshtml"
} catch(Exception ex) {

#line default
#line hidden

            Instrumentation.BeginContext(538, 33, true);
            WriteLiteral("    <p>Oh no! An error occurred: ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(573, 10, false);
#line 33 "Instrumented.cshtml"
                             Write(ex.Message);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(584, 6, true);
            WriteLiteral("</p>\r\n");
            Instrumentation.EndContext();
#line 34 "Instrumented.cshtml"
}

#line default
#line hidden

            Instrumentation.BeginContext(593, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 36 "Instrumented.cshtml"
 lock(new object()) {

#line default
#line hidden

            Instrumentation.BeginContext(618, 53, true);
            WriteLiteral("    <p>This block is locked, for your security!</p>\r\n");
            Instrumentation.EndContext();
#line 38 "Instrumented.cshtml"
}

#line default
#line hidden

        }
        #pragma warning restore 1998
    }
}
