namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class EscapedTagHelpers
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            string __tagHelperDirectiveSyntaxHelper = null;
            __tagHelperDirectiveSyntaxHelper = 
#line 1 "EscapedTagHelpers.cshtml"
              "something"

#line default
#line hidden
            ;
            #pragma warning restore 219
        }
        #line hidden
        private InputTagHelper __InputTagHelper = null;
        private InputTagHelper2 __InputTagHelper2 = null;
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
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
#line 6 "EscapedTagHelpers.cshtml"
__o = DateTime.Now;

#line default
#line hidden
            __InputTagHelper.Type = string.Empty;
            __InputTagHelper2.Type = __InputTagHelper.Type;
#line 6 "EscapedTagHelpers.cshtml"
                                              __InputTagHelper2.Checked = true;

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
