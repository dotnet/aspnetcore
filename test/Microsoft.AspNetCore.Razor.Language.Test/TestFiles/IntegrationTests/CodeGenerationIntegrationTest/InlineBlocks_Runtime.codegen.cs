#pragma checksum "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/InlineBlocks.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "e827e93343a95c7254a19287b095dfba9390d29f"
namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_InlineBlocks_Runtime
    {
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            DefineSection("Link", async () => {
            });
            WriteLiteral("(string link) {\r\n    <a");
            BeginWriteAttribute("href", " href=\"", 36, "\"", 93, 1);
            WriteAttributeValue("", 43, new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_attribute_value_writer) => {
#line 2 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/InlineBlocks.cshtml"
              if(link != null) { 

#line default
#line hidden
#line 2 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/InlineBlocks.cshtml"
WriteTo(__razor_attribute_value_writer, link);

#line default
#line hidden
#line 2 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/InlineBlocks.cshtml"
                                       } else {

#line default
#line hidden
                WriteLiteralTo(__razor_attribute_value_writer, "#");
#line 2 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/InlineBlocks.cshtml"
                                                               }

#line default
#line hidden
            }
            ), 43, 50, false);
            EndWriteAttribute();
            WriteLiteral(" />\r\n}");
        }
        #pragma warning restore 1998
    }
}
