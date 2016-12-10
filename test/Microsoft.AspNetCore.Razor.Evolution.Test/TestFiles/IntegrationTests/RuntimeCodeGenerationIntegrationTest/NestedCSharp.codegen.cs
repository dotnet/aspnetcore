#pragma checksum "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/NestedCSharp.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "2b9e8dcf7c08153c15ac84973938a7c0254f2369"
namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_RuntimeCodeGenerationIntegrationTest_NestedCSharp
    {
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
#line 2 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/NestedCSharp.cshtml"
     foreach (var result in (dynamic)Url)
    {


#line default
#line hidden
            WriteLiteral("        <div>\r\n            ");
#line 5 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/NestedCSharp.cshtml"
       Write(result.SomeValue);

#line default
#line hidden
            WriteLiteral(".\r\n        </div>\r\n");
#line 7 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/NestedCSharp.cshtml"
    }

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
