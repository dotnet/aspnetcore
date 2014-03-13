namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class ExplicitExpression
    {
        #line hidden
        public ExplicitExpression()
        {
        }

        public override async Task ExecuteAsync()
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
