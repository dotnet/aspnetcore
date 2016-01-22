#pragma checksum "HtmlCommentWithQuote_Single.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "2d9bb4407e7aac9563aaeac9f0534a48f54e3d44"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class HtmlCommentWithQuote_Single
    {
        #line hidden
        public HtmlCommentWithQuote_Single()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            Instrumentation.BeginContext(0, 45, true);
            WriteLiteral("<!-- \' -->\r\n<img src=\"~/images/submit.png\" />");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
