#pragma checksum "FunctionsBlock.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "94813053694a285515d791c48d703f1131881d0c"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

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

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            Instrumentation.BeginContext(19, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(140, 26, true);
            WriteLiteral("\r\nHere\'s a random number: ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(167, 11, false);
#line 12 "FunctionsBlock.cshtml"
                   Write(RandomInt());

#line default
#line hidden
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
