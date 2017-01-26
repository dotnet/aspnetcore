namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_DuplicateTargetTagHelper_DesignTime
    {
        #pragma warning disable 219
        private void __RazorDirectiveTokenHelpers__() {
        }
        #pragma warning restore 219
        private static System.Object __o = null;
        private global::TestNamespace.InputTagHelper __TestNamespace_InputTagHelper = null;
        private global::TestNamespace.CatchAllTagHelper __TestNamespace_CatchAllTagHelper = null;
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __TestNamespace_InputTagHelper.Type = "checkbox";
            __TestNamespace_CatchAllTagHelper.Type = __TestNamespace_InputTagHelper.Type;
#line 3 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DuplicateTargetTagHelper.cshtml"
__TestNamespace_InputTagHelper.Checked = true;

#line default
#line hidden
            __TestNamespace_CatchAllTagHelper.Checked = __TestNamespace_InputTagHelper.Checked;
        }
        #pragma warning restore 1998
    }
}
