#pragma checksum "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Usings.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "f452adb7c255f6d9d6d2573c6add7cb28022b151"
namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    using System.IO;
    using Foo = System.Text.Encoding;
    using static System;
    using static System.Console;
    using static global::System.Text.Encoding;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_Usings_Runtime
    {
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral("\r\n");
            WriteLiteral("\r\n<p>Path\'s full type name is ");
#line 9 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Usings.cshtml"
                       Write(typeof(Path).FullName);

#line default
#line hidden
            WriteLiteral("</p>\r\n<p>Foo\'s actual full type name is ");
#line 10 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Usings.cshtml"
                             Write(typeof(Foo).FullName);

#line default
#line hidden
            WriteLiteral("</p>");
        }
        #pragma warning restore 1998
    }
}
