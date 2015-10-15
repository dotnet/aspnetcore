namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class NestedScriptTagTagHelpers
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            string __tagHelperDirectiveSyntaxHelper = null;
            __tagHelperDirectiveSyntaxHelper = 
#line 1 "NestedScriptTagTagHelpers.cshtml"
              "something, nice"

#line default
#line hidden
            ;
            #pragma warning restore 219
        }
        #line hidden
        private PTagHelper __PTagHelper = null;
        private InputTagHelper __InputTagHelper = null;
        private InputTagHelper2 __InputTagHelper2 = null;
        #line hidden
        public NestedScriptTagTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 6 "NestedScriptTagTagHelpers.cshtml"
            

#line default
#line hidden

#line 6 "NestedScriptTagTagHelpers.cshtml"
            for(var i = 0; i < 5; i++) {

#line default
#line hidden

            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
#line 8 "NestedScriptTagTagHelpers.cshtml"
                                            __o = ViewBag.DefaultInterval;

#line default
#line hidden
            __InputTagHelper.Type = "text";
            __InputTagHelper2.Type = __InputTagHelper.Type;
#line 8 "NestedScriptTagTagHelpers.cshtml"
                                                                        __InputTagHelper2.Checked = true;

#line default
#line hidden
#line 10 "NestedScriptTagTagHelpers.cshtml"
            }

#line default
#line hidden

            __PTagHelper = CreateTagHelper<PTagHelper>();
        }
        #pragma warning restore 1998
    }
}
