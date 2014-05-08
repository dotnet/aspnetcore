namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class RazorComments
    {
        #line hidden
        public RazorComments()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            WriteLiteral("\r\n<p>This should  be shown</p>\r\n\r\n");
#line 4 "RazorComments.cshtml"
  
    

#line default
#line hidden

#line 5 "RazorComments.cshtml"
                                       
    Exception foo = 

#line default
#line hidden

#line 6 "RazorComments.cshtml"
                                                  null;
    if(foo != null) {
        throw foo;
    }

#line default
#line hidden

            WriteLiteral("\r\n\r\n");
#line 12 "RazorComments.cshtml"
   var bar = "@* bar *@"; 

#line default
#line hidden

            WriteLiteral("\r\n<p>But this should show the comment syntax: ");
            Write(
#line 13 "RazorComments.cshtml"
                                             bar

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n\r\n");
            Write(
#line 15 "RazorComments.cshtml"
  a

#line default
#line hidden
#line 15 "RazorComments.cshtml"
       b

#line default
#line hidden
            );

            WriteLiteral("\r\n");
        }
        #pragma warning restore 1998
    }
}
