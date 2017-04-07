namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_EnumTagHelpers_DesignTime
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
        private global::TestNamespace.CatchAllTagHelper __TestNamespace_CatchAllTagHelper = null;
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
#line 3 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/EnumTagHelpers.cshtml"
  
    var enumValue = MyEnum.MyValue;

#line default
#line hidden
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
#line 7 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/EnumTagHelpers.cshtml"
__TestNamespace_InputTagHelper.Value = MyEnum.MyValue;

#line default
#line hidden
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
#line 8 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/EnumTagHelpers.cshtml"
         __o = MyEnum.MySecondValue;

#line default
#line hidden
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
#line 9 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/EnumTagHelpers.cshtml"
__TestNamespace_InputTagHelper.Value = global::Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestTagHelperDescriptors+MyEnum.MyValue;

#line default
#line hidden
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
#line 10 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/EnumTagHelpers.cshtml"
__TestNamespace_InputTagHelper.Value = global::Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestTagHelperDescriptors+MyEnum.MySecondValue;

#line default
#line hidden
#line 10 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/EnumTagHelpers.cshtml"
__TestNamespace_CatchAllTagHelper.CatchAll = global::Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestTagHelperDescriptors+MyEnum.MyValue;

#line default
#line hidden
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
#line 11 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/EnumTagHelpers.cshtml"
__TestNamespace_InputTagHelper.Value = enumValue;

#line default
#line hidden
#line 11 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/EnumTagHelpers.cshtml"
__TestNamespace_CatchAllTagHelper.CatchAll = enumValue;

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
