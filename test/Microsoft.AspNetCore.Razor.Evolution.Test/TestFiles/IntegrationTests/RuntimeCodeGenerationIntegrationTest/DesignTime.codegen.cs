#pragma checksum "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/DesignTime.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "fa44b61006e587564a67bc785a9beeb41425a016"
namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_RuntimeCodeGenerationIntegrationTest_DesignTime
    {
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral("<div>\r\n");
#line 2 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/DesignTime.cshtml"
             for(int i = 1; i <= 10; i++) {


#line default
#line hidden
            WriteLiteral("    <p>This is item #");
#line 3 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/DesignTime.cshtml"
                Write(i);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 4 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/DesignTime.cshtml"
            }


#line default
#line hidden
            WriteLiteral("</div>\r\n\r\n<p>\r\n");
#line 8 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/DesignTime.cshtml"
Write(Foo(Bar.Baz));

#line default
#line hidden
            WriteLiteral("\r\n");
#line 9 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/DesignTime.cshtml"
Write(Foo(item => new HelperResult(async(__razor_template_writer) => {
    WriteLiteralTo(__razor_template_writer, "<p>Bar ");
#line 9 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/DesignTime.cshtml"
WriteTo(__razor_template_writer, baz);

#line default
#line hidden
    WriteLiteralTo(__razor_template_writer, " Biz</p>");
}
)));

#line default
#line hidden
            WriteLiteral("\r\n</p>\r\n\r\n");
            DefineSection("Footer", async(__razor_section_writer) => {
                WriteLiteralTo(__razor_section_writer, "\r\n    <p>Foo</p>\r\n    ");
#line 14 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/DesignTime.cshtml"
WriteTo(__razor_section_writer, bar);

#line default
#line hidden
                WriteLiteralTo(__razor_section_writer, "\r\n");
            }
            );
        }
        #pragma warning restore 1998
    }
}
