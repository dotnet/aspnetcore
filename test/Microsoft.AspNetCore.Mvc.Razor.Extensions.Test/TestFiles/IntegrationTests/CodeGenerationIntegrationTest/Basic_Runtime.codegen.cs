#pragma checksum "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Basic.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "fd421120502bfd80d21169d04fd6ba54b5cc7f12"
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_Basic_cshtml : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            BeginContext(0, 4, true);
            WriteLiteral("<div");
            EndContext();
            BeginWriteAttribute("class", " class=\"", 4, "\"", 28, 1);
#line 1 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Basic.cshtml"
WriteAttributeValue("", 12, this.ToString(), 12, 16, false);

#line default
#line hidden
            EndWriteAttribute();
            BeginContext(29, 24, true);
            WriteLiteral(">\r\n    Hello world\r\n    ");
            EndContext();
            BeginContext(54, 29, false);
#line 3 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Basic.cshtml"
Write(string.Format("{0}", "Hello"));

#line default
#line hidden
            EndContext();
            BeginContext(83, 10, true);
            WriteLiteral("\r\n</div>\r\n");
            EndContext();
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
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
