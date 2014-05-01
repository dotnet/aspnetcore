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

    /// <summary>
    /// 
    /// </summary>
    public ErrorPageModel Model { get; set; }

#line default
#line hidden
        #line hidden
        public ErrorPage()
        {
        }

        public override async Task ExecuteAsync()
        {
#line 12 "ErrorPage.cshtml"
  
    Response.StatusCode = 500;
    // TODO: Response.ReasonPhrase = "Internal Server Error";
    Response.ContentType = "text/html";
    Response.ContentLength = null; // Clear any prior Content-Length
    string location = string.Empty;

#line default
#line hidden

            WriteLiteral("\r\n<!DOCTYPE html>\r\n<html");
            WriteAttribute("lang", Tuple.Create(" lang=\"", 464), Tuple.Create("\"", 525), 
            Tuple.Create(Tuple.Create("", 471), Tuple.Create<System.Object, System.Int32>(
#line 20 "ErrorPage.cshtml"
             CultureInfo.CurrentUICulture.TwoLetterISOLanguageName

#line default
#line hidden
            , 471), false));
            WriteLiteral(" xmlns=\"http://www.w3.org/1999/xhtml\">\r\n    <head>\r\n        <meta charset=\"utf-8\"" +
" />\r\n        <title>");
            Write(
#line 23 "ErrorPage.cshtml"
                Resources.ErrorPageHtml_Title

#line default
#line hidden
            );

            WriteLiteral("</title>\r\n        <style>\r\n            body {\r\n    font-family: 'Segoe UI',Tahoma,Arial,Helvetica,sans-serif;\r\n    font-size: .813em;\r\n    line-height: 1.4em;\r\n    color: #222;\r\n}\r\n\r\nh1, h2, h3, h4, h5 {\r\n    /*font-family: 'Segoe UI',Tahoma,Arial,Helvetica,sans-serif;*/\r\n    font-weight: 100;\r\n}\r\n\r\nh1 {\r\n    color: #44525e;\r\n    margin: 15px 0 15px 0;\r\n}\r\n\r\nh2 {\r\n    margin: 10px 5px 0 0;\r\n}\r\n\r\nh3 {\r\n    color: #363636;\r\n    margin: 5px 5px 0 0;\r\n}\r\n\r\ncode {\r\n    font-family: consolas, \"Courier New\", courier, monospace;\r\n}\r\n\r\nbody .titleerror {\r\n    padding: 3px;\r\n}\r\n\r\nbody .location {\r\n    margin: 3px 0 10px 30px;\r\n}\r\n\r\n#header {\r\n    font-size: 18px;\r\n    padding-left: 0px;\r\n    padding-right: 0px;\r\n    padding-top: 15px;\r\n    padding-bottom: 15px;\r\n    border-top: 1px #ddd solid;\r\n    border-bottom: 1px #ddd solid;\r\n    margin-bottom: 0px;\r\n}\r\n\r\n#header li {\r\n    display: inline;\r\n    margin: 5px;\r\n    padding: 5px;\r\n    color: #a0a0a0;\r\n}\r\n\r\n#header li:hover {\r\n    background: #A9E4F9;\r\n    color: #fff;\r\n}\r\n\r\n#header li.selected {\r\n    background: #44C5F2;\r\n    color: #fff;\r\n}\r\n\r\n#stackpage ul {\r\n    list-style: none;\r\n    padding-left: 0;\r\n    margin: 0;\r\n    /*border-bottom: 1px #ddd solid;*/\r\n}\r\n\r\n#stackpage .stackerror {\r\n    padding: 5px;\r\n    border-bottom: 1px #ddd solid;\r\n}\r\n\r\n#stackpage .stackerror:hover {\r\n    background-color: #f0f0f0;\r\n}\r\n\r\n#stackpage .frame:hover {\r\n    background-color: #f0f0f0;\r\n    text-decoration: none;\r\n}\r\n\r\n#stackpage .frame {\r\n    padding: 2px;\r\n    margin: 0 0 0 30px;\r\n    border-bottom: 1px #ddd solid;\r\n}\r\n\r\n#stackpage .frame h3 {\r\n    padding: 5px;\r\n    margin: 0;\r\n}\r\n\r\n#stackpage .source {\r\n    padding: 0px;\r\n}\r\n\r\n#stackpage .source ol li {\r\n    font-family: consolas, \"Courier New\", courier, monospace;\r\n    white-space: pre;\r\n}\r\n\r\n#stackpage .source ol.highlight li {\r\n    /*color: #e22;*/\r\n    /*font-weight: bold;*/\r\n}\r\n\r\n#stackpage .source ol.highlight li span {\r\n    /*color: #000;*/\r\n}\r\n\r\n#stackpage .frame:hover .source ol.highlight li span {\r\n    color: #fff;\r\n    background: #B20000;\r\n}\r\n\r\n#stackpage .source ol.collapsable li {\r\n    color: #888;\r\n}\r\n\r\n#stackpage .source ol.collapsable li span {\r\n    color: #606060;\r\n}\r\n\r\n.page table {\r\n    border-collapse: separate;\r\n    border-spacing: 0;\r\n    margin: 0 0 20px;\r\n}\r\n\r\n.page th {\r\n    vertical-align: bottom;\r\n    padding: 10px 5px 5px 5px;\r\n    font-weight: 400;\r\n    color: #a0a0a0;\r\n    text-align: left;\r\n}\r\n\r\n.page td {\r\n    padding: 3px 10px;\r\n}\r\n\r\n.page th, .page td {\r\n    border-right: 1px #ddd solid;\r\n    border-bottom: 1px #ddd solid;\r\n    border-left: 1px transparent solid;\r\n    border-top: 1px transparent solid;\r\n    box-sizing: border-box;\r\n}\r\n\r\n.page th:last-child, .page td:last-child {\r\n    border-right: 1px transparent solid;\r\n}\r\n\r\n.page td.length {\r\n    text-align: right;\r\n}\r\n\r\na {\r\n    color: #1ba1e2;\r\n    text-decoration: none;\r\n}\r\n\r\na:hover {\r\n    color: #13709e;\r\n    text-decoration: underline;\r\n}\r\n\r\n        </s" +
"tyle>\r\n    </head>\r\n    <body>\r\n        Hello\r\n        <h1>");
            Write(
#line 30 "ErrorPage.cshtml"
             Resources.ErrorPageHtml_UnhandledException

#line default
#line hidden
            );

            WriteLiteral("</h1>\r\n");
#line 31 "ErrorPage.cshtml"
        

#line default
#line hidden

#line 31 "ErrorPage.cshtml"
         if (Model.Options.ShowExceptionDetails)
        {
            foreach (var errorDetail in Model.ErrorDetails)
            {

#line default
#line hidden

            WriteLiteral("                <h2 class=\"titleerror\">");
            Write(
#line 35 "ErrorPage.cshtml"
                                        errorDetail.Error.GetType().Name

#line default
#line hidden
            );

            WriteLiteral(": ");
            Write(
#line 35 "ErrorPage.cshtml"
                                                                           errorDetail.Error.Message

#line default
#line hidden
            );

            WriteLiteral("</h2>\r\n");
#line 36 "ErrorPage.cshtml"
                

#line default
#line hidden

#line 36 "ErrorPage.cshtml"
                  
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

#line 47 "ErrorPage.cshtml"
                 
                if (!string.IsNullOrEmpty(location) && firstFrame != null && !string.IsNullOrEmpty(firstFrame.File))
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

            WriteLiteral(" in <code");
            WriteAttribute("title", Tuple.Create(" title=\"", 1935), Tuple.Create("\"", 1959), 
            Tuple.Create(Tuple.Create("", 1943), Tuple.Create<System.Object, System.Int32>(
#line 50 "ErrorPage.cshtml"
                                                                   firstFrame.File

#line default
#line hidden
            , 1943), false));
            WriteLiteral(">");
            Write(
#line 50 "ErrorPage.cshtml"
                                                                                     System.IO.Path.GetFileName(firstFrame.File)

#line default
#line hidden
            );

            WriteLiteral("</code>, line ");
            Write(
#line 50 "ErrorPage.cshtml"
                                                                                                                                               firstFrame.Line

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n");
#line 51 "ErrorPage.cshtml"
                }
                else if (!string.IsNullOrEmpty(location))
                {

#line default
#line hidden

            WriteLiteral("                    <p class=\"location\">");
            Write(
#line 54 "ErrorPage.cshtml"
                                         location

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n");
#line 55 "ErrorPage.cshtml"
                }
                else
                {

#line default
#line hidden

            WriteLiteral("                    <p class=\"location\">");
            Write(
#line 58 "ErrorPage.cshtml"
                                         Resources.ErrorPageHtml_UnknownLocation

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n");
#line 59 "ErrorPage.cshtml"
                }
            }
        }
        else
        {

#line default
#line hidden

            WriteLiteral("            <h2>");
            Write(
#line 64 "ErrorPage.cshtml"
                 Resources.ErrorPageHtml_EnableShowExceptions

#line default
#line hidden
            );

            WriteLiteral("</h2>\r\n");
#line 65 "ErrorPage.cshtml"
        }

#line default
#line hidden

            WriteLiteral("        <ul id=\"header\">\r\n");
#line 67 "ErrorPage.cshtml"
            

#line default
#line hidden

#line 67 "ErrorPage.cshtml"
             if (Model.Options.ShowExceptionDetails)
            {

#line default
#line hidden

            WriteLiteral("                <li id=\"stack\" tabindex=\"1\" class=\"selected\">\r\n                  " +
"  ");
            Write(
#line 70 "ErrorPage.cshtml"
                     Resources.ErrorPageHtml_StackButton

#line default
#line hidden
            );

            WriteLiteral("\r\n                </li>\r\n");
#line 72 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("            ");
#line 73 "ErrorPage.cshtml"
             if (Model.Options.ShowQuery)
            {

#line default
#line hidden

            WriteLiteral("                <li id=\"query\" tabindex=\"2\">\r\n                    ");
            Write(
#line 76 "ErrorPage.cshtml"
                     Resources.ErrorPageHtml_QueryButton

#line default
#line hidden
            );

            WriteLiteral("\r\n                </li>\r\n");
#line 78 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("            ");
#line 79 "ErrorPage.cshtml"
             if (Model.Options.ShowCookies)
            {

#line default
#line hidden

            WriteLiteral("                <li id=\"cookies\" tabindex=\"3\">\r\n                    ");
            Write(
#line 82 "ErrorPage.cshtml"
                     Resources.ErrorPageHtml_CookiesButton

#line default
#line hidden
            );

            WriteLiteral("\r\n                </li>\r\n");
#line 84 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("            ");
#line 85 "ErrorPage.cshtml"
             if (Model.Options.ShowHeaders)
            {

#line default
#line hidden

            WriteLiteral("                <li id=\"headers\" tabindex=\"4\">\r\n                    ");
            Write(
#line 88 "ErrorPage.cshtml"
                     Resources.ErrorPageHtml_HeadersButton

#line default
#line hidden
            );

            WriteLiteral("\r\n                </li>\r\n");
#line 90 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("            ");
#line 91 "ErrorPage.cshtml"
             if (Model.Options.ShowEnvironment)
            {

#line default
#line hidden

            WriteLiteral("                <li id=\"environment\" tabindex=\"5\">\r\n                    ");
            Write(
#line 94 "ErrorPage.cshtml"
                     Resources.ErrorPageHtml_EnvironmentButton

#line default
#line hidden
            );

            WriteLiteral("\r\n                </li>\r\n");
#line 96 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("        </ul>\r\n");
#line 98 "ErrorPage.cshtml"
        

#line default
#line hidden

#line 98 "ErrorPage.cshtml"
         if (Model.Options.ShowExceptionDetails)
        {

#line default
#line hidden

            WriteLiteral("            <div id=\"stackpage\" class=\"page\">\r\n                <ul>\r\n");
#line 102 "ErrorPage.cshtml"
                    

#line default
#line hidden

#line 102 "ErrorPage.cshtml"
                       int tabIndex = 6; 

#line default
#line hidden

            WriteLiteral("\r\n");
#line 103 "ErrorPage.cshtml"
                    

#line default
#line hidden

#line 103 "ErrorPage.cshtml"
                     foreach (var errorDetail in Model.ErrorDetails)
                    {

#line default
#line hidden

            WriteLiteral("                        <li>\r\n                            <h2 class=\"stackerror\">" +
"");
            Write(
#line 106 "ErrorPage.cshtml"
                                                    errorDetail.Error.GetType().Name

#line default
#line hidden
            );

            WriteLiteral(": ");
            Write(
#line 106 "ErrorPage.cshtml"
                                                                                       errorDetail.Error.Message

#line default
#line hidden
            );

            WriteLiteral("</h2>\r\n                            <ul>\r\n");
#line 108 "ErrorPage.cshtml"
                            

#line default
#line hidden

#line 108 "ErrorPage.cshtml"
                             foreach (var frame in errorDetail.StackFrames)
                            {

#line default
#line hidden

            WriteLiteral("                                <li class=\"frame\"");
            WriteAttribute("tabindex", Tuple.Create(" tabindex=\"", 4194), Tuple.Create("\"", 4214), 
            Tuple.Create(Tuple.Create("", 4205), Tuple.Create<System.Object, System.Int32>(
#line 110 "ErrorPage.cshtml"
                                                             tabIndex

#line default
#line hidden
            , 4205), false));
            WriteLiteral(">\r\n");
#line 111 "ErrorPage.cshtml"
                                    

#line default
#line hidden

#line 111 "ErrorPage.cshtml"
                                       tabIndex++; 

#line default
#line hidden

            WriteLiteral("\r\n");
#line 112 "ErrorPage.cshtml"
                                    

#line default
#line hidden

#line 112 "ErrorPage.cshtml"
                                     if (string.IsNullOrEmpty(frame.File))
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

            WriteLiteral("</h3>\r\n");
#line 115 "ErrorPage.cshtml"
                                    }
                                    else
                                    {

#line default
#line hidden

            WriteLiteral("                                        <h3>");
            Write(
#line 118 "ErrorPage.cshtml"
                                             frame.Function

#line default
#line hidden
            );

            WriteLiteral(" in <code");
            WriteAttribute("title", Tuple.Create(" title=\"", 4641), Tuple.Create("\"", 4660), 
            Tuple.Create(Tuple.Create("", 4649), Tuple.Create<System.Object, System.Int32>(
#line 118 "ErrorPage.cshtml"
                                                                             frame.File

#line default
#line hidden
            , 4649), false));
            WriteLiteral(">");
            Write(
#line 118 "ErrorPage.cshtml"
                                                                                          System.IO.Path.GetFileName(frame.File)

#line default
#line hidden
            );

            WriteLiteral("</code></h3>\r\n");
#line 119 "ErrorPage.cshtml"
                                    }

#line default
#line hidden

            WriteLiteral("\r\n");
#line 121 "ErrorPage.cshtml"
                                    

#line default
#line hidden

#line 121 "ErrorPage.cshtml"
                                     if (frame.Line != 0 && frame.ContextCode != null)
                                    {

#line default
#line hidden

            WriteLiteral("                                        <div class=\"source\">\r\n");
#line 124 "ErrorPage.cshtml"
                                            

#line default
#line hidden

#line 124 "ErrorPage.cshtml"
                                             if (frame.PreContextCode != null)
                                            {

#line default
#line hidden

            WriteLiteral("                                                <ol");
            WriteAttribute("start", Tuple.Create(" start=\"", 5123), Tuple.Create("\"", 5152), 
            Tuple.Create(Tuple.Create("", 5131), Tuple.Create<System.Object, System.Int32>(
#line 126 "ErrorPage.cshtml"
                                                            frame.PreContextLine

#line default
#line hidden
            , 5131), false));
            WriteLiteral(" class=\"collapsable\">\r\n");
#line 127 "ErrorPage.cshtml"
                                                    

#line default
#line hidden

#line 127 "ErrorPage.cshtml"
                                                     foreach (var line in frame.PreContextCode)
                                                    {

#line default
#line hidden

            WriteLiteral("                                                        <li><span>");
            Write(
#line 129 "ErrorPage.cshtml"
                                                                   line

#line default
#line hidden
            );

            WriteLiteral("</span></li>\r\n");
#line 130 "ErrorPage.cshtml"
                                                    }

#line default
#line hidden

            WriteLiteral("                                                </ol>\r\n");
#line 132 "ErrorPage.cshtml"
                                            } 

#line default
#line hidden

            WriteLiteral("\r\n                                            <ol");
            WriteAttribute("start", Tuple.Create(" start=\"", 5620), Tuple.Create("\"", 5639), 
            Tuple.Create(Tuple.Create("", 5628), Tuple.Create<System.Object, System.Int32>(
#line 134 "ErrorPage.cshtml"
                                                        frame.Line

#line default
#line hidden
            , 5628), false));
            WriteLiteral(" class=\"highlight\">\r\n                                                <li><span>");
            Write(
#line 135 "ErrorPage.cshtml"
                                                           frame.ContextCode

#line default
#line hidden
            );

            WriteLiteral("</span></li></ol>\r\n\r\n");
#line 137 "ErrorPage.cshtml"
                                            

#line default
#line hidden

#line 137 "ErrorPage.cshtml"
                                             if (frame.PostContextCode != null)
                                            {

#line default
#line hidden

            WriteLiteral("                                                <ol");
            WriteAttribute("start", Tuple.Create(" start=\'", 5937), Tuple.Create("\'", 5962), 
            Tuple.Create(Tuple.Create("", 5945), Tuple.Create<System.Object, System.Int32>(
#line 139 "ErrorPage.cshtml"
                                                             frame.Line + 1

#line default
#line hidden
            , 5945), false));
            WriteLiteral(" class=\"collapsable\">\r\n");
#line 140 "ErrorPage.cshtml"
                                                    

#line default
#line hidden

#line 140 "ErrorPage.cshtml"
                                                     foreach (var line in frame.PostContextCode)
                                                    {

#line default
#line hidden

            WriteLiteral("                                                        <li><span>");
            Write(
#line 142 "ErrorPage.cshtml"
                                                                   line

#line default
#line hidden
            );

            WriteLiteral("</span></li>\r\n");
#line 143 "ErrorPage.cshtml"
                                                    }

#line default
#line hidden

            WriteLiteral("                                                </ol>\r\n");
#line 145 "ErrorPage.cshtml"
                                            } 

#line default
#line hidden

            WriteLiteral("                                        </div>\r\n");
#line 147 "ErrorPage.cshtml"
                                    } 

#line default
#line hidden

            WriteLiteral("                                </li>\r\n");
#line 149 "ErrorPage.cshtml"
                            }

#line default
#line hidden

            WriteLiteral("                            </ul>\r\n                        </li>\r\n");
#line 152 "ErrorPage.cshtml"
                    }

#line default
#line hidden

            WriteLiteral("                </ul>\r\n            </div>\r\n");
#line 155 "ErrorPage.cshtml"
        }

#line default
#line hidden

            WriteLiteral("        ");
#line 156 "ErrorPage.cshtml"
         if (Model.Options.ShowQuery)
        {

#line default
#line hidden

            WriteLiteral("            <div id=\"querypage\" class=\"page\">\r\n");
#line 159 "ErrorPage.cshtml"
                

#line default
#line hidden

#line 159 "ErrorPage.cshtml"
                 if (Model.Query.Any())
                {

#line default
#line hidden

            WriteLiteral("                    <table>\r\n                        <thead>\r\n                   " +
"         <tr>\r\n                                <th>");
            Write(
#line 164 "ErrorPage.cshtml"
                                     Resources.ErrorPageHtml_VariableColumn

#line default
#line hidden
            );

            WriteLiteral("</th>\r\n                                <th>");
            Write(
#line 165 "ErrorPage.cshtml"
                                     Resources.ErrorPageHtml_ValueColumn

#line default
#line hidden
            );

            WriteLiteral("</th>\r\n                            </tr>\r\n                        </thead>\r\n     " +
"                   <tbody>\r\n");
#line 169 "ErrorPage.cshtml"
                            

#line default
#line hidden

#line 169 "ErrorPage.cshtml"
                             foreach (var kv in Model.Query.OrderBy(kv => kv.Key))
                            {
                                foreach (var v in kv.Value)
                                {

#line default
#line hidden

            WriteLiteral("                                    <tr>\r\n                                       " +
" <td>");
            Write(
#line 174 "ErrorPage.cshtml"
                                             kv.Key

#line default
#line hidden
            );

            WriteLiteral("</td>\r\n                                        <td>");
            Write(
#line 175 "ErrorPage.cshtml"
                                             v

#line default
#line hidden
            );

            WriteLiteral("</td>\r\n                                    </tr>\r\n");
#line 177 "ErrorPage.cshtml"
                                }
                            }

#line default
#line hidden

            WriteLiteral("                        </tbody>\r\n                    </table>\r\n");
#line 181 "ErrorPage.cshtml"
                }
                else
                {

#line default
#line hidden

            WriteLiteral("                    <p>");
            Write(
#line 184 "ErrorPage.cshtml"
                        Resources.ErrorPageHtml_NoQueryStringData

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n");
#line 185 "ErrorPage.cshtml"
                }

#line default
#line hidden

            WriteLiteral("            </div>\r\n");
#line 187 "ErrorPage.cshtml"
        }

#line default
#line hidden

            WriteLiteral("        ");
#line 188 "ErrorPage.cshtml"
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
#line 219 "ErrorPage.cshtml"
         if (Model.Options.ShowHeaders)
        {

#line default
#line hidden

            WriteLiteral("            <div id=\"headerspage\" class=\"page\">\r\n");
#line 222 "ErrorPage.cshtml"
                

#line default
#line hidden

#line 222 "ErrorPage.cshtml"
                 if (Model.Headers.Any())
                {

#line default
#line hidden

            WriteLiteral("                    <table>\r\n                        <thead>\r\n                   " +
"         <tr>\r\n                                <th>");
            Write(
#line 227 "ErrorPage.cshtml"
                                     Resources.ErrorPageHtml_VariableColumn

#line default
#line hidden
            );

            WriteLiteral("</th>\r\n                                <th>");
            Write(
#line 228 "ErrorPage.cshtml"
                                     Resources.ErrorPageHtml_ValueColumn

#line default
#line hidden
            );

            WriteLiteral("</th>\r\n                            </tr>\r\n                        </thead>\r\n     " +
"                   <tbody>\r\n");
#line 232 "ErrorPage.cshtml"
                            

#line default
#line hidden

#line 232 "ErrorPage.cshtml"
                             foreach (var kv in Model.Headers.OrderBy(kv => kv.Key))
                            {
                                foreach (var v in kv.Value)
                                {

#line default
#line hidden

            WriteLiteral("                                    <tr>\r\n                                       " +
" <td>");
            Write(
#line 237 "ErrorPage.cshtml"
                                             kv.Key

#line default
#line hidden
            );

            WriteLiteral("</td>\r\n                                        <td>");
            Write(
#line 238 "ErrorPage.cshtml"
                                             v

#line default
#line hidden
            );

            WriteLiteral("</td>\r\n                                    </tr>\r\n");
#line 240 "ErrorPage.cshtml"
                                }
                            }

#line default
#line hidden

            WriteLiteral("                        </tbody>\r\n                    </table>\r\n");
#line 244 "ErrorPage.cshtml"
                }
                else
                {

#line default
#line hidden

            WriteLiteral("                    <p>");
            Write(
#line 247 "ErrorPage.cshtml"
                        Resources.ErrorPageHtml_NoHeaderData

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n");
#line 248 "ErrorPage.cshtml"
                }

#line default
#line hidden

            WriteLiteral("            </div>\r\n");
#line 250 "ErrorPage.cshtml"
        }

#line default
#line hidden

            WriteLiteral("        ");
#line 251 "ErrorPage.cshtml"
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

            WriteLiteral("        <script src=\"http://ajax.aspnetcdn.com/ajax/jquery/jquery-1.9.0.js\"></scr" +
"ipt>\r\n        <script>\r\n            //<!--\r\n            (function (window, undefined) {\r\n    $('.collapsable').hide();\r\n    $('.page').hide();\r\n    $('#stackpage').show();\r\n\r\n    $('.frame').click(function () {\r\n        $(this).children('.source').children('.collapsable').toggle('fast');\r\n    });\r\n\r\n    $('.frame').keypress(function (e) {\r\n        if (e.which == 13) {\r\n            $(this).children('.source').children('.collapsable').toggle('fast');\r\n        }\r\n    });\r\n    \r\n    $('#header li').click(function () {\r\n\r\n        var unselected = $('#header .selected').removeClass('selected').attr('id');\r\n        var selected = $(this).addClass('selected').attr('id');\r\n        \r\n        $('#' + unselected + 'page').hide();\r\n        $('#' + selected + 'page').show('fast');\r\n    });\r\n\r\n    $('#header li').keypress(function (e) {\r\n        if (e.which == 13) {\r\n            var unselected = $('#header .selected').removeClass('selected').attr('id');\r\n            var selected = $(this).addClass('selected').attr('id');\r\n\r\n            $('#' + unselected + 'page').hide();\r\n            $('#' + selected + 'page').show('fast');\r\n        }\r\n    });\r\n    \r\n})(window);\r\n\r\n            //-->\r\n        </script>\r\n    </body>\r\n</html>\r\n");
        }
    }
}
