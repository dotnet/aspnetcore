#pragma checksum "" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "c6fa3992fa56644768995c97941d682d90f6d8ec"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class NoLinePragmas
    {
        #line hidden
        public NoLinePragmas()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 1 ""
  
    int i = 1;

#line default
#line hidden

            Instrumentation.BeginContext(21, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
#line 5 ""
 while(i <= 10) {

#line default
#line hidden

            Instrumentation.BeginContext(44, 23, true);
            WriteLiteral("    <p>Hello from C#, #");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(69, 1, false);
            Write(
#line 6 ""
                         i

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(71, 6, true);
            WriteLiteral("</p>\r\n");
            Instrumentation.EndContext();
#line 7 ""
    i += 1;
}

#line default
#line hidden

            Instrumentation.BeginContext(93, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 10 ""
 if(i == 11) {

#line default
#line hidden

            Instrumentation.BeginContext(111, 31, true);
            WriteLiteral("    <p>We wrote 10 lines!</p>\r\n");
            Instrumentation.EndContext();
#line 12 ""
}

#line default
#line hidden

            Instrumentation.BeginContext(145, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 14 ""
 switch(i) {
    case 11:

#line default
#line hidden

            Instrumentation.BeginContext(175, 46, true);
            WriteLiteral("        <p>No really, we wrote 10 lines!</p>\r\n");
            Instrumentation.EndContext();
#line 17 ""
        break;
    default:

#line default
#line hidden

            Instrumentation.BeginContext(251, 39, true);
            WriteLiteral("        <p>Actually, we didn\'t...</p>\r\n");
            Instrumentation.EndContext();
#line 20 ""
        break;
}

#line default
#line hidden

            Instrumentation.BeginContext(309, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 23 ""
 for(int j = 1; j <= 10; j += 2) {

#line default
#line hidden

            Instrumentation.BeginContext(347, 29, true);
            WriteLiteral("    <p>Hello again from C#, #");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(378, 1, false);
            Write(
#line 24 ""
                               j

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(380, 6, true);
            WriteLiteral("</p>\r\n");
            Instrumentation.EndContext();
#line 25 ""
}

#line default
#line hidden

            Instrumentation.BeginContext(389, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 27 ""
 try {

#line default
#line hidden

            Instrumentation.BeginContext(399, 41, true);
            WriteLiteral("    <p>That time, we wrote 5 lines!</p>\r\n");
            Instrumentation.EndContext();
#line 29 ""
} catch(Exception ex) {

#line default
#line hidden

            Instrumentation.BeginContext(465, 33, true);
            WriteLiteral("    <p>Oh no! An error occurred: ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(500, 10, false);
            Write(
#line 30 ""
                                   ex.Message

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(511, 6, true);
            WriteLiteral("</p>\r\n");
            Instrumentation.EndContext();
#line 31 ""
}


#line default
#line hidden

#line 33 ""
                                  

#line default
#line hidden

            Instrumentation.BeginContext(558, 12, true);
            WriteLiteral("<p>i is now ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(571, 1, false);
            Write(
#line 34 ""
             i

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(572, 8, true);
            WriteLiteral("</p>\r\n\r\n");
            Instrumentation.EndContext();
#line 36 ""
 lock(new object()) {

#line default
#line hidden

            Instrumentation.BeginContext(603, 53, true);
            WriteLiteral("    <p>This block is locked, for your security!</p>\r\n");
            Instrumentation.EndContext();
#line 38 ""
}

#line default
#line hidden

        }
        #pragma warning restore 1998
    }
}
