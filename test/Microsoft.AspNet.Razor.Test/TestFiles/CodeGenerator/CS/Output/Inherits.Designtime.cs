namespace TestOutput
{
    using System;

    public class Inherits : foo.bar<baz<biz>>.boz bar
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
#line 3 "Inherits.cshtml"
          foo.bar<baz<biz>>.boz bar __inheritsHelper = null;
#line default
#line hidden
            #pragma warning restore 219
        }
        #line hidden
        public Inherits()
        {
        }

        public override void Execute()
        {
            __o = 
#line 1 "Inherits.cshtml"
 foo()
#line default
#line hidden
            ;
        }
    }
}
