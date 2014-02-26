namespace TestOutput
{
    using System;

    public class ExplicitExpression
    {
        #line hidden
        public ExplicitExpression()
        {
        }

        public override void Execute()
        {
            WriteLiteral("1 + 1 = ");
            Write(
#line 1 "ExplicitExpression.cshtml"
          1+1

#line default
#line hidden
            );

        }
    }
}
