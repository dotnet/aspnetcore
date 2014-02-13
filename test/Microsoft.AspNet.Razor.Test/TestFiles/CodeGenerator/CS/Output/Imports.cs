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

    public class Imports
    {
        #line hidden
        public Imports()
        {
        }

        public override void Execute()
        {
            WriteLiteral("\r\n<p>Path\'s full type name is ");
            Write(
#line 5 "Imports.cshtml"
                             typeof(Path).FullName
#line default
#line hidden
            );
            WriteLiteral("</p>\r\n<p>Foo\'s actual full type name is ");
            Write(
#line 6 "Imports.cshtml"
                                   typeof(Foo).FullName
#line default
#line hidden
            );
            WriteLiteral("</p>");
        }
    }
}
