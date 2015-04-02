#pragma checksum "Imports.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "1b57def0ce2632d0f50d63dbdc5e0e719c202863"
namespace TestOutput
{
#line 1 "Imports.cshtml"
using System.IO

#line default
#line hidden
    ;
#line 2 "Imports.cshtml"
using Foo = System.Text.Encoding

#line default
#line hidden
    ;
#line 3 "Imports.cshtml"
using System

#line default
#line hidden
    ;
    using System.Threading.Tasks;

    public class Imports
    {
        #line hidden
        public Imports()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            Instrumentation.BeginContext(68, 30, true);
            WriteLiteral("\r\n<p>Path\'s full type name is ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(99, 21, false);
#line 5 "Imports.cshtml"
                       Write(typeof(Path).FullName);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(120, 40, true);
            WriteLiteral("</p>\r\n<p>Foo\'s actual full type name is ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(161, 20, false);
#line 6 "Imports.cshtml"
                             Write(typeof(Foo).FullName);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(181, 4, true);
            WriteLiteral("</p>");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
