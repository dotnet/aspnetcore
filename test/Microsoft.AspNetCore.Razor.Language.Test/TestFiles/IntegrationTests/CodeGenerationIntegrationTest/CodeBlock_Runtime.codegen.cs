#pragma checksum "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/CodeBlock.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "019ce8023d064d08ca88c597b764aea895ec5841"
namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_CodeBlock_Runtime
    {
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
#line 1 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/CodeBlock.cshtml"
  
    for(int i = 1; i <= 10; i++) {
        Output.Write("<p>Hello from C#, #" + i.ToString() + "</p>");
    }

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
