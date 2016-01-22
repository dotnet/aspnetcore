namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class SingleTagHelper
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            string __tagHelperDirectiveSyntaxHelper = null;
            __tagHelperDirectiveSyntaxHelper = "something, nice";
            #pragma warning restore 219
        }
        #line hidden
        private global::TestNamespace.PTagHelper __TestNamespace_PTagHelper = null;
        #line hidden
        public SingleTagHelper()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
#line 3 "SingleTagHelper.cshtml"
__TestNamespace_PTagHelper.Age = 1337;

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
