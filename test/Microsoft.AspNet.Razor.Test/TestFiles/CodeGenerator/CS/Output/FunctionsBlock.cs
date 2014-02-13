namespace TestOutput
{
    using System;

    public class FunctionsBlock
    {
#line 1 "FunctionsBlock.cshtml"
            

#line default
#line hidden
#line 5 "FunctionsBlock.cshtml"
            
    Random _rand = new Random();
    private int RandomInt() {
        return _rand.Next();
    }
#line default
#line hidden
        #line hidden
        public FunctionsBlock()
        {
        }

        public override void Execute()
        {
            WriteLiteral("\r\n");
            WriteLiteral("\r\nHere\'s a random number: ");
            Write(
#line 12 "FunctionsBlock.cshtml"
                         RandomInt()
#line default
#line hidden
            );
        }
    }
}
