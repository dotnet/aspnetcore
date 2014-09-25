#pragma checksum "Inherits.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "b9264c58289dbea68e46a818fd0c4c4d835b3a84"
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
