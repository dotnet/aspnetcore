namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class FunctionsBlock_Tabs
    {
#line 1 "FunctionsBlock_Tabs.cshtml"



#line default
#line hidden
#line 5 "FunctionsBlock_Tabs.cshtml"

	Random _rand = new Random();
	private int RandomInt() {
		return _rand.Next();
	}

#line default
#line hidden
        #line hidden
        public FunctionsBlock_Tabs()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            Instrumentation.BeginContext(19, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(125, 26, true);
            WriteLiteral("\r\nHere\'s a random number: ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(152, 11, false);
            Write(
#line 12 "FunctionsBlock_Tabs.cshtml"
                         RandomInt()

#line default
#line hidden
            );

            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
