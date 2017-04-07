namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_FunctionsBlock_DesignTime
    {
        #pragma warning disable 219
        private void __RazorDirectiveTokenHelpers__() {
        }
        #pragma warning restore 219
        private static System.Object __o = null;
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
#line 12 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/FunctionsBlock.cshtml"
                   __o = RandomInt();

#line default
#line hidden
        }
        #pragma warning restore 1998
                    


#line 5 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/FunctionsBlock.cshtml"
            
    Random _rand = new Random();
    private int RandomInt() {
        return _rand.Next();
    }

#line default
#line hidden
    }
}
