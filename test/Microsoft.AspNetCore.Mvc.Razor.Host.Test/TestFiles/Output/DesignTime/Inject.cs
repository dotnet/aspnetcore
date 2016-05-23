namespace AspNetCore
{
#line 1 "testfiles/input/inject.cshtml"
using MyNamespace

#line default
#line hidden
    ;
    using System.Threading.Tasks;

    public class testfiles_input_inject_cshtml : Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            #pragma warning restore 219
        }
        #line hidden
        public testfiles_input_inject_cshtml()
        {
        }
        #line hidden
        [Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public
#line 2 "testfiles/input/inject.cshtml"
        MyApp MyPropertyName

#line default
#line hidden
        { get; private set; }
        [Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }

        #line hidden

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
        }
        #pragma warning restore 1998
    }
}
