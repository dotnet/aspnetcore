namespace Microsoft.AspNet.Diagnostics.Views
{
#line 1 "ErrorPage.cshtml"
using System

#line default
#line hidden
    ;
#line 2 "ErrorPage.cshtml"
using System.Globalization

#line default
#line hidden
    ;
#line 3 "ErrorPage.cshtml"
using System.Linq

#line default
#line hidden
    ;
#line 4 "ErrorPage.cshtml"
using Views

#line default
#line hidden
    ;
    using System.Threading.Tasks;

    public class ErrorPage : Microsoft.AspNet.Diagnostics.Views.BaseView
    {
#line 6 "ErrorPage.cshtml"

    public ErrorPageModel Model { get; set; }

#line default
#line hidden
        #line hidden
        public ErrorPage()
        {
        }

        public override async Task ExecuteAsync()
        {
#line 9 "ErrorPage.cshtml"
  
    Response.StatusCode = 500;
    // TODO: Response.ReasonPhrase = "Internal Server Error";
    Response.ContentType = "text/html";
    Response.ContentLength = null; // Clear any prior Content-Length
    string location = string.Empty;

#line default
#line hidden

            WriteLiteral("\r\n<!DOCTYPE html>\r\n<html");
            WriteAttribute("lang", Tuple.Create(" lang=\"", 415), Tuple.Create("\"", 476), 
            Tuple.Create(Tuple.Create("", 422), Tuple.Create<System.Object, System.Int32>(
#line 17 "ErrorPage.cshtml"
             CultureInfo.CurrentUICulture.TwoLetterISOLanguageName

#line default
#line hidden
            , 422), false));
            WriteLiteral(" xmlns=\"http://www.w3.org/1999/xhtml\">\r\n    <head>\r\n        <meta charset=\"utf-8\"" +
" />\r\n        <title>");
            Write(
#line 20 "ErrorPage.cshtml"
                Resources.ErrorPageHtml_Title

#line default
#line hidden
            );

            WriteLiteral("</title>\r\n        <style>\r\n            body {\r\n    font-family: 'Segoe UI', Tahoma, Arial, Helvetica, sans-serif;\r\n    font-size: .813em;\r\n    line-height: 1.4em;\r\n    color: #222;\r\n}\r\n\r\nh1, h2, h3, h4, h5 {\r\n    /*font-family: 'Segoe UI',Tahoma,Arial,Helvetica,sans-serif;*/\r\n    font-weight: 100;\r\n}\r\n\r\nh1 {\r\n    color: #44525e;\r\n    margin: 15px 0 15px 0;\r\n}\r\n\r\nh2 {\r\n    margin: 10px 5px 0 0;\r\n}\r\n\r\nh3 {\r\n    color: #363636;\r\n    margin: 5px 5px 0 0;\r\n}\r\n\r\ncode {\r\n    font-family: Consolas, \"Courier New\", courier, monospace;\r\n}\r\n\r\nbody .titleerror {\r\n    padding: 3px;\r\n}\r\n\r\nbody .location {\r\n    margin: 3px 0 10px 30px;\r\n}\r\n\r\n#header {\r\n    font-size: 18px;\r\n    padding-left: 0px;\r\n    padding-right: 0px;\r\n    padding-top: 15px;\r\n    padding-bottom: 15px;\r\n    border-top: 1px #ddd solid;\r\n    border-bottom: 1px #ddd solid;\r\n    margin-bottom: 0px;\r\n}\r\n\r\n    #header li {\r\n        display: inline;\r\n        margin: 5px;\r\n        padding: 5px;\r\n        color: #a0a0a0;\r\n        cursor: pointer;\r\n    }\r\n\r\n        #header li:hover {\r\n            background: #a9e4f9;\r\n            color: #fff;\r\n        }\r\n\r\n        #header li.selected {\r\n            background: #44c5f2;\r\n            color: #fff;\r\n        }\r\n\r\n#stackpage ul {\r\n    list-style: none;\r\n    padding-left: 0;\r\n    margin: 0;\r\n    /*border-bottom: 1px #ddd solid;*/\r\n}\r\n\r\n#stackpage .stackerror {\r\n    padding: 5px;\r\n    border-bottom: 1px #ddd solid;\r\n}\r\n\r\n    #stackpage .stackerror:hover {\r\n        background-color: #f0f0f0;\r\n    }\r\n\r\n#stackpage .frame:hover {\r\n    background-color: #f0f0f0;\r\n    text-decoration: none;\r\n}\r\n\r\n#stackpage .frame {\r\n    padding: 2px;\r\n    margin: 0 0 0 30px;\r\n    border-bottom: 1px #ddd solid;\r\n    cursor: pointer;\r\n}\r\n\r\n    #stackpage .frame h3 {\r\n        padding: 5px;\r\n        margin: 0;\r\n    }\r\n\r\n#stackpage .source {\r\n    padding: 0px;\r\n}\r\n\r\n    #stackpage .source ol li {\r\n        font-family: Consolas, \"Courier New\", courier, monospace;\r\n        white-space: pre;\r\n    }\r\n\r\n    #stackpage .source ol.highlight li {\r\n        /*color: #e22;*/\r\n        /*font-weight: bold;*/\r\n    }\r\n\r\n        #stackpage .source ol.highlight li span {\r\n            /*color: #000;*/\r\n        }\r\n\r\n#stackpage .frame:hover .source ol.highlight li span {\r\n    color: #fff;\r\n    background: #b20000;\r\n}\r\n\r\n#stackpage .source ol.collapsable li {\r\n    color: #888;\r\n}\r\n\r\n    #stackpage .source ol.collapsable li span {\r\n        color: #606060;\r\n    }\r\n\r\n.page table {\r\n    border-collapse: separate;\r\n    border-spacing: 0;\r\n    margin: 0 0 20px;\r\n}\r\n\r\n.page th {\r\n    vertical-align: bottom;\r\n    padding: 10px 5px 5px 5px;\r\n    font-weight: 400;\r\n    color: #a0a0a0;\r\n    text-align: left;\r\n}\r\n\r\n.page td {\r\n    padding: 3px 10px;\r\n}\r\n\r\n.page th, .page td {\r\n    border-right: 1px #ddd solid;\r\n    border-bottom: 1px #ddd solid;\r\n    border-left: 1px transparent solid;\r\n    border-top: 1px transparent solid;\r\n    box-sizing: border-box;\r\n}\r\n\r\n    .page th:last-child, .page td:last-child {\r\n        border-right: 1px transparent solid;\r\n    }\r\n\r\n    .page td.length {\r\n        text-align: right;\r\n    }\r\n\r\na {\r\n    color: #1ba1e2;\r\n    text-decoration: none;\r\n}\r\n\r\n    a:hover {\r\n        color: #13709e;\r\n        text-decoration: underline;\r\n    }\r\n\r\n        </s" +
"tyle>\r\n    </head>\r\n    <body>\r\n        <h1>");
            Write(
#line 26 "ErrorPage.cshtml"
             Resources.ErrorPageHtml_UnhandledException

#line default
#line hidden
            );

            WriteLiteral("</h1>\r\n");
#line 27 "ErrorPage.cshtml"
        

#line default
#line hidden

#line 27 "ErrorPage.cshtml"
         if (Model.Options.ShowExceptionDetails)
        {
            foreach (var errorDetail in Model.ErrorDetails)
            {

#line default
#line hidden

            WriteLiteral("                <h2 class=\"titleerror\">");
            Write(
#line 31 "ErrorPage.cshtml"
                                        errorDetail.Error.GetType().Name

#line default
#line hidden
            );

            WriteLiteral(": ");
            Write(
#line 31 "ErrorPage.cshtml"
                                                                           errorDetail.Error.Message

#line default
#line hidden
            );

            WriteLiteral("</h2>\r\n");
#line 32 "ErrorPage.cshtml"
                

#line default
#line hidden

#line 32 "ErrorPage.cshtml"
                  
                    StackFrame firstFrame = null;
                    firstFrame = errorDetail.StackFrames.FirstOrDefault();
                    if (firstFrame != null)
                    {
                        location = firstFrame.Function;
                    }/* TODO: TargetSite is not defined
                    else if (errorDetail.Error.TargetSite != null && errorDetail.Error.TargetSite.DeclaringType != null)
                    {
                        location = errorDetail.Error.TargetSite.DeclaringType.FullName + "." + errorDetail.Error.TargetSite.Name;
                    }*/
                

#line default
#line hidden

#line 43 "ErrorPage.cshtml"
                 
                if (!string.IsNullOrEmpty(location) && firstFrame != null && !string.IsNullOrEmpty(firstFrame.File))
                {

#line default
#line hidden

            WriteLiteral("                    <p class=\"location\">");
            Write(
#line 46 "ErrorPage.cshtml"
                                         location

#line default
#line hidden
            );

            WriteLiteral(" in <code");
            WriteAttribute("title", Tuple.Create(" title=\"", 1871), Tuple.Create("\"", 1895), 
            Tuple.Create(Tuple.Create("", 1879), Tuple.Create<System.Object, System.Int32>(
#line 46 "ErrorPage.cshtml"
                                                                   firstFrame.File

#line default
#line hidden
            , 1879), false));
            WriteLiteral(">");
            Write(
#line 46 "ErrorPage.cshtml"
                                                                                     System.IO.Path.GetFileName(firstFrame.File)

#line default
#line hidden
            );

            WriteLiteral("</code>, line ");
            Write(
#line 46 "ErrorPage.cshtml"
                                                                                                                                               firstFrame.Line

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n");
#line 47 "ErrorPage.cshtml"
                }
                else if (!string.IsNullOrEmpty(location))
                {

#line default
#line hidden

            WriteLiteral("                    <p class=\"location\">");
            Write(
#line 50 "ErrorPage.cshtml"
                                         location

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n");
#line 51 "ErrorPage.cshtml"
                }
                else
                {

#line default
#line hidden

            WriteLiteral("                    <p class=\"location\">");
            Write(
#line 54 "ErrorPage.cshtml"
                                         Resources.ErrorPageHtml_UnknownLocation

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n");
#line 55 "ErrorPage.cshtml"
                }
            }
        }
        else
        {

#line default
#line hidden

            WriteLiteral("            <h2>");
            Write(
#line 60 "ErrorPage.cshtml"
                 Resources.ErrorPageHtml_EnableShowExceptions

#line default
#line hidden
            );

            WriteLiteral("</h2>\r\n");
#line 61 "ErrorPage.cshtml"
        }

#line default
#line hidden

            WriteLiteral("        <ul id=\"header\">\r\n");
#line 63 "ErrorPage.cshtml"
            

#line default
#line hidden

#line 63 "ErrorPage.cshtml"
             if (Model.Options.ShowExceptionDetails)
            {

#line default
#line hidden

            WriteLiteral("                <li id=\"stack\" tabindex=\"1\" class=\"selected\">\r\n                  " +
"  ");
            Write(
#line 66 "ErrorPage.cshtml"
                     Resources.ErrorPageHtml_StackButton

#line default
#line hidden
            );

            WriteLiteral("\r\n                </li>\r\n");
#line 68 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("            ");
#line 69 "ErrorPage.cshtml"
             if (Model.Options.ShowQuery)
            {

#line default
#line hidden

            WriteLiteral("                <li id=\"query\" tabindex=\"2\">\r\n                    ");
            Write(
#line 72 "ErrorPage.cshtml"
                     Resources.ErrorPageHtml_QueryButton

#line default
#line hidden
            );

            WriteLiteral("\r\n                </li>\r\n");
#line 74 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("            ");
#line 75 "ErrorPage.cshtml"
             if (Model.Options.ShowCookies)
            {

#line default
#line hidden

            WriteLiteral("                <li id=\"cookies\" tabindex=\"3\">\r\n                    ");
            Write(
#line 78 "ErrorPage.cshtml"
                     Resources.ErrorPageHtml_CookiesButton

#line default
#line hidden
            );

            WriteLiteral("\r\n                </li>\r\n");
#line 80 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("            ");
#line 81 "ErrorPage.cshtml"
             if (Model.Options.ShowHeaders)
            {

#line default
#line hidden

            WriteLiteral("                <li id=\"headers\" tabindex=\"4\">\r\n                    ");
            Write(
#line 84 "ErrorPage.cshtml"
                     Resources.ErrorPageHtml_HeadersButton

#line default
#line hidden
            );

            WriteLiteral("\r\n                </li>\r\n");
#line 86 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("            ");
#line 87 "ErrorPage.cshtml"
             if (Model.Options.ShowEnvironment)
            {

#line default
#line hidden

            WriteLiteral("                <li id=\"environment\" tabindex=\"5\">\r\n                    ");
            Write(
#line 90 "ErrorPage.cshtml"
                     Resources.ErrorPageHtml_EnvironmentButton

#line default
#line hidden
            );

            WriteLiteral("\r\n                </li>\r\n");
#line 92 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("        </ul>\r\n");
#line 94 "ErrorPage.cshtml"
        

#line default
#line hidden

#line 94 "ErrorPage.cshtml"
         if (Model.Options.ShowExceptionDetails)
        {

#line default
#line hidden

            WriteLiteral("            <div id=\"stackpage\" class=\"page\">\r\n                <ul>\r\n");
#line 98 "ErrorPage.cshtml"
                    

#line default
#line hidden

#line 98 "ErrorPage.cshtml"
                       int tabIndex = 6; 

#line default
#line hidden

            WriteLiteral("\r\n");
#line 99 "ErrorPage.cshtml"
                    

#line default
#line hidden

#line 99 "ErrorPage.cshtml"
                     foreach (var errorDetail in Model.ErrorDetails)
                    {

#line default
#line hidden

            WriteLiteral("                        <li>\r\n                            <h2 class=\"stackerror\">" +
"");
            Write(
#line 102 "ErrorPage.cshtml"
                                                    errorDetail.Error.GetType().Name

#line default
#line hidden
            );

            WriteLiteral(": ");
            Write(
#line 102 "ErrorPage.cshtml"
                                                                                       errorDetail.Error.Message

#line default
#line hidden
            );

            WriteLiteral("</h2>\r\n                            <ul>\r\n");
#line 104 "ErrorPage.cshtml"
                            

#line default
#line hidden

#line 104 "ErrorPage.cshtml"
                             foreach (var frame in errorDetail.StackFrames)
                            {

#line default
#line hidden

            WriteLiteral("                                <li class=\"frame\"");
            WriteAttribute("tabindex", Tuple.Create(" tabindex=\"", 4130), Tuple.Create("\"", 4150), 
            Tuple.Create(Tuple.Create("", 4141), Tuple.Create<System.Object, System.Int32>(
#line 106 "ErrorPage.cshtml"
                                                             tabIndex

#line default
#line hidden
            , 4141), false));
            WriteLiteral(">\r\n");
#line 107 "ErrorPage.cshtml"
                                    

#line default
#line hidden

#line 107 "ErrorPage.cshtml"
                                       tabIndex++; 

#line default
#line hidden

            WriteLiteral("\r\n");
#line 108 "ErrorPage.cshtml"
                                    

#line default
#line hidden

#line 108 "ErrorPage.cshtml"
                                     if (string.IsNullOrEmpty(frame.File))
                                    {

#line default
#line hidden

            WriteLiteral("                                        <h3>");
            Write(
#line 110 "ErrorPage.cshtml"
                                             frame.Function

#line default
#line hidden
            );

            WriteLiteral("</h3>\r\n");
#line 111 "ErrorPage.cshtml"
                                    }
                                    else
                                    {

#line default
#line hidden

            WriteLiteral("                                        <h3>");
            Write(
#line 114 "ErrorPage.cshtml"
                                             frame.Function

#line default
#line hidden
            );

            WriteLiteral(" in <code");
            WriteAttribute("title", Tuple.Create(" title=\"", 4577), Tuple.Create("\"", 4596), 
            Tuple.Create(Tuple.Create("", 4585), Tuple.Create<System.Object, System.Int32>(
#line 114 "ErrorPage.cshtml"
                                                                             frame.File

#line default
#line hidden
            , 4585), false));
            WriteLiteral(">");
            Write(
#line 114 "ErrorPage.cshtml"
                                                                                          System.IO.Path.GetFileName(frame.File)

#line default
#line hidden
            );

            WriteLiteral("</code></h3>\r\n");
#line 115 "ErrorPage.cshtml"
                                    }

#line default
#line hidden

            WriteLiteral("\r\n");
#line 117 "ErrorPage.cshtml"
                                    

#line default
#line hidden

#line 117 "ErrorPage.cshtml"
                                     if (frame.Line != 0 && frame.ContextCode != null)
                                    {

#line default
#line hidden

            WriteLiteral("                                        <div class=\"source\">\r\n");
#line 120 "ErrorPage.cshtml"
                                            

#line default
#line hidden

#line 120 "ErrorPage.cshtml"
                                             if (frame.PreContextCode != null)
                                            {

#line default
#line hidden

            WriteLiteral("                                                <ol");
            WriteAttribute("start", Tuple.Create(" start=\"", 5059), Tuple.Create("\"", 5088), 
            Tuple.Create(Tuple.Create("", 5067), Tuple.Create<System.Object, System.Int32>(
#line 122 "ErrorPage.cshtml"
                                                            frame.PreContextLine

#line default
#line hidden
            , 5067), false));
            WriteLiteral(" class=\"collapsible\">\r\n");
#line 123 "ErrorPage.cshtml"
                                                    

#line default
#line hidden

#line 123 "ErrorPage.cshtml"
                                                     foreach (var line in frame.PreContextCode)
                                                    {

#line default
#line hidden

            WriteLiteral("                                                        <li><span>");
            Write(
#line 125 "ErrorPage.cshtml"
                                                                   line

#line default
#line hidden
            );

            WriteLiteral("</span></li>\r\n");
#line 126 "ErrorPage.cshtml"
                                                    }

#line default
#line hidden

            WriteLiteral("                                                </ol>\r\n");
#line 128 "ErrorPage.cshtml"
                                            } 

#line default
#line hidden

            WriteLiteral("\r\n                                            <ol");
            WriteAttribute("start", Tuple.Create(" start=\"", 5556), Tuple.Create("\"", 5575), 
            Tuple.Create(Tuple.Create("", 5564), Tuple.Create<System.Object, System.Int32>(
#line 130 "ErrorPage.cshtml"
                                                        frame.Line

#line default
#line hidden
            , 5564), false));
            WriteLiteral(" class=\"highlight\">\r\n                                                <li><span>");
            Write(
#line 131 "ErrorPage.cshtml"
                                                           frame.ContextCode

#line default
#line hidden
            );

            WriteLiteral("</span></li></ol>\r\n\r\n");
#line 133 "ErrorPage.cshtml"
                                            

#line default
#line hidden

#line 133 "ErrorPage.cshtml"
                                             if (frame.PostContextCode != null)
                                            {

#line default
#line hidden

            WriteLiteral("                                                <ol");
            WriteAttribute("start", Tuple.Create(" start=\'", 5873), Tuple.Create("\'", 5898), 
            Tuple.Create(Tuple.Create("", 5881), Tuple.Create<System.Object, System.Int32>(
#line 135 "ErrorPage.cshtml"
                                                             frame.Line + 1

#line default
#line hidden
            , 5881), false));
            WriteLiteral(" class=\"collapsible\">\r\n");
#line 136 "ErrorPage.cshtml"
                                                    

#line default
#line hidden

#line 136 "ErrorPage.cshtml"
                                                     foreach (var line in frame.PostContextCode)
                                                    {

#line default
#line hidden

            WriteLiteral("                                                        <li><span>");
            Write(
#line 138 "ErrorPage.cshtml"
                                                                   line

#line default
#line hidden
            );

            WriteLiteral("</span></li>\r\n");
#line 139 "ErrorPage.cshtml"
                                                    }

#line default
#line hidden

            WriteLiteral("                                                </ol>\r\n");
#line 141 "ErrorPage.cshtml"
                                            } 

#line default
#line hidden

            WriteLiteral("                                        </div>\r\n");
#line 143 "ErrorPage.cshtml"
                                    } 

#line default
#line hidden

            WriteLiteral("                                </li>\r\n");
#line 145 "ErrorPage.cshtml"
                            }

#line default
#line hidden

            WriteLiteral("                            </ul>\r\n                        </li>\r\n");
#line 148 "ErrorPage.cshtml"
                    }

#line default
#line hidden

            WriteLiteral("                </ul>\r\n            </div>\r\n");
#line 151 "ErrorPage.cshtml"
        }

#line default
#line hidden

            WriteLiteral("        ");
#line 152 "ErrorPage.cshtml"
         if (Model.Options.ShowQuery)
        {

#line default
#line hidden

            WriteLiteral("            <div id=\"querypage\" class=\"page\">\r\n");
#line 155 "ErrorPage.cshtml"
                

#line default
#line hidden

#line 155 "ErrorPage.cshtml"
                 if (Model.Query.Any())
                {

#line default
#line hidden

            WriteLiteral("                    <table>\r\n                        <thead>\r\n                   " +
"         <tr>\r\n                                <th>");
            Write(
#line 160 "ErrorPage.cshtml"
                                     Resources.ErrorPageHtml_VariableColumn

#line default
#line hidden
            );

            WriteLiteral("</th>\r\n                                <th>");
            Write(
#line 161 "ErrorPage.cshtml"
                                     Resources.ErrorPageHtml_ValueColumn

#line default
#line hidden
            );

            WriteLiteral("</th>\r\n                            </tr>\r\n                        </thead>\r\n     " +
"                   <tbody>\r\n");
#line 165 "ErrorPage.cshtml"
                            

#line default
#line hidden

#line 165 "ErrorPage.cshtml"
                             foreach (var kv in Model.Query.OrderBy(kv => kv.Key))
                            {
                                foreach (var v in kv.Value)
                                {

#line default
#line hidden

            WriteLiteral("                                    <tr>\r\n                                       " +
" <td>");
            Write(
#line 170 "ErrorPage.cshtml"
                                             kv.Key

#line default
#line hidden
            );

            WriteLiteral("</td>\r\n                                        <td>");
            Write(
#line 171 "ErrorPage.cshtml"
                                             v

#line default
#line hidden
            );

            WriteLiteral("</td>\r\n                                    </tr>\r\n");
#line 173 "ErrorPage.cshtml"
                                }
                            }

#line default
#line hidden

            WriteLiteral("                        </tbody>\r\n                    </table>\r\n");
#line 177 "ErrorPage.cshtml"
                }
                else
                {

#line default
#line hidden

            WriteLiteral("                    <p>");
            Write(
#line 180 "ErrorPage.cshtml"
                        Resources.ErrorPageHtml_NoQueryStringData

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n");
#line 181 "ErrorPage.cshtml"
                }

#line default
#line hidden

            WriteLiteral("            </div>\r\n");
#line 183 "ErrorPage.cshtml"
        }

#line default
#line hidden

            WriteLiteral("        ");
#line 184 "ErrorPage.cshtml"
         if (Model.Options.ShowCookies)
        {
            /* TODO:
            <div id="cookiespage" class="page">
                @if (Model.Cookies.Any())
                {
                    <table>
                        <thead>
                            <tr>
                                <th>@Resources.ErrorPageHtml_VariableColumn</th>
                                <th>@Resources.ErrorPageHtml_ValueColumn</th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var kv in Model.Cookies.OrderBy(kv => kv.Key))
                            {
                                <tr>
                                    <td>@kv.Key</td>
                                    <td>@kv.Value</td>
                                </tr>
                            }
                        </tbody>
                    </table>
                }
                else
                {
                    <p>@Resources.ErrorPageHtml_NoCookieData</p>
                }
            </div>
             */
        }

#line default
#line hidden

            WriteLiteral("        ");
#line 215 "ErrorPage.cshtml"
         if (Model.Options.ShowHeaders)
        {

#line default
#line hidden

            WriteLiteral("            <div id=\"headerspage\" class=\"page\">\r\n");
#line 218 "ErrorPage.cshtml"
                

#line default
#line hidden

#line 218 "ErrorPage.cshtml"
                 if (Model.Headers.Any())
                {

#line default
#line hidden

            WriteLiteral("                    <table>\r\n                        <thead>\r\n                   " +
"         <tr>\r\n                                <th>");
            Write(
#line 223 "ErrorPage.cshtml"
                                     Resources.ErrorPageHtml_VariableColumn

#line default
#line hidden
            );

            WriteLiteral("</th>\r\n                                <th>");
            Write(
#line 224 "ErrorPage.cshtml"
                                     Resources.ErrorPageHtml_ValueColumn

#line default
#line hidden
            );

            WriteLiteral("</th>\r\n                            </tr>\r\n                        </thead>\r\n     " +
"                   <tbody>\r\n");
#line 228 "ErrorPage.cshtml"
                            

#line default
#line hidden

#line 228 "ErrorPage.cshtml"
                             foreach (var kv in Model.Headers.OrderBy(kv => kv.Key))
                            {
                                foreach (var v in kv.Value)
                                {

#line default
#line hidden

            WriteLiteral("                                    <tr>\r\n                                       " +
" <td>");
            Write(
#line 233 "ErrorPage.cshtml"
                                             kv.Key

#line default
#line hidden
            );

            WriteLiteral("</td>\r\n                                        <td>");
            Write(
#line 234 "ErrorPage.cshtml"
                                             v

#line default
#line hidden
            );

            WriteLiteral("</td>\r\n                                    </tr>\r\n");
#line 236 "ErrorPage.cshtml"
                                }
                            }

#line default
#line hidden

            WriteLiteral("                        </tbody>\r\n                    </table>\r\n");
#line 240 "ErrorPage.cshtml"
                }
                else
                {

#line default
#line hidden

            WriteLiteral("                    <p>");
            Write(
#line 243 "ErrorPage.cshtml"
                        Resources.ErrorPageHtml_NoHeaderData

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n");
#line 244 "ErrorPage.cshtml"
                }

#line default
#line hidden

            WriteLiteral("            </div>\r\n");
#line 246 "ErrorPage.cshtml"
        }

#line default
#line hidden

            WriteLiteral("        ");
#line 247 "ErrorPage.cshtml"
         if (Model.Options.ShowEnvironment)
        {
            /* TODO:
            <div id="environmentpage" class="page">
                <table>
                    <thead>
                        <tr>
                            <th>@Resources.ErrorPageHtml_VariableColumn</th>
                            <th>@Resources.ErrorPageHtml_ValueColumn</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var kv in Model.Environment.OrderBy(kv => kv.Key))
                        {
                            <tr>
                                <td>@kv.Key</td>
                                <td>@kv.Value</td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>
             */
        }

#line default
#line hidden

            WriteLiteral("        <script>\r\n            //<!--\r\n            (function (window, undefined) {\r\n    \"use strict\";\r\n\r\n    function $(selector, element) {\r\n        return new NodeCollection(selector, element);\r\n    }\r\n\r\n    function NodeCollection(selector, element) {\r\n        this.items = [];\r\n        element = element || window.document;\r\n\r\n        var nodeList;\r\n\r\n        if (typeof (selector) === \"string\") {\r\n            nodeList = element.querySelectorAll(selector);\r\n            for (var i = 0, l = nodeList.length; i < l; i++) {\r\n                this.items.push(nodeList.item(i));\r\n            }\r\n        } else if (selector.tagName) {\r\n            this.items.push(selector);\r\n        } else if (selector.splice) {\r\n            this.items = this.items.concat(selector);\r\n        }\r\n    }\r\n\r\n    NodeCollection.prototype = {\r\n        each: function (callback) {\r\n            for (var i = 0, l = this.items.length; i < l; i++) {\r\n                callback(this.items[i], i);\r\n            }\r\n            return this;\r\n        },\r\n\r\n        children: function (selector) {\r\n            var children = [];\r\n\r\n            this.each(function (el) {\r\n                children = children.concat($(selector, el).items);\r\n            });\r\n\r\n            return $(children);\r\n        },\r\n\r\n        hide: function () {\r\n            this.each(function (el) {\r\n                el.style.display = \"none\";\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        toggle: function () {\r\n            this.each(function (el) {\r\n                el.style.display = el.style.display === \"none\" ? \"\" : \"none\";\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        show: function () {\r\n            this.each(function (el) {\r\n                el.style.display = \"\";\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        addClass: function (className) {\r\n            this.each(function (el) {\r\n                var existingClassName = el.className,\r\n                    classNames;\r\n                if (!existingClassName) {\r\n                    el.className = className;\r\n                } else {\r\n                    classNames = existingClassName.split(\" \");\r\n                    if (classNames.indexOf(className) < 0) {\r\n                        el.className = existingClassName + \" \" + className;\r\n                    }\r\n                }\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        removeClass: function (className) {\r\n            this.each(function (el) {\r\n                var existingClassName = el.className,\r\n                    classNames, index;\r\n                if (existingClassName === className) {\r\n                    el.className = \"\";\r\n                } else if (existingClassName) {\r\n                    classNames = existingClassName.split(\" \");\r\n                    index = classNames.indexOf(className);\r\n                    if (index > 0) {\r\n                        classNames.splice(index, 1);\r\n                        el.className = classNames.join(\" \");\r\n                    }\r\n                }\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        toggleClass: function (className) {\r\n            this.each(function (el) {\r\n                var classNames = el.className.split(\" \");\r\n                if (classNames.indexOf(className) >= 0) {\r\n                    $(el).removeClass(className);\r\n                } else {\r\n                    $(el).addClass(className);\r\n                }\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        attr: function (name) {\r\n            if (this.items.length === 0) {\r\n                return null;\r\n            }\r\n\r\n            return this.items[0].getAttribute(name);\r\n        },\r\n\r\n        on: function (eventName, handler) {\r\n            this.each(function (el, idx) {\r\n                var callback = function (e) {\r\n                    e = e || window.event;\r\n                    if (!e.which && e.keyCode) {\r\n                        e.which = e.keyCode; // Normalize IE8 key events\r\n                    }\r\n                    handler.apply(el, [e]);\r\n                };\r\n\r\n                if (el.addEventListener) { // DOM Events\r\n                    el.addEventListener(eventName, callback, false);\r\n                } else if (el.attachEvent) { // IE8 events\r\n                    el.attachEvent('on' + eventName, callback)\r\n                } else {\r\n                    el['on' + type] = callback;\r\n                }\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        click: function (handler) {\r\n            return this.on(\"click\", handler);\r\n        },\r\n\r\n        keypress: function (handler) {\r\n            return this.on(\"keypress\", handler);\r\n        }\r\n    };\r\n\r\n    function frame(el) {\r\n        $(el).children(\".source .collapsible\").toggle();\r\n    }\r\n\r\n    function tab(el) {\r\n        var unselected = $(\"#header .selected\").removeClass(\"selected\").attr(\"id\");\r\n        var selected = $(el).addClass(\"selected\").attr(\"id\");\r\n\r\n        $(\"#\" + unselected + \"page\").hide();\r\n        $(\"#\" + selected + \"page\").show();\r\n    }\r\n\r\n    $(\".collapsible\").hide();\r\n    $(\".page\").hide();\r\n    $(\"#stackpage\").show();\r\n\r\n    $(\".frame\")\r\n        .click(function () {\r\n            frame(this);\r\n        })\r\n        .keypress(function (e) {\r\n            if (e.which === 13) {\r\n                frame(this);\r\n            }\r\n        });\r\n\r\n    $(\"#header li\")\r\n        .click(function () {\r\n            tab(this)\r\n        })\r\n        .keypress(function (e) {\r\n            if (e.which === 13) {\r\n                tab(this);\r\n            }\r\n        });\r\n})(window);\r\n " +
"           //-->\r\n        </script>\r\n    </body>\r\n</html>\r\n");
        }
    }
}
