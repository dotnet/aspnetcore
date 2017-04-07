#pragma checksum "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NullConditionalExpressions.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "c8c4f34e0768aea12ef6ce8e3fe0e384ad023faf"
namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_NullConditionalExpressions_Runtime
    {
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
#line 2 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NullConditionalExpressions.cshtml"
Write(ViewBag?.Data);

#line default
#line hidden
#line 3 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NullConditionalExpressions.cshtml"
Write(ViewBag.IntIndexer?[0]);

#line default
#line hidden
#line 4 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NullConditionalExpressions.cshtml"
Write(ViewBag.StrIndexer?["key"]);

#line default
#line hidden
#line 5 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NullConditionalExpressions.cshtml"
Write(ViewBag?.Method(Value?[23]?.More)?["key"]);

#line default
#line hidden
            WriteLiteral("\r\n");
#line 8 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NullConditionalExpressions.cshtml"
Write(ViewBag?.Data);

#line default
#line hidden
            WriteLiteral("\r\n");
#line 9 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NullConditionalExpressions.cshtml"
Write(ViewBag.IntIndexer?[0]);

#line default
#line hidden
            WriteLiteral("\r\n");
#line 10 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NullConditionalExpressions.cshtml"
Write(ViewBag.StrIndexer?["key"]);

#line default
#line hidden
            WriteLiteral("\r\n");
#line 11 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NullConditionalExpressions.cshtml"
Write(ViewBag?.Method(Value?[23]?.More)?["key"]);

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
