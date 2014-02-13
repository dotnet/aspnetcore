namespace TestOutput
{
    using System;

    public class MarkupInCodeBlock
    {
        #line hidden
        public MarkupInCodeBlock()
        {
        }

        public override void Execute()
        {
#line 1 "MarkupInCodeBlock.cshtml"
  
    for(int i = 1; i <= 10; i++) {
#line default
#line hidden

            WriteLiteral("        <p>Hello from C#, #");
            Write(
#line 3 "MarkupInCodeBlock.cshtml"
                             i.ToString()
#line default
#line hidden
            );
            WriteLiteral("</p>\r\n");
#line 4 "MarkupInCodeBlock.cshtml"
    }
#line default
#line hidden

            WriteLiteral("\r\n");
        }
    }
}
