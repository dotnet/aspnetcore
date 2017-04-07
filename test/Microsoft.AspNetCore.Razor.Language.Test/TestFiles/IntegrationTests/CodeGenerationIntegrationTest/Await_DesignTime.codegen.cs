namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_Await_DesignTime
    {
        #pragma warning disable 219
        private void __RazorDirectiveTokenHelpers__() {
        }
        #pragma warning restore 219
        private static System.Object __o = null;
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
#line 10 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Await.cshtml"
                                 __o = await Foo();

#line default
#line hidden
#line 11 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Await.cshtml"
                                __o = await Foo();

#line default
#line hidden
#line 12 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Await.cshtml"
                                        await Foo(); 

#line default
#line hidden
                                                           
#line 13 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Await.cshtml"
                                             __o = await Foo();

#line default
#line hidden
                                                                               
#line 14 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Await.cshtml"
                                           __o = await;

#line default
#line hidden
#line 19 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Await.cshtml"
                                    __o = await Foo(1, 2);

#line default
#line hidden
#line 20 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Await.cshtml"
                                             __o = await Foo.Bar(1, 2);

#line default
#line hidden
#line 21 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Await.cshtml"
                                   __o = await Foo("bob", true);

#line default
#line hidden
#line 22 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Await.cshtml"
                                           await Foo(something, hello: "world"); 

#line default
#line hidden
#line 23 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Await.cshtml"
                                                    await Foo.Bar(1, 2) 

#line default
#line hidden
                                                              
#line 24 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Await.cshtml"
                                                __o = await Foo(boolValue: false);

#line default
#line hidden
                                                                                                  
#line 25 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Await.cshtml"
                                              __o = await ("wrrronggg");

#line default
#line hidden
        }
        #pragma warning restore 1998
#line 1 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Await.cshtml"
            
    public async Task<string> Foo()
    {
        return "Bar";
    }

#line default
#line hidden
    }
}
