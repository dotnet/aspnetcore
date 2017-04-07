#pragma checksum "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Sections.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "ec9a74381c339244a887565526c11056ece494a3"
namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_Sections_Runtime
    {
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
#line 1 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Sections.cshtml"
  
    Layout = "_SectionTestLayout.cshtml"

#line default
#line hidden
            WriteLiteral("\r\n<div>This is in the Body>\r\n\r\n");
            DefineSection("Section2", async () => {
            WriteLiteral("\r\n    <div");
            BeginWriteAttribute("class", " class=\"", 109, "\"", 128, 2);
            WriteAttributeValue("", 117, "some", 117, 4, true);
#line 8 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Sections.cshtml"
WriteAttributeValue(" ", 121, thing, 122, 6, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">This is in Section 2</div>\r\n");
            });
            WriteLiteral("\r\n");
            DefineSection("Section1", async () => {
            WriteLiteral("\r\n    <div>This is in Section 1</div>\r\n");
            });
            WriteLiteral("\r\n");
            DefineSection("NestedDelegates", async () => {
            WriteLiteral("\r\n");
#line 16 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Sections.cshtml"
       Func<dynamic, object> f = 

#line default
#line hidden
            item => new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_template_writer) => {
                WriteLiteralTo(__razor_template_writer, "<span>");
#line 16 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Sections.cshtml"
        WriteTo(__razor_template_writer, item);

#line default
#line hidden
                WriteLiteralTo(__razor_template_writer, "</span>");
            }
            )
#line 16 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Sections.cshtml"
                                                    ; 

#line default
#line hidden
            });
        }
        #pragma warning restore 1998
    }
}
