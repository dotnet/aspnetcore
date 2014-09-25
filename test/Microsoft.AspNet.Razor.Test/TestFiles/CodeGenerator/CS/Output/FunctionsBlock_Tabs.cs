#pragma checksum "FunctionsBlock_Tabs.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "93dd195557fe9c2e5a15b75d76608f2cb7082f3f"
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
#line 12 "FunctionsBlock_Tabs.cshtml"
                   Write(RandomInt());

#line default
#line hidden
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
