namespace Microsoft.AspNet.Diagnostics.Views
{
#line 1 "RuntimeInfoPage.cshtml"
using System

#line default
#line hidden
    ;
#line 2 "RuntimeInfoPage.cshtml"
using System.Globalization

#line default
#line hidden
    ;
#line 3 "RuntimeInfoPage.cshtml"
using System.Linq

#line default
#line hidden
    ;
#line 4 "RuntimeInfoPage.cshtml"
using Microsoft.AspNet.Diagnostics

#line default
#line hidden
    ;
#line 5 "RuntimeInfoPage.cshtml"
using Microsoft.AspNet.Diagnostics.Views

#line default
#line hidden
    ;
#line 6 "RuntimeInfoPage.cshtml"
using Microsoft.Extensions.PlatformAbstractions;

#line default
#line hidden
    using System.Threading.Tasks;

    public class RuntimeInfoPage : Microsoft.AspNet.Diagnostics.Views.BaseView
    {
#line 9 "RuntimeInfoPage.cshtml"

    public RuntimeInfoPage(RuntimeInfoPageModel model)
    {
        Model = model;
    }

    public RuntimeInfoPageModel Model { get; set; }

#line default
#line hidden
        #line hidden
        public RuntimeInfoPage()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            WriteLiteral("\r\n");
#line 17 "RuntimeInfoPage.cshtml"
  
    Response.ContentType = "text/html; charset=utf-8";

#line default
#line hidden

            WriteLiteral("<!DOCTYPE html>\r\n<html");
            BeginWriteAttribute("lang", " lang=\"", 449, "\"", 510, 1);
#line 21 "RuntimeInfoPage.cshtml"
WriteAttributeValue("", 456, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, 456, 54, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" xmlns=\"http://www.w3.org/1999/xhtml\">\r\n<head>\r\n    <meta charset=\"utf-8\" />\r\n    <title>");
#line 24 "RuntimeInfoPage.cshtml"
      Write(Resources.RuntimeInfoPage_Title);

#line default
#line hidden
            WriteLiteral("</title>\r\n    <style>\r\n        <%$ include: RuntimeInfoPage.css % >\r\n    </style>\r\n</head>\r\n<body>\r\n    <h2>");
#line 30 "RuntimeInfoPage.cshtml"
   Write(Resources.RuntimeInfoPage_Environment);

#line default
#line hidden
            WriteLiteral("</h2>\r\n    <p>");
#line 31 "RuntimeInfoPage.cshtml"
  Write(Resources.RuntimeInfoPage_OperatingSystem);

#line default
#line hidden
            WriteLiteral(" ");
#line 31 "RuntimeInfoPage.cshtml"
                                              Write(string.IsNullOrWhiteSpace(Model.OperatingSystem) ? Resources.RuntimeInfoPage_OperatingSystemFail : Model.OperatingSystem);

#line default
#line hidden
            WriteLiteral("</p>\r\n\r\n    <p>");
#line 33 "RuntimeInfoPage.cshtml"
  Write(Resources.RuntimeInfoPage_RuntimeVersion);

#line default
#line hidden
            WriteLiteral(" ");
#line 33 "RuntimeInfoPage.cshtml"
                                             Write(string.IsNullOrWhiteSpace(Model.Version) ? Resources.RuntimeInfoPage_RuntimeVersionFail : Model.Version);

#line default
#line hidden
            WriteLiteral("</p>\r\n\r\n    <p>");
#line 35 "RuntimeInfoPage.cshtml"
  Write(Resources.RuntimeInfoPage_RuntimeArchitecture);

#line default
#line hidden
            WriteLiteral(" ");
#line 35 "RuntimeInfoPage.cshtml"
                                                  Write(string.IsNullOrWhiteSpace(Model.RuntimeArchitecture) ? Resources.RuntimeInfoPage_RuntimeArchitectureFail : Model.RuntimeArchitecture);

#line default
#line hidden
            WriteLiteral("</p>\r\n\r\n    <p>");
#line 37 "RuntimeInfoPage.cshtml"
  Write(Resources.RuntimeInfoPage_RuntimeType);

#line default
#line hidden
            WriteLiteral(" ");
#line 37 "RuntimeInfoPage.cshtml"
                                          Write(string.IsNullOrWhiteSpace(Model.RuntimeType) ? Resources.RuntimeInfoPage_RuntimeTypeFail : Model.RuntimeType);

#line default
#line hidden
            WriteLiteral("</p>\r\n</body>\r\n</html>\r\n");
        }
        #pragma warning restore 1998
    }
}
