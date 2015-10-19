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
using Microsoft.Dnx.Runtime;

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
            BeginWriteAttribute("lang", " lang=\"", 429, "\"", 490, 1);
#line 21 "RuntimeInfoPage.cshtml"
WriteAttributeValue("", 436, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, 436, 54, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" xmlns=\"http://www.w3.org/1999/xhtml\">\r\n<head>\r\n    <meta charset=\"utf-8\" />\r\n   " +
" <title>");
#line 24 "RuntimeInfoPage.cshtml"
      Write(Resources.RuntimeInfoPage_Title);

#line default
#line hidden
            WriteLiteral("</title>\r\n    <style>\r\n        body {\r\n    font-family: 'Segoe UI', Tahoma, Arial, Helvetica, sans-serif;\r\n    font-size: .813em;\r\n    line-height: 1.4em;\r\n    color: #222;\r\n}\r\n\r\nh1, h2, h3, h4, h5, th {\r\n    font-weight: 100;\r\n}\r\n\r\nh1 {\r\n    color: #44525e;\r\n    margin: 15px 0 15px 0;\r\n}\r\n\r\nh2 {\r\n    margin: 10px 5px 0 0;\r\n}\r\n\r\ntable .even{\r\n    background-color: #f0f0f0;\r\n}\r\n\r\nth {\r\n    font-size: 16px;\r\n}\r\n\r\n\r\n\r\n    </style>\r" +
"\n</head>\r\n<body>\r\n    <h2>");
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
            WriteLiteral("</p>\r\n\r\n    <h2>");
#line 39 "RuntimeInfoPage.cshtml"
   Write(Resources.RuntimeInfoPage_Packages);

#line default
#line hidden
            WriteLiteral("</h2>\r\n");
#line 40 "RuntimeInfoPage.cshtml"
    

#line default
#line hidden

#line 40 "RuntimeInfoPage.cshtml"
     if (@Resources.RuntimeInfoPage_Packages == null)
    {

#line default
#line hidden

            WriteLiteral("        <h2>");
#line 42 "RuntimeInfoPage.cshtml"
       Write(Resources.RuntimeInfoPage_PackagesFail);

#line default
#line hidden
            WriteLiteral("</h2>\r\n");
#line 43 "RuntimeInfoPage.cshtml"
    }
    else
    {

#line default
#line hidden

            WriteLiteral("        <table>\r\n            <thead>\r\n                <tr>\r\n                    <" +
"th>");
#line 49 "RuntimeInfoPage.cshtml"
                   Write(Resources.RuntimeInfoPage_PackageNameColumnName);

#line default
#line hidden
            WriteLiteral("</th>\r\n                    <th>");
#line 50 "RuntimeInfoPage.cshtml"
                   Write(Resources.RuntimeInfoPage_PackageVersionColumnName);

#line default
#line hidden
            WriteLiteral("</th>\r\n                    <th>");
#line 51 "RuntimeInfoPage.cshtml"
                   Write(Resources.RuntimeInfoPage_PackagePathColumnName);

#line default
#line hidden
            WriteLiteral("</th>\r\n                </tr>\r\n            </thead>\r\n            <tbody>\r\n");
#line 55 "RuntimeInfoPage.cshtml"
            

#line default
#line hidden

#line 55 "RuntimeInfoPage.cshtml"
               bool even = false; 

#line default
#line hidden

            WriteLiteral("            ");
#line 56 "RuntimeInfoPage.cshtml"
             foreach (var package in Model.References.OrderBy(package => package.Name.ToLowerInvariant()))
            {

#line default
#line hidden

            WriteLiteral("                <tr");
            BeginWriteAttribute("class", " class=\"", 2209, "\"", 2237, 1);
#line 58 "RuntimeInfoPage.cshtml"
WriteAttributeValue("", 2217, even?"even":"odd", 2217, 20, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">\r\n                    <td>");
#line 59 "RuntimeInfoPage.cshtml"
                   Write(package.Name);

#line default
#line hidden
            WriteLiteral("</td>\r\n                    <td>");
#line 60 "RuntimeInfoPage.cshtml"
                   Write(package.Version);

#line default
#line hidden
            WriteLiteral("</td>\r\n                    <td>");
#line 61 "RuntimeInfoPage.cshtml"
                   Write(package.Path);

#line default
#line hidden
            WriteLiteral("</td>\r\n                </tr>\r\n");
#line 63 "RuntimeInfoPage.cshtml"
                

#line default
#line hidden

#line 63 "RuntimeInfoPage.cshtml"
                   even = !even; 

#line default
#line hidden

#line 63 "RuntimeInfoPage.cshtml"
                                  
            }

#line default
#line hidden

            WriteLiteral("            </tbody>\r\n        </table>\r\n");
#line 67 "RuntimeInfoPage.cshtml"
    }

#line default
#line hidden

            WriteLiteral("</body>\r\n</html>\r\n");
        }
        #pragma warning restore 1998
    }
}
