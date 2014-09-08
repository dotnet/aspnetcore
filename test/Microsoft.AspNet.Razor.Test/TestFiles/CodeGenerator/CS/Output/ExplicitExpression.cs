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

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            Instrumentation.BeginContext(0, 8, true);
            WriteLiteral("1 + 1 = ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(10, 3, false);
            Write(
#line 1 "ExplicitExpression.cshtml"
          1+1

#line default
#line hidden
            );

            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
