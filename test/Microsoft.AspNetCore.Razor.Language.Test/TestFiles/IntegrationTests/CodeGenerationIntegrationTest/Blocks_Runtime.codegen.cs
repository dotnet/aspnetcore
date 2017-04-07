#pragma checksum "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "ba7d8f5f5159a2389c780aa606885ef6c917a45a"
namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_Blocks_Runtime
    {
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
#line 1 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml"
  
    int i = 1;

#line default
#line hidden
            WriteLiteral("\r\n");
#line 5 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml"
 while(i <= 10) {

#line default
#line hidden
            WriteLiteral("    <p>Hello from C#, #");
#line 6 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml"
                   Write(i);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 7 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml"
    i += 1;
}

#line default
#line hidden
            WriteLiteral("\r\n");
#line 10 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml"
 if(i == 11) {

#line default
#line hidden
            WriteLiteral("    <p>We wrote 10 lines!</p>\r\n");
#line 12 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml"
}

#line default
#line hidden
            WriteLiteral("\r\n");
#line 14 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml"
 switch(i) {
    case 11:

#line default
#line hidden
            WriteLiteral("        <p>No really, we wrote 10 lines!</p>\r\n");
#line 17 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml"
        break;
    default:

#line default
#line hidden
            WriteLiteral("        <p>Actually, we didn\'t...</p>\r\n");
#line 20 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml"
        break;
}

#line default
#line hidden
            WriteLiteral("\r\n");
#line 23 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml"
 for(int j = 1; j <= 10; j += 2) {

#line default
#line hidden
            WriteLiteral("    <p>Hello again from C#, #");
#line 24 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml"
                         Write(j);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 25 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml"
}

#line default
#line hidden
            WriteLiteral("\r\n");
#line 27 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml"
 try {

#line default
#line hidden
            WriteLiteral("    <p>That time, we wrote 5 lines!</p>\r\n");
#line 29 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml"
} catch(Exception ex) {

#line default
#line hidden
            WriteLiteral("    <p>Oh no! An error occurred: ");
#line 30 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml"
                             Write(ex.Message);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 31 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml"
}

#line default
#line hidden
            WriteLiteral("\r\n<p>i is now ");
#line 33 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml"
       Write(i);

#line default
#line hidden
            WriteLiteral("</p>\r\n\r\n");
#line 35 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml"
 lock(new object()) {

#line default
#line hidden
            WriteLiteral("    <p>This block is locked, for your security!</p>\r\n");
#line 37 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Blocks.cshtml"
}

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
