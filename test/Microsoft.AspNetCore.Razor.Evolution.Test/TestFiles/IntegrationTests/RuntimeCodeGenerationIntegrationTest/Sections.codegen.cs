#pragma checksum "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Sections.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "ec9a74381c339244a887565526c11056ece494a3"
namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_RuntimeCodeGenerationIntegrationTest_Sections
    {
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
#line 1 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Sections.cshtml"
  
    Layout = "_SectionTestLayout.cshtml"


#line default
#line hidden
            WriteLiteral("\r\n<div>This is in the Body>\r\n\r\n");
            DefineSection("Section2", async(__razor_section_writer) => {
                WriteLiteralTo(__razor_section_writer, "\r\n    <div");
                BeginWriteAttributeTo(__razor_section_writer, "class", " class=\"", 109, "\"", 128, 2);
                WriteAttributeValueTo(__razor_section_writer, "", 117, "some", 117, 4, true);
#line 8 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Sections.cshtml"
WriteAttributeValueTo(__razor_section_writer, " ", 121, thing, 122, 6, false);

#line default
#line hidden
                EndWriteAttributeTo(__razor_section_writer);
                WriteLiteralTo(__razor_section_writer, ">This is in Section 2</div>\r\n");
            }
            );
            WriteLiteral("\r\n");
            DefineSection("Section1", async(__razor_section_writer) => {
                WriteLiteralTo(__razor_section_writer, "\r\n    <div>This is in Section 1</div>\r\n");
            }
            );
            WriteLiteral("\r\n");
            DefineSection("NestedDelegates", async(__razor_section_writer) => {
                WriteLiteralTo(__razor_section_writer, "\r\n");
#line 16 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Sections.cshtml"
       Func<dynamic, object> f = 

#line default
#line hidden
                item => new HelperResult(async(__razor_template_writer) => {
                    WriteLiteralTo(__razor_template_writer, "<span>");
#line 16 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Sections.cshtml"
        WriteTo(__razor_template_writer, item);

#line default
#line hidden
                    WriteLiteralTo(__razor_template_writer, "</span>");
                }
                )
#line 16 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Sections.cshtml"
                                                    ; 

#line default
#line hidden
            }
            );
        }
        #pragma warning restore 1998
    }
}
