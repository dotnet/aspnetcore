namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class EmptyAttributeTagHelpers
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            string __tagHelperDirectiveSyntaxHelper = null;
            __tagHelperDirectiveSyntaxHelper = 
#line 1 "EmptyAttributeTagHelpers.cshtml"
              "something"

#line default
#line hidden
            ;
            #pragma warning restore 219
        }
        #line hidden
        private InputTagHelper __InputTagHelper = null;
        private InputTagHelper2 __InputTagHelper2 = null;
        private PTagHelper __PTagHelper = null;
        #line hidden
        public EmptyAttributeTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
            __InputTagHelper.Type = "";
            __InputTagHelper2.Type = __InputTagHelper.Type;
#line 4 "EmptyAttributeTagHelpers.cshtml"
__InputTagHelper2.Checked = ;

#line default
#line hidden
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
            __InputTagHelper.Type = "";
            __InputTagHelper2.Type = __InputTagHelper.Type;
#line 6 "EmptyAttributeTagHelpers.cshtml"
  __InputTagHelper2.Checked = ;

#line default
#line hidden
            __PTagHelper = CreateTagHelper<PTagHelper>();
#line 5 "EmptyAttributeTagHelpers.cshtml"
__PTagHelper.Age = ;

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
