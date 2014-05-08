namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class ImplicitExpression
    {
        #line hidden
        public ImplicitExpression()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 1 "ImplicitExpression.cshtml"
 for(int i = 1; i <= 10; i++) {

#line default
#line hidden

            WriteLiteral("    <p>This is item #");
            Write(
#line 2 "ImplicitExpression.cshtml"
                      i

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n");
#line 3 "ImplicitExpression.cshtml"
}

#line default
#line hidden

        }
        #pragma warning restore 1998
    }
}
