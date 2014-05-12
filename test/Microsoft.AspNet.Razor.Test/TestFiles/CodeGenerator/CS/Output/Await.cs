namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class Await
    {
#line 1 "Await.cshtml"

    public async Task<string> Foo()
    {
        return "Bar";
    }

#line default
#line hidden
        #line hidden
        public Await()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            WriteLiteral("\r\n<section>\r\n    <h1>Basic Asynchronous Expression Test</h1>\r\n    <p>Basic Asynch" +
"ronous Expression: ");
            Write(
#line 10 "Await.cshtml"
                                       await Foo()

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n    <p>Basic Asynchronous Template: ");
            Write(
#line 11 "Await.cshtml"
                                      await Foo()

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n    <p>Basic Asynchronous Statement: ");
#line 12 "Await.cshtml"
                                        await Foo(); 

#line default
#line hidden

            WriteLiteral("</p>\r\n    <p>Basic Asynchronous Statement Nested:  <b>");
            Write(
#line 13 "Await.cshtml"
                                                   await Foo()

#line default
#line hidden
            );

            WriteLiteral("</b> ");
#line 13 "Await.cshtml"
                                                                   

#line default
#line hidden

            WriteLiteral("</p>\r\n    <p>Basic Incomplete Asynchronous Statement: ");
            Write(
#line 14 "Await.cshtml"
                                                 await

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n</section>\r\n\r\n<section>\r\n    <h1>Advanced Asynchronous Expression Test</h1>" +
"\r\n    <p>Advanced Asynchronous Expression: ");
            Write(
#line 19 "Await.cshtml"
                                          await Foo(1, 2)

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n    <p>Advanced Asynchronous Expression Extended: ");
            Write(
#line 20 "Await.cshtml"
                                                   await Foo.Bar(1, 2)

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n    <p>Advanced Asynchronous Template: ");
            Write(
#line 21 "Await.cshtml"
                                         await Foo("bob", true)

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n    <p>Advanced Asynchronous Statement: ");
#line 22 "Await.cshtml"
                                           await Foo(something, hello: "world"); 

#line default
#line hidden

            WriteLiteral("</p>\r\n    <p>Advanced Asynchronous Statement Extended: ");
#line 23 "Await.cshtml"
                                                    await Foo.Bar(1, 2) 

#line default
#line hidden

            WriteLiteral("</p>\r\n    <p>Advanced Asynchronous Statement Nested:  <b>");
            Write(
#line 24 "Await.cshtml"
                                                      await Foo(boolValue: false)

#line default
#line hidden
            );

            WriteLiteral("</b> ");
#line 24 "Await.cshtml"
                                                                                      

#line default
#line hidden

            WriteLiteral("</p>\r\n    <p>Advanced Incomplete Asynchronous Statement: ");
            Write(
#line 25 "Await.cshtml"
                                                    await ("wrrronggg")

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n</section>");
        }
        #pragma warning restore 1998
    }
}
