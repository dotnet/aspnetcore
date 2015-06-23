namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class DuplicateAttributeTagHelpers
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            string __tagHelperDirectiveSyntaxHelper = null;
            __tagHelperDirectiveSyntaxHelper = 
#line 1 "DuplicateAttributeTagHelpers.cshtml"
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
        public DuplicateAttributeTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
            __InputTagHelper.Type = "button";
            __InputTagHelper2.Type = __InputTagHelper.Type;
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
            __InputTagHelper.Type = "button";
            __InputTagHelper2.Type = __InputTagHelper.Type;
#line 5 "DuplicateAttributeTagHelpers.cshtml"
      __InputTagHelper2.Checked = true;

#line default
#line hidden
            __PTagHelper = CreateTagHelper<PTagHelper>();
#line 3 "DuplicateAttributeTagHelpers.cshtml"
__PTagHelper.Age = 3;

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
