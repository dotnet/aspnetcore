#pragma checksum "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/MalformedPageDirective.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "5a9ff8440150c6746e4a8ba63bc633ea84930405"
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_MalformedPageDirective_cshtml : global::Microsoft.AspNetCore.Mvc.RazorPages.Page
    {
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            BeginContext(12, 43, true);
            WriteLiteral("\r\n<h1>About Us</h1>\r\n<p>We are awesome.</p>");
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
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TestFiles_IntegrationTests_CodeGenerationIntegrationTest_MalformedPageDirective_cshtml> Html { get; private set; }
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<TestFiles_IntegrationTests_CodeGenerationIntegrationTest_MalformedPageDirective_cshtml> ViewData => (global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<TestFiles_IntegrationTests_CodeGenerationIntegrationTest_MalformedPageDirective_cshtml>)PageContext?.ViewData;
        public TestFiles_IntegrationTests_CodeGenerationIntegrationTest_MalformedPageDirective_cshtml Model => ViewData.Model;
    }
}
