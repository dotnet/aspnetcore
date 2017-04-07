namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_EscapedTagHelpers_DesignTime
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
        private global::TestNamespace.InputTagHelper __TestNamespace_InputTagHelper = null;
        private global::TestNamespace.InputTagHelper2 __TestNamespace_InputTagHelper2 = null;
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
#line 4 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/EscapedTagHelpers.cshtml"
                       __o = DateTime.Now;

#line default
#line hidden
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
#line 6 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/EscapedTagHelpers.cshtml"
                                             __o = DateTime.Now;

#line default
#line hidden
            __TestNamespace_InputTagHelper.Type = string.Empty;
            __TestNamespace_InputTagHelper2.Type = __TestNamespace_InputTagHelper.Type;
#line 6 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/EscapedTagHelpers.cshtml"
                                __TestNamespace_InputTagHelper2.Checked = true;

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
