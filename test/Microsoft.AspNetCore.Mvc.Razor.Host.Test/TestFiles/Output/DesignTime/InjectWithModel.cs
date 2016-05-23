namespace AspNetCore
{
    using System.Threading.Tasks;

    public class testfiles_input_injectwithmodel_cshtml : Microsoft.AspNetCore.Mvc.Razor.RazorPage<MyModel>
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
#line 1 "testfiles/input/injectwithmodel.cshtml"
var __modelHelper = default(MyModel);

#line default
#line hidden
            #pragma warning restore 219
        }
        #line hidden
        public testfiles_input_injectwithmodel_cshtml()
        {
        }
        #line hidden
        [Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public
#line 2 "testfiles/input/injectwithmodel.cshtml"
        MyApp MyPropertyName

#line default
#line hidden
        { get; private set; }
        [Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public
#line 3 "testfiles/input/injectwithmodel.cshtml"
        MyService<MyModel> Html

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

        #line hidden

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
        }
        #pragma warning restore 1998
    }
}
