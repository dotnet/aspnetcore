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
            Write(
#line 5 "Imports.cshtml"
                             typeof(Path).FullName

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(120, 40, true);
            WriteLiteral("</p>\r\n<p>Foo\'s actual full type name is ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(161, 20, false);
            Write(
#line 6 "Imports.cshtml"
                                   typeof(Foo).FullName

#line default
#line hidden
            );

            Instrumentation.EndContext();
            Instrumentation.BeginContext(181, 4, true);
            WriteLiteral("</p>");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
