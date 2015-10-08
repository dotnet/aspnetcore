namespace Asp
{
    using System.Threading.Tasks;

    public class ASPV_testfiles_input_basic_cshtml : Microsoft.AspNet.Mvc.Razor.RazorPage<dynamic>
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            #pragma warning restore 219
        }
        #line hidden
        public ASPV_testfiles_input_basic_cshtml()
        {
        }
        #line hidden
        [Microsoft.AspNet.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNet.Mvc.IUrlHelper Url { get; private set; }
        [Microsoft.AspNet.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNet.Mvc.IViewComponentHelper Component { get; private set; }
        [Microsoft.AspNet.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNet.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [Microsoft.AspNet.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNet.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }

        #line hidden

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 1 "testfiles/input/basic.cshtml"
       __o = logo;

#line default
#line hidden
#line 3 "testfiles/input/basic.cshtml"
__o = Html.Input("SomeKey");

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
