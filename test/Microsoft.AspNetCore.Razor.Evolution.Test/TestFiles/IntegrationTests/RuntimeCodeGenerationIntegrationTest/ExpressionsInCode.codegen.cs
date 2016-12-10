#pragma checksum "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/ExpressionsInCode.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "8c7ae67489dbddec9f2dbef3c2b65def1149e507"
namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_RuntimeCodeGenerationIntegrationTest_ExpressionsInCode
    {
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
#line 1 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/ExpressionsInCode.cshtml"
  
    object foo = null;
    string bar = "Foo";


#line default
#line hidden
            WriteLiteral("\r\n");
#line 6 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/ExpressionsInCode.cshtml"
 if(foo != null) {
    

#line default
#line hidden
#line 7 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/ExpressionsInCode.cshtml"
Write(foo);

#line default
#line hidden
#line 7 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/ExpressionsInCode.cshtml"
        
} else {


#line default
#line hidden
            WriteLiteral("    <p>Foo is Null!</p>\r\n");
#line 10 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/ExpressionsInCode.cshtml"
}


#line default
#line hidden
            WriteLiteral("\r\n<p>\r\n");
#line 13 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/ExpressionsInCode.cshtml"
 if(!String.IsNullOrEmpty(bar)) {
    

#line default
#line hidden
#line 14 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/ExpressionsInCode.cshtml"
Write(bar.Replace("F", "B"));

#line default
#line hidden
#line 14 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/ExpressionsInCode.cshtml"
                            
}


#line default
#line hidden
            WriteLiteral("</p>");
        }
        #pragma warning restore 1998
    }
}
