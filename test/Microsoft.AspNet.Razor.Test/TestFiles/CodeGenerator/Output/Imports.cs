#pragma checksum "Imports.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "f452adb7c255f6d9d6d2573c6add7cb28022b151"
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
#line 5 "Imports.cshtml"
using static System

#line default
#line hidden
    ;
#line 6 "Imports.cshtml"
using static System.Console

#line default
#line hidden
    ;
#line 7 "Imports.cshtml"
using static global::System.Text.Encoding

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
            Instrumentation.BeginContext(68, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(166, 30, true);
            WriteLiteral("\r\n<p>Path\'s full type name is ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(197, 21, false);
#line 9 "Imports.cshtml"
                       Write(typeof(Path).FullName);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(218, 40, true);
            WriteLiteral("</p>\r\n<p>Foo\'s actual full type name is ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(259, 20, false);
#line 10 "Imports.cshtml"
                             Write(typeof(Foo).FullName);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(279, 4, true);
            WriteLiteral("</p>");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
