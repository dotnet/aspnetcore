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
#line 1 "This is here only for document formatting."
__o = foo();

#line default
#line hidden
        }
    }
}
