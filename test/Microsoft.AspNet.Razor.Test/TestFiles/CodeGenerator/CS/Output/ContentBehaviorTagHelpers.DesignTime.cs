namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class ContentBehaviorTagHelpers
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            string __tagHelperDirectiveSyntaxHelper = null;
            __tagHelperDirectiveSyntaxHelper = 
#line 1 "ContentBehaviorTagHelpers.cshtml"
              "something"

#line default
#line hidden
            ;
            #pragma warning restore 219
        }
        #line hidden
        private ModifyTagHelper __ModifyTagHelper = null;
        private NoneTagHelper __NoneTagHelper = null;
        private AppendTagHelper __AppendTagHelper = null;
        private PrependTagHelper __PrependTagHelper = null;
        private ReplaceTagHelper __ReplaceTagHelper = null;
        #line hidden
        public ContentBehaviorTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __ModifyTagHelper = CreateTagHelper<ModifyTagHelper>();
            __NoneTagHelper = CreateTagHelper<NoneTagHelper>();
            __AppendTagHelper = CreateTagHelper<AppendTagHelper>();
            __PrependTagHelper = CreateTagHelper<PrependTagHelper>();
            __ReplaceTagHelper = CreateTagHelper<ReplaceTagHelper>();
        }
        #pragma warning restore 1998
    }
}
