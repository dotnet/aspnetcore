#pragma checksum "CodeBlock.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "019ce8023d064d08ca88c597b764aea895ec5841"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class CodeBlock
    {
        #line hidden
        public CodeBlock()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 1 "CodeBlock.cshtml"
  
    for(int i = 1; i <= 10; i++) {
        Output.Write("<p>Hello from C#, #" + i.ToString() + "</p>");
    }

#line default
#line hidden

        }
        #pragma warning restore 1998
    }
}
