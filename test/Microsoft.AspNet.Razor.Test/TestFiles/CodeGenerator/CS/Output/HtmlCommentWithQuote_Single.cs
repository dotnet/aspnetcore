namespace TestOutput
{
    using System;

    public class HtmlCommentWithQuote_Single
    {
        #line hidden
        public HtmlCommentWithQuote_Single()
        {
        }

        public override void Execute()
        {
            WriteLiteral("<!-- \' -->\r\n<img");
            WriteAttribute("src", Tuple.Create(" src=\"", 16), Tuple.Create("\"", 41), Tuple.Create(Tuple.Create("", 22), Tuple.Create<System.Object, System.Int32>(Href("~/images/submit.png"), 22), false));
            WriteLiteral(" />");
        }
    }
}
