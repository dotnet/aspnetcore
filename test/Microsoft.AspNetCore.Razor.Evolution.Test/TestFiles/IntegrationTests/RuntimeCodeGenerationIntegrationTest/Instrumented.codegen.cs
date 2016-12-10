#pragma checksum "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "9b521264e3e64710635c0f0490a368845d90da66"
namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_RuntimeCodeGenerationIntegrationTest_Instrumented
    {
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
#line 1 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml"
  
    int i = 1;
    var foo = 

#line default
#line hidden
            item => new HelperResult(async(__razor_template_writer) => {
                WriteLiteralTo(__razor_template_writer, "<p>Bar</p>");
            }
            )
#line 3 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml"
                         ;


#line default
#line hidden
            WriteLiteral("    Hello, World\r\n    <p>Hello, World</p>\r\n");
            WriteLiteral("\r\n");
#line 8 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml"
 while(i <= 10) {


#line default
#line hidden
            WriteLiteral("    <p>Hello from C#, #");
#line 9 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml"
                   Write(i);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 10 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml"
    i += 1;
}


#line default
#line hidden
            WriteLiteral("\r\n");
#line 13 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml"
 if(i == 11) {


#line default
#line hidden
            WriteLiteral("    <p>We wrote 10 lines!</p>\r\n");
#line 15 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml"
}


#line default
#line hidden
            WriteLiteral("\r\n");
#line 17 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml"
 switch(i) {
    case 11:


#line default
#line hidden
            WriteLiteral("        <p>No really, we wrote 10 lines!</p>\r\n");
#line 20 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml"
        break;
    default:


#line default
#line hidden
            WriteLiteral("        <p>Actually, we didn\'t...</p>\r\n");
#line 23 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml"
        break;
}


#line default
#line hidden
            WriteLiteral("\r\n");
#line 26 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml"
 for(int j = 1; j <= 10; j += 2) {


#line default
#line hidden
            WriteLiteral("    <p>Hello again from C#, #");
#line 27 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml"
                         Write(j);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 28 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml"
}


#line default
#line hidden
            WriteLiteral("\r\n");
#line 30 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml"
 try {


#line default
#line hidden
            WriteLiteral("    <p>That time, we wrote 5 lines!</p>\r\n");
#line 32 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml"
} catch(Exception ex) {


#line default
#line hidden
            WriteLiteral("    <p>Oh no! An error occurred: ");
#line 33 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml"
                             Write(ex.Message);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 34 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml"
}


#line default
#line hidden
            WriteLiteral("\r\n");
#line 36 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml"
 lock(new object()) {


#line default
#line hidden
            WriteLiteral("    <p>This block is locked, for your security!</p>\r\n");
#line 38 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Instrumented.cshtml"
}

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
