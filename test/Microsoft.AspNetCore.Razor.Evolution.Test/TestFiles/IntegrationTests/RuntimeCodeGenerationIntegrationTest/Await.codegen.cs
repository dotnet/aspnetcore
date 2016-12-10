#pragma checksum "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Await.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "00b5e01b7a405dcfde7e4d512ee930daaa1778b5"
namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_RuntimeCodeGenerationIntegrationTest_Await
    {
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral("\r\n<section>\r\n    <h1>Basic Asynchronous Expression Test</h1>\r\n    <p>Basic Asynchronous Expression: ");
#line 10 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Await.cshtml"
                                 Write(await Foo());

#line default
#line hidden
            WriteLiteral("</p>\r\n    <p>Basic Asynchronous Template: ");
#line 11 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Await.cshtml"
                                Write(await Foo());

#line default
#line hidden
            WriteLiteral("</p>\r\n    <p>Basic Asynchronous Statement: ");
#line 12 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Await.cshtml"
                                        await Foo(); 

#line default
#line hidden
            WriteLiteral("</p>\r\n    <p>Basic Asynchronous Statement Nested:  <b>");
#line 13 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Await.cshtml"
                                             Write(await Foo());

#line default
#line hidden
            WriteLiteral("</b> ");
            WriteLiteral("</p>\r\n    <p>Basic Incomplete Asynchronous Statement: ");
#line 14 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Await.cshtml"
                                           Write(await);

#line default
#line hidden
            WriteLiteral("</p>\r\n</section>\r\n\r\n<section>\r\n    <h1>Advanced Asynchronous Expression Test</h1>\r\n    <p>Advanced Asynchronous Expression: ");
#line 19 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Await.cshtml"
                                    Write(await Foo(1, 2));

#line default
#line hidden
            WriteLiteral("</p>\r\n    <p>Advanced Asynchronous Expression Extended: ");
#line 20 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Await.cshtml"
                                             Write(await Foo.Bar(1, 2));

#line default
#line hidden
            WriteLiteral("</p>\r\n    <p>Advanced Asynchronous Template: ");
#line 21 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Await.cshtml"
                                   Write(await Foo("bob", true));

#line default
#line hidden
            WriteLiteral("</p>\r\n    <p>Advanced Asynchronous Statement: ");
#line 22 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Await.cshtml"
                                           await Foo(something, hello: "world"); 

#line default
#line hidden
            WriteLiteral("</p>\r\n    <p>Advanced Asynchronous Statement Extended: ");
#line 23 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Await.cshtml"
                                                    await Foo.Bar(1, 2) 

#line default
#line hidden
            WriteLiteral("</p>\r\n    <p>Advanced Asynchronous Statement Nested:  <b>");
#line 24 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Await.cshtml"
                                                Write(await Foo(boolValue: false));

#line default
#line hidden
            WriteLiteral("</b> ");
            WriteLiteral("</p>\r\n    <p>Advanced Incomplete Asynchronous Statement: ");
#line 25 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Await.cshtml"
                                              Write(await ("wrrronggg"));

#line default
#line hidden
            WriteLiteral("</p>\r\n</section>");
        }
        #pragma warning restore 1998
#line 1 "TestFiles/IntegrationTests/RuntimeCodeGenerationIntegrationTest/Await.cshtml"
            
    public async Task<string> Foo()
    {
        return "Bar";
    }


#line default
#line hidden
    }
}
