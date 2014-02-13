namespace TestOutput
{
    using System;

    public class HtmlCommentWithQuote_Double
    {
        #line hidden
        public HtmlCommentWithQuote_Double()
        {
        }

        public override void Execute()
        {
            WriteLiteral("<!-- \" -->\r\n<img");
            WriteAttribute("src", Tuple.Create(" src=\"", 16), Tuple.Create("\"", 41), Tuple.Create(Tuple.Create("", 22), Tuple.Create<System.Object, System.Int32>(Href("~/images/submit.png"), 22), false));
            WriteLiteral(" />");
        }
    }
}
