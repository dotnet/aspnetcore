namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class AttributeTargetingTagHelpers
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            string __tagHelperDirectiveSyntaxHelper = null;
            __tagHelperDirectiveSyntaxHelper = 
#line 1 "AttributeTargetingTagHelpers.cshtml"
              "*, something"

#line default
#line hidden
            ;
            #pragma warning restore 219
        }
        #line hidden
        private PTagHelper __PTagHelper = null;
        private CatchAllTagHelper __CatchAllTagHelper = null;
        private InputTagHelper __InputTagHelper = null;
        private InputTagHelper2 __InputTagHelper2 = null;
        #line hidden
        public AttributeTargetingTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
            __InputTagHelper.Type = "checkbox";
            __InputTagHelper2.Type = __InputTagHelper.Type;
#line 6 "AttributeTargetingTagHelpers.cshtml"
        __InputTagHelper2.Checked = true;

#line default
#line hidden
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
            __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
            __InputTagHelper.Type = "checkbox";
            __InputTagHelper2.Type = __InputTagHelper.Type;
#line 7 "AttributeTargetingTagHelpers.cshtml"
        __InputTagHelper2.Checked = true;

#line default
#line hidden
            __PTagHelper = CreateTagHelper<PTagHelper>();
        }
        #pragma warning restore 1998
    }
}
