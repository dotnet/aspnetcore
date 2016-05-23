#pragma checksum "TestFiles/Input/InjectWithModel.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "1f010500f93116162444110956e512df61642f4e"
namespace AspNetCore
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using System.Threading.Tasks;

    public class TestFiles_Input_InjectWithModel_cshtml : Microsoft.AspNetCore.Mvc.Razor.RazorPage<MyModel>
    {
        #line hidden
        public TestFiles_Input_InjectWithModel_cshtml()
        {
        }
        #line hidden
        [Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public MyApp MyPropertyName { get; private set; }
        [Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public MyService<MyModel> Html { get; private set; }
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
