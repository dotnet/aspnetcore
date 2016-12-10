#pragma checksum "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/RazorComments.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "45c16f152aef80d7de27c7df32dc522af5842197"
namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_RuntimeCodeGenerationIntegrationTest_RazorComments
    {
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral("\r\n<p>This should  be shown</p>\r\n\r\n");
#line 5 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/RazorComments.cshtml"
                                       
    Exception foo = 

#line default
#line hidden
#line 6 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/RazorComments.cshtml"
                                                  null;
    if(foo != null) {
        throw foo;
    }


#line default
#line hidden
            WriteLiteral("\r\n");
#line 12 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/RazorComments.cshtml"
   var bar = "@* bar *@"; 

#line default
#line hidden
            WriteLiteral("<p>But this should show the comment syntax: ");
#line 13 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/RazorComments.cshtml"
                                       Write(bar);

#line default
#line hidden
            WriteLiteral("</p>\r\n\r\n");
#line 15 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/RazorComments.cshtml"
Write(ab);

#line default
#line hidden
            WriteLiteral("\r\n");
        }
        #pragma warning restore 1998
    }
}
