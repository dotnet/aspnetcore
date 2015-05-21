#pragma checksum "ImplicitExpression.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "77befd9645f3c2d9ab48b935faebf9f731f42abc"
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

            Instrumentation.BeginContext(33, 21, true);
            WriteLiteral("    <p>This is item #");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(55, 1, false);
#line 2 "ImplicitExpression.cshtml"
                Write(i);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(56, 6, true);
            WriteLiteral("</p>\r\n");
            Instrumentation.EndContext();
#line 3 "ImplicitExpression.cshtml"
}

#line default
#line hidden

        }
        #pragma warning restore 1998
    }
}
