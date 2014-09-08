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
            Write(
#line 1 "Inherits.cshtml"
 foo()

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(6, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
