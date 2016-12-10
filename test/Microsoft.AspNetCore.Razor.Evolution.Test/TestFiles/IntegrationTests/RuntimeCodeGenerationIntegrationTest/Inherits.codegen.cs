#pragma checksum "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Inherits.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "72d6e21c6366f99a17c63abebb46db3470f4d1da"
namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_RuntimeCodeGenerationIntegrationTest_Inherits : foo.bar<baz<biz>>.boz
    {
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
#line 1 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Inherits.cshtml"
Write(foo());

#line default
#line hidden
            WriteLiteral("\r\n\r\n");
            WriteLiteral("bar\r\n");
        }
        #pragma warning restore 1998
    }
}
