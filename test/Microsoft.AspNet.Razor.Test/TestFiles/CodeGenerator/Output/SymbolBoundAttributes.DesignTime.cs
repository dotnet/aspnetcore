namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class SymbolBoundAttributes
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            string __tagHelperDirectiveSyntaxHelper = null;
            __tagHelperDirectiveSyntaxHelper = 
#line 1 "SymbolBoundAttributes.cshtml"
              "*, nice"

#line default
#line hidden
            ;
            #pragma warning restore 219
        }
        #line hidden
        private CatchAllTagHelper __CatchAllTagHelper = null;
        #line hidden
        public SymbolBoundAttributes()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
#line 12 "SymbolBoundAttributes.cshtml"
__CatchAllTagHelper.ListItems = items;

#line default
#line hidden
            __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
#line 13 "SymbolBoundAttributes.cshtml"
__CatchAllTagHelper.ArrayItems = items;

#line default
#line hidden
            __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
#line 14 "SymbolBoundAttributes.cshtml"
__CatchAllTagHelper.Event1 = doSomething();

#line default
#line hidden
            __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
#line 15 "SymbolBoundAttributes.cshtml"
__CatchAllTagHelper.Event2 = doSomething();

#line default
#line hidden
            __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
            __CatchAllTagHelper.StringProperty1 = "value";
            __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
            __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
            __CatchAllTagHelper.StringProperty2 = "value";
        }
        #pragma warning restore 1998
    }
}
