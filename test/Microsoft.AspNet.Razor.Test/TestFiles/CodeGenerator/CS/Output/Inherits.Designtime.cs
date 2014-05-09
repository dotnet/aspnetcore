namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

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

        public override async Task ExecuteAsync()
        {
#line 1 "Inherits.cshtml"
__o = foo();

#line default
#line hidden
        }
    }
}
