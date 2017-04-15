#pragma checksum "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "8a16b856f52a2ed469347984184b8ac48080234f"
namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_Templates_Runtime
    {
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral("\r\n");
#line 11 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
  
    Func<dynamic, object> foo = 

#line default
#line hidden
            item => new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_template_writer) => {
                WriteLiteralTo(__razor_template_writer, "This works ");
#line 12 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
                  WriteTo(__razor_template_writer, item);

#line default
#line hidden
                WriteLiteralTo(__razor_template_writer, "!");
            }
            )
#line 12 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
                                                               ;
    

#line default
#line hidden
#line 13 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
Write(foo(""));

#line default
#line hidden
            WriteLiteral("\r\n");
#line 16 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
   
    Func<dynamic, object> bar = 

#line default
#line hidden
            item => new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_template_writer) => {
                WriteLiteralTo(__razor_template_writer, "<p");
                BeginWriteAttributeTo(__razor_template_writer, "class", " class=\"", 411, "\"", 424, 1);
#line 17 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
WriteAttributeValueTo(__razor_template_writer, "", 419, item, 419, 5, false);

#line default
#line hidden
                EndWriteAttributeTo(__razor_template_writer);
                WriteLiteralTo(__razor_template_writer, ">Hello</p>");
            }
            )
#line 17 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
                                                           ;
    

#line default
#line hidden
#line 18 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
Write(bar("myclass"));

#line default
#line hidden
            WriteLiteral("\r\n<ul>\r\n");
#line 22 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
Write(Repeat(10, item => new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_template_writer) => {
    WriteLiteralTo(__razor_template_writer, "<li>Item #");
#line 22 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
WriteTo(__razor_template_writer, item);

#line default
#line hidden
    WriteLiteralTo(__razor_template_writer, "</li>");
}
)));

#line default
#line hidden
            WriteLiteral("\r\n</ul>\r\n\r\n<p>\r\n");
#line 26 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
Write(Repeat(10,
    item => new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_template_writer) => {
    WriteLiteralTo(__razor_template_writer, " This is line#");
#line 27 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
WriteTo(__razor_template_writer, item);

#line default
#line hidden
    WriteLiteralTo(__razor_template_writer, " of markup<br/>\r\n");
}
)));

#line default
#line hidden
            WriteLiteral("\r\n</p>\r\n\r\n<p>\r\n");
#line 32 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
Write(Repeat(10,
    item => new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_template_writer) => {
    WriteLiteralTo(__razor_template_writer, ": This is line#");
#line 33 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
WriteTo(__razor_template_writer, item);

#line default
#line hidden
    WriteLiteralTo(__razor_template_writer, " of markup<br />\r\n");
}
)));

#line default
#line hidden
            WriteLiteral("\r\n</p>\r\n\r\n<p>\r\n");
#line 38 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
Write(Repeat(10,
    item => new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_template_writer) => {
    WriteLiteralTo(__razor_template_writer, ":: This is line#");
#line 39 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
WriteTo(__razor_template_writer, item);

#line default
#line hidden
    WriteLiteralTo(__razor_template_writer, " of markup<br />\r\n");
}
)));

#line default
#line hidden
            WriteLiteral("\r\n</p>\r\n\r\n\r\n<ul>\r\n    ");
#line 45 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
Write(Repeat(10, item => new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_template_writer) => {
    WriteLiteralTo(__razor_template_writer, "<li>\r\n        Item #");
#line 46 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
WriteTo(__razor_template_writer, item);

#line default
#line hidden
    WriteLiteralTo(__razor_template_writer, "\r\n");
#line 47 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
          var parent = item;

#line default
#line hidden
    WriteLiteralTo(__razor_template_writer, "        <ul>\r\n            <li>Child Items... ?</li>\r\n");
    WriteLiteralTo(__razor_template_writer, "        </ul>\r\n    </li>");
}
)));

#line default
#line hidden
            WriteLiteral("\r\n</ul> ");
        }
        #pragma warning restore 1998
#line 1 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Templates.cshtml"
            
    public HelperResult Repeat(int times, Func<int, object> template) {
        return new HelperResult((writer) => {
            for(int i = 0; i < times; i++) {
                ((HelperResult)template(i)).WriteTo(writer);
            }
        });
    }

#line default
#line hidden
    }
}
