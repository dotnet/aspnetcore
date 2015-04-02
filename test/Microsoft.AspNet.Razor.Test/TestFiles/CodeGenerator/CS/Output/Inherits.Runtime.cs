#pragma checksum "Inherits.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "72d6e21c6366f99a17c63abebb46db3470f4d1da"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class Inherits : foo.bar<baz<biz>>.boz bar
    {
        #line hidden
        public Inherits()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            Instrumentation.BeginContext(1, 5, false);
#line 1 "Inherits.cshtml"
Write(foo());

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(6, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
