namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_SymbolBoundAttributes_DesignTime
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
        private global::TestNamespace.CatchAllTagHelper __TestNamespace_CatchAllTagHelper = null;
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
#line 12 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/SymbolBoundAttributes.cshtml"
__TestNamespace_CatchAllTagHelper.ListItems = items;

#line default
#line hidden
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
#line 13 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/SymbolBoundAttributes.cshtml"
__TestNamespace_CatchAllTagHelper.ArrayItems = items;

#line default
#line hidden
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
#line 14 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/SymbolBoundAttributes.cshtml"
__TestNamespace_CatchAllTagHelper.Event1 = doSomething();

#line default
#line hidden
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
#line 15 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/SymbolBoundAttributes.cshtml"
__TestNamespace_CatchAllTagHelper.Event2 = doSomething();

#line default
#line hidden
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __TestNamespace_CatchAllTagHelper.StringProperty1 = "value";
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __TestNamespace_CatchAllTagHelper.StringProperty2 = "value";
        }
        #pragma warning restore 1998
    }
}
