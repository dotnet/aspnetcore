namespace TestOutput
{
    using System;

    public class CodeBlock
    {
        #line hidden
        public CodeBlock()
        {
        }

        public override void Execute()
        {
#line 1 "CodeBlock.cshtml"
  
    for(int i = 1; i <= 10; i++) {
        Output.Write("<p>Hello from C#, #" + i.ToString() + "</p>");
    }
#line default
#line hidden

        }
    }
}
