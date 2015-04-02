#pragma checksum "ExplicitExpression.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "a897a227b26c531d644bdff988df46d3c8178346"
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
#line 1 "ExplicitExpression.cshtml"
    Write(1+1);

#line default
#line hidden
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
