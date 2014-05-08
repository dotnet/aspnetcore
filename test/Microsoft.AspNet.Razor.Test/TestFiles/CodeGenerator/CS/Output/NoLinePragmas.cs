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

            WriteLiteral("\r\n\r\n");
#line 5 ""
 while(i <= 10) {

#line default
#line hidden

            WriteLiteral("    <p>Hello from C#, #");
            Write(
#line 6 ""
                         i

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n");
#line 7 ""
    i += 1;
}

#line default
#line hidden

            WriteLiteral("\r\n");
#line 10 ""
 if(i == 11) {

#line default
#line hidden

            WriteLiteral("    <p>We wrote 10 lines!</p>\r\n");
#line 12 ""
}

#line default
#line hidden

            WriteLiteral("\r\n");
#line 14 ""
 switch(i) {
    case 11:

#line default
#line hidden

            WriteLiteral("        <p>No really, we wrote 10 lines!</p>\r\n");
#line 17 ""
        break;
    default:

#line default
#line hidden

            WriteLiteral("        <p>Actually, we didn\'t...</p>\r\n");
#line 20 ""
        break;
}

#line default
#line hidden

            WriteLiteral("\r\n");
#line 23 ""
 for(int j = 1; j <= 10; j += 2) {

#line default
#line hidden

            WriteLiteral("    <p>Hello again from C#, #");
            Write(
#line 24 ""
                               j

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n");
#line 25 ""
}

#line default
#line hidden

            WriteLiteral("\r\n");
#line 27 ""
 try {

#line default
#line hidden

            WriteLiteral("    <p>That time, we wrote 5 lines!</p>\r\n");
#line 29 ""
} catch(Exception ex) {

#line default
#line hidden

            WriteLiteral("    <p>Oh no! An error occurred: ");
            Write(
#line 30 ""
                                   ex.Message

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n");
#line 31 ""
}


#line default
#line hidden

#line 33 ""
                                  

#line default
#line hidden

            WriteLiteral("<p>i is now ");
            Write(
#line 34 ""
             i

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n\r\n");
#line 36 ""
 lock(new object()) {

#line default
#line hidden

            WriteLiteral("    <p>This block is locked, for your security!</p>\r\n");
#line 38 ""
}

#line default
#line hidden

        }
        #pragma warning restore 1998
    }
}
