#pragma checksum "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/FunctionsBlock.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "94813053694a285515d791c48d703f1131881d0c"
namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_RuntimeCodeGenerationIntegrationTest_FunctionsBlock
    {
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral("\r\n");
            WriteLiteral("\r\nHere\'s a random number: ");
#line 12 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/FunctionsBlock.cshtml"
                   Write(RandomInt());

#line default
#line hidden
        }
        #pragma warning restore 1998
#line 5 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/FunctionsBlock.cshtml"
            
    Random _rand = new Random();
    private int RandomInt() {
        return _rand.Next();
    }


#line default
#line hidden
    }
}
