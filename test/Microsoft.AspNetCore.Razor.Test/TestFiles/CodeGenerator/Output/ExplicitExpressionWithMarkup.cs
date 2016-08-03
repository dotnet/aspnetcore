#pragma checksum "ExplicitExpressionWithMarkup.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "1252c799cdeb86a71e4304f01ebaae540fa26894"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class ExplicitExpressionWithMarkup
    {
        #line hidden
        public ExplicitExpressionWithMarkup()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            Instrumentation.BeginContext(0, 5, true);
            WriteLiteral("<div>");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(14, 0, false);
#line 1 "ExplicitExpressionWithMarkup.cshtml"
        Write(item => new Template(async(__razor_template_writer) => {
    Instrumentation.BeginContext(8, 6, true);
    WriteLiteralTo(__razor_template_writer, "</div>");
    Instrumentation.EndContext();
}
)
);

#line default
#line hidden
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
