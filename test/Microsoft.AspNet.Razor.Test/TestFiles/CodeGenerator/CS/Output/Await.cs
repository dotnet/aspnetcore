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
            Instrumentation.BeginContext(91, 100, true);
            WriteLiteral("\r\n<section>\r\n    <h1>Basic Asynchronous Expression Test</h1>\r\n    <p>Basic Asynch" +
"ronous Expression: ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(192, 11, false);
            Write(
#line 10 "Await.cshtml"
                                       await Foo()

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(203, 42, true);
            WriteLiteral("</p>\r\n    <p>Basic Asynchronous Template: ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(247, 11, false);
            Write(
#line 11 "Await.cshtml"
                                      await Foo()

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(259, 43, true);
            WriteLiteral("</p>\r\n    <p>Basic Asynchronous Statement: ");
            Instrumentation.EndContext();
#line 12 "Await.cshtml"
                                        await Foo(); 

#line default
#line hidden

            Instrumentation.BeginContext(319, 54, true);
            WriteLiteral("</p>\r\n    <p>Basic Asynchronous Statement Nested:  <b>");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(376, 11, false);
            Write(
#line 13 "Await.cshtml"
                                                   await Foo()

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(387, 5, true);
            WriteLiteral("</b> ");
            Instrumentation.EndContext();
#line 13 "Await.cshtml"
                                                                   

#line default
#line hidden

            Instrumentation.BeginContext(393, 54, true);
            WriteLiteral("</p>\r\n    <p>Basic Incomplete Asynchronous Statement: ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(448, 5, false);
            Write(
#line 14 "Await.cshtml"
                                                 await

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(453, 124, true);
            WriteLiteral("</p>\r\n</section>\r\n\r\n<section>\r\n    <h1>Advanced Asynchronous Expression Test</h1>" +
"\r\n    <p>Advanced Asynchronous Expression: ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(578, 15, false);
            Write(
#line 19 "Await.cshtml"
                                          await Foo(1, 2)

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(593, 56, true);
            WriteLiteral("</p>\r\n    <p>Advanced Asynchronous Expression Extended: ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(650, 19, false);
            Write(
#line 20 "Await.cshtml"
                                                   await Foo.Bar(1, 2)

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(669, 45, true);
            WriteLiteral("</p>\r\n    <p>Advanced Asynchronous Template: ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(716, 22, false);
            Write(
#line 21 "Await.cshtml"
                                         await Foo("bob", true)

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(739, 46, true);
            WriteLiteral("</p>\r\n    <p>Advanced Asynchronous Statement: ");
            Instrumentation.EndContext();
#line 22 "Await.cshtml"
                                           await Foo(something, hello: "world"); 

#line default
#line hidden

            Instrumentation.BeginContext(827, 55, true);
            WriteLiteral("</p>\r\n    <p>Advanced Asynchronous Statement Extended: ");
            Instrumentation.EndContext();
#line 23 "Await.cshtml"
                                                    await Foo.Bar(1, 2) 

#line default
#line hidden

            Instrumentation.BeginContext(906, 57, true);
            WriteLiteral("</p>\r\n    <p>Advanced Asynchronous Statement Nested:  <b>");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(966, 27, false);
            Write(
#line 24 "Await.cshtml"
                                                      await Foo(boolValue: false)

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(993, 5, true);
            WriteLiteral("</b> ");
            Instrumentation.EndContext();
#line 24 "Await.cshtml"
                                                                                      

#line default
#line hidden

            Instrumentation.BeginContext(999, 57, true);
            WriteLiteral("</p>\r\n    <p>Advanced Incomplete Asynchronous Statement: ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(1057, 19, false);
            Write(
#line 25 "Await.cshtml"
                                                    await ("wrrronggg")

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(1076, 16, true);
            WriteLiteral("</p>\r\n</section>");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
