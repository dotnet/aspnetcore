namespace TestOutput
{
    using System;

    public class Inherits : foo.bar<baz<biz>>.boz bar
    {
        #line hidden
        public Inherits()
        {
        }

        public override void Execute()
        {
            Write(
#line 1 "Inherits.cshtml"
 foo()

#line default
#line hidden
            );

            WriteLiteral("\r\n\r\n");
        }
    }
}
