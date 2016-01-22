namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class EscapedTagHelpers
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            string __tagHelperDirectiveSyntaxHelper = null;
            __tagHelperDirectiveSyntaxHelper = "something";
            #pragma warning restore 219
        }
        #line hidden
        private global::TestNamespace.InputTagHelper __TestNamespace_InputTagHelper = null;
        private global::TestNamespace.InputTagHelper2 __TestNamespace_InputTagHelper2 = null;
        #line hidden
        public EscapedTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 4 "EscapedTagHelpers.cshtml"
                       __o = DateTime.Now;

#line default
#line hidden
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
#line 6 "EscapedTagHelpers.cshtml"
                                             __o = DateTime.Now;

#line default
#line hidden
            __TestNamespace_InputTagHelper.Type = string.Empty;
            __TestNamespace_InputTagHelper2.Type = __TestNamespace_InputTagHelper.Type;
#line 6 "EscapedTagHelpers.cshtml"
                                __TestNamespace_InputTagHelper2.Checked = true;

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
