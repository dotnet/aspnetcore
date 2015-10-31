namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class EmptyAttributeTagHelpers
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
        private global::TestNamespace.PTagHelper __TestNamespace_PTagHelper = null;
        #line hidden
        public EmptyAttributeTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
            __TestNamespace_InputTagHelper.Type = "";
            __TestNamespace_InputTagHelper2.Type = __TestNamespace_InputTagHelper.Type;
#line 4 "EmptyAttributeTagHelpers.cshtml"
__TestNamespace_InputTagHelper2.Checked = ;

#line default
#line hidden
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
            __TestNamespace_InputTagHelper.Type = "";
            __TestNamespace_InputTagHelper2.Type = __TestNamespace_InputTagHelper.Type;
#line 6 "EmptyAttributeTagHelpers.cshtml"
__TestNamespace_InputTagHelper2.Checked = ;

#line default
#line hidden
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
#line 5 "EmptyAttributeTagHelpers.cshtml"
__TestNamespace_PTagHelper.Age = ;

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
