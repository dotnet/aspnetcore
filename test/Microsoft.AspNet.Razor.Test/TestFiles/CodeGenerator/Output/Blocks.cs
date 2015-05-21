#pragma checksum "Blocks.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "ba7d8f5f5159a2389c780aa606885ef6c917a45a"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class Blocks
    {
        #line hidden
        public Blocks()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 1 "Blocks.cshtml"
  
    int i = 1;

#line default
#line hidden

            Instrumentation.BeginContext(21, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
#line 5 "Blocks.cshtml"
 while(i <= 10) {

#line default
#line hidden

            Instrumentation.BeginContext(44, 23, true);
            WriteLiteral("    <p>Hello from C#, #");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(69, 1, false);
#line 6 "Blocks.cshtml"
                   Write(i);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(71, 6, true);
            WriteLiteral("</p>\r\n");
            Instrumentation.EndContext();
#line 7 "Blocks.cshtml"
    i += 1;
}

#line default
#line hidden

            Instrumentation.BeginContext(93, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 10 "Blocks.cshtml"
 if(i == 11) {

#line default
#line hidden

            Instrumentation.BeginContext(111, 31, true);
            WriteLiteral("    <p>We wrote 10 lines!</p>\r\n");
            Instrumentation.EndContext();
#line 12 "Blocks.cshtml"
}

#line default
#line hidden

            Instrumentation.BeginContext(145, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 14 "Blocks.cshtml"
 switch(i) {
    case 11:

#line default
#line hidden

            Instrumentation.BeginContext(175, 46, true);
            WriteLiteral("        <p>No really, we wrote 10 lines!</p>\r\n");
            Instrumentation.EndContext();
#line 17 "Blocks.cshtml"
        break;
    default:

#line default
#line hidden

            Instrumentation.BeginContext(251, 39, true);
            WriteLiteral("        <p>Actually, we didn\'t...</p>\r\n");
            Instrumentation.EndContext();
#line 20 "Blocks.cshtml"
        break;
}

#line default
#line hidden

            Instrumentation.BeginContext(309, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 23 "Blocks.cshtml"
 for(int j = 1; j <= 10; j += 2) {

#line default
#line hidden

            Instrumentation.BeginContext(347, 29, true);
            WriteLiteral("    <p>Hello again from C#, #");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(378, 1, false);
#line 24 "Blocks.cshtml"
                         Write(j);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(380, 6, true);
            WriteLiteral("</p>\r\n");
            Instrumentation.EndContext();
#line 25 "Blocks.cshtml"
}

#line default
#line hidden

            Instrumentation.BeginContext(389, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 27 "Blocks.cshtml"
 try {

#line default
#line hidden

            Instrumentation.BeginContext(399, 41, true);
            WriteLiteral("    <p>That time, we wrote 5 lines!</p>\r\n");
            Instrumentation.EndContext();
#line 29 "Blocks.cshtml"
} catch(Exception ex) {

#line default
#line hidden

            Instrumentation.BeginContext(465, 33, true);
            WriteLiteral("    <p>Oh no! An error occurred: ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(500, 10, false);
#line 30 "Blocks.cshtml"
                             Write(ex.Message);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(511, 6, true);
            WriteLiteral("</p>\r\n");
            Instrumentation.EndContext();
#line 31 "Blocks.cshtml"
}

#line default
#line hidden

            Instrumentation.BeginContext(520, 14, true);
            WriteLiteral("\r\n<p>i is now ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(535, 1, false);
#line 33 "Blocks.cshtml"
       Write(i);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(536, 8, true);
            WriteLiteral("</p>\r\n\r\n");
            Instrumentation.EndContext();
#line 35 "Blocks.cshtml"
 lock(new object()) {

#line default
#line hidden

            Instrumentation.BeginContext(567, 53, true);
            WriteLiteral("    <p>This block is locked, for your security!</p>\r\n");
            Instrumentation.EndContext();
#line 37 "Blocks.cshtml"
}

#line default
#line hidden

        }
        #pragma warning restore 1998
    }
}
