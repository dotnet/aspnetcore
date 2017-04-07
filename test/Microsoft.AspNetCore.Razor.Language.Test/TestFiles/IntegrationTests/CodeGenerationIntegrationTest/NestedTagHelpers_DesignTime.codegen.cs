namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_NestedTagHelpers_DesignTime
    {
        #pragma warning disable 219
        private void __RazorDirectiveTokenHelpers__() {
        ((System.Action)(() => {
global::System.Object __typeHelper = "*, TestAssembly";
        }
        ))();
        }
        #pragma warning restore 219
        private static System.Object __o = null;
        private global::SpanTagHelper __SpanTagHelper = null;
        private global::DivTagHelper __DivTagHelper = null;
        private global::InputTagHelper __InputTagHelper = null;
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            __SpanTagHelper = CreateTagHelper<global::SpanTagHelper>();
            __InputTagHelper = CreateTagHelper<global::InputTagHelper>();
            __InputTagHelper.FooProp = "Hello";
            __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
        }
        #pragma warning restore 1998
    }
}
