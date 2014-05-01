namespace Microsoft.AspNet.Diagnostics.Views
{
#line 1 "DiagnosticsPage.cshtml"
using System

#line default
#line hidden
    ;
#line 2 "DiagnosticsPage.cshtml"
using System.Globalization

#line default
#line hidden
    ;
    using System.Threading.Tasks;

    public class DiagnosticsPage : Microsoft.AspNet.Diagnostics.Views.BaseView
    {
        #line hidden
        public DiagnosticsPage()
        {
        }

        public override async Task ExecuteAsync()
        {
#line 3 "DiagnosticsPage.cshtml"
  
    Response.ContentType = "text/html";
    string error = Request.Query.Get("error");
    if (!string.IsNullOrWhiteSpace(error))
    {
        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "User requested error '{0}'", error));
    }

#line default
#line hidden

            WriteLiteral("\r\n<!DOCTYPE html>\r\n\r\n<html lang=\"en\" xmlns=\"http://www.w3.org/1999/xhtml\">\r\n<head" +
">\r\n    <meta charset=\"utf-8\" />\r\n    <title>");
            Write(
#line 16 "DiagnosticsPage.cshtml"
            Resources.DiagnosticsPageHtml_Title

#line default
#line hidden
            );

            WriteLiteral("</title>\r\n</head>\r\n<body>\r\n    <div class=\"main\">\r\n        <h1>");
            Write(
#line 20 "DiagnosticsPage.cshtml"
             Resources.DiagnosticsPageHtml_Title

#line default
#line hidden
            );

            WriteLiteral("</h1>\r\n        <p>");
            Write(
#line 21 "DiagnosticsPage.cshtml"
            Resources.DiagnosticsPageHtml_Information

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n    </div>\r\n    <div class=\"errors\">\r\n        <h2>");
            Write(
#line 24 "DiagnosticsPage.cshtml"
             Resources.DiagnosticsPageHtml_TestErrorSection

#line default
#line hidden
            );

            WriteLiteral("</h2>\r\n        <p><a");
            WriteAttribute("href", Tuple.Create(" href=\"", 767), Tuple.Create("\"", 858), 
            Tuple.Create(Tuple.Create("", 774), Tuple.Create<System.Object, System.Int32>(
#line 25 "DiagnosticsPage.cshtml"
                     Request.PathBase

#line default
#line hidden
            , 774), false), 
            Tuple.Create(Tuple.Create("", 791), Tuple.Create<System.Object, System.Int32>(
#line 25 "DiagnosticsPage.cshtml"
                                      Request.Path

#line default
#line hidden
            , 791), false), Tuple.Create(Tuple.Create("", 804), Tuple.Create("?error=", 804), true), 
            Tuple.Create(Tuple.Create("", 811), Tuple.Create<System.Object, System.Int32>(
#line 25 "DiagnosticsPage.cshtml"
                                                          Resources.DiagnosticsPageHtml_TestErrorMessage

#line default
#line hidden
            , 811), false));
            WriteLiteral(">throw InvalidOperationException</a></p>\r\n    </div>\r\n</body>\r\n</html>\r\n");
        }
    }
}
