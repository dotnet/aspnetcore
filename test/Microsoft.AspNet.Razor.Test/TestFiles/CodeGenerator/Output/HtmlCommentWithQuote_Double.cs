#pragma checksum "HtmlCommentWithQuote_Double.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "a07711bc1fd0478b3b8329a68ab2028ef93429df"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class HtmlCommentWithQuote_Double
    {
        #line hidden
        public HtmlCommentWithQuote_Double()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            Instrumentation.BeginContext(0, 45, true);
            WriteLiteral("<!-- \" -->\r\n<img src=\"~/images/submit.png\" />");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
