namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class FunctionsBlock
    {
        private static object @__o;
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
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            #pragma warning restore 219
        }
        #line hidden
        public FunctionsBlock()
        {
        }

        public override async Task ExecuteAsync()
        {
#line 1 "------------------------------------------"
                   __o = RandomInt();

#line default
#line hidden
        }
    }
}
