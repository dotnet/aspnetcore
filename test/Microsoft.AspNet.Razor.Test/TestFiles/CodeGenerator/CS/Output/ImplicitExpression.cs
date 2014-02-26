namespace TestOutput
{
    using System;

    public class ImplicitExpression
    {
        #line hidden
        public ImplicitExpression()
        {
        }

        public override void Execute()
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
    }
}
