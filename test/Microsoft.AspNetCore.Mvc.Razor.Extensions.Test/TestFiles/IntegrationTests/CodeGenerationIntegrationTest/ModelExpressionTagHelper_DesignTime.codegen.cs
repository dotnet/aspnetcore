namespace AspNetCore
{
    #line hidden
    using TModel = global::System.Object;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_ModelExpressionTagHelper_cshtml : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<DateTime>
    {
        #pragma warning disable 219
        private void __RazorDirectiveTokenHelpers__() {
        ((System.Action)(() => {
DateTime __typeHelper = null;
        }
        ))();
        ((System.Action)(() => {
global::System.Object __typeHelper = "InputTestTagHelper, AppCode";
        }
        ))();
        }
        #pragma warning restore 219
        private static System.Object __o = null;
        private global::InputTestTagHelper __InputTestTagHelper = null;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            __InputTestTagHelper = CreateTagHelper<global::InputTestTagHelper>();
#line 5 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ModelExpressionTagHelper.cshtml"
__InputTestTagHelper.For = ModelExpressionProvider.CreateModelExpression(ViewData, __model => __model.Date);

#line default
#line hidden
            __InputTestTagHelper = CreateTagHelper<global::InputTestTagHelper>();
#line 6 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ModelExpressionTagHelper.cshtml"
__InputTestTagHelper.For = ModelExpressionProvider.CreateModelExpression(ViewData, __model => Model);

#line default
#line hidden
        }
        #pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<DateTime> Html { get; private set; }
    }
}
