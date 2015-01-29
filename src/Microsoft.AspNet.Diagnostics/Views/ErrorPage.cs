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
using System.Net

#line default
#line hidden
    ;
#line 5 "ErrorPage.cshtml"
using Views

#line default
#line hidden
    ;
    using System.Threading.Tasks;

    public class ErrorPage : Microsoft.AspNet.Diagnostics.Views.BaseView
    {
#line 7 "ErrorPage.cshtml"

    public ErrorPage(ErrorPageModel model)
    {
        Model = model;
    }

    public ErrorPageModel Model { get; set; }

#line default
#line hidden
        #line hidden
        public ErrorPage()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 15 "ErrorPage.cshtml"
  
    Response.StatusCode = 500;
    // TODO: Response.ReasonPhrase = "Internal Server Error";
    Response.ContentType = "text/html";
    Response.ContentLength = null; // Clear any prior Content-Length
    string location = string.Empty;

#line default
#line hidden

            WriteLiteral("\r\n<!DOCTYPE html>\r\n<html");
            WriteAttribute("lang", Tuple.Create(" lang=\"", 518), Tuple.Create("\"", 579), 
            Tuple.Create(Tuple.Create("", 525), Tuple.Create<System.Object, System.Int32>(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, 525), false));
            WriteLiteral(" xmlns=\"http://www.w3.org/1999/xhtml\">\r\n    <head>\r\n        <meta charset=\"utf-8\"" +
" />\r\n        <title>");
#line 26 "ErrorPage.cshtml"
          Write(Resources.ErrorPageHtml_Title);

#line default
#line hidden
            WriteLiteral("</title>\r\n        <style>\r\n            body {\r\n    font-family: 'Segoe UI', Tahoma, Arial, Helvetica, sans-serif;\r\n    font-size: .813em;\r\n    line-height: 1.4em;\r\n    color: #222;\r\n}\r\n\r\nh1, h2, h3, h4, h5 {\r\n    /*font-family: 'Segoe UI',Tahoma,Arial,Helvetica,sans-serif;*/\r\n    font-weight: 100;\r\n}\r\n\r\nh1 {\r\n    color: #44525e;\r\n    margin: 15px 0 15px 0;\r\n}\r\n\r\nh2 {\r\n    margin: 10px 5px 0 0;\r\n}\r\n\r\nh3 {\r\n    color: #363636;\r\n    margin: 5px 5px 0 0;\r\n}\r\n\r\ncode {\r\n    font-family: Consolas, \"Courier New\", courier, monospace;\r\n}\r\n\r\nbody .titleerror {\r\n    padding: 3px;\r\n    display: block;\r\n    font-size: 1.5em;\r\n    font-weight: 100;\r\n}\r\n\r\nbody .location {\r\n    margin: 3px 0 10px 30px;\r\n}\r\n\r\n#header {\r\n    font-size: 18px;\r\n    padding: 15px 0;\r\n    border-top: 1px #ddd solid;\r\n    border-bottom: 1px #ddd solid;\r\n    margin-bottom: 0;\r\n}\r\n\r\n    #header li {\r\n        display: inline;\r\n        margin: 5px;\r\n        padding: 5px;\r\n        color: #a0a0a0;\r\n        cursor: pointer;\r\n    }\r\n\r\n        #header li:hover {\r\n            background: #a9e4f9;\r\n            color: #fff;\r\n        }\r\n\r\n        #header .selected {\r\n            background: #44c5f2;\r\n            color: #fff;\r\n        }\r\n\r\n#stackpage ul {\r\n    list-style: none;\r\n    padding-left: 0;\r\n    margin: 0;\r\n    /*border-bottom: 1px #ddd solid;*/\r\n}\r\n\r\n#stackpage .stackerror {\r\n    padding: 5px;\r\n    border-bottom: 1px #ddd solid;\r\n}\r\n\r\n    #stackpage .stackerror:hover {\r\n        background-color: #f0f0f0;\r\n    }\r\n\r\n#stackpage .frame:hover {\r\n    background-color: #f0f0f0;\r\n    text-decoration: none;\r\n}\r\n\r\n#stackpage .frame {\r\n    padding: 2px;\r\n    margin: 0 0 0 30px;\r\n    border-bottom: 1px #ddd solid;\r\n    cursor: pointer;\r\n}\r\n\r\n    #stackpage .frame h3 {\r\n        padding: 5px;\r\n        margin: 0;\r\n    }\r\n\r\n#stackpage .source {\r\n    padding: 0;\r\n}\r\n\r\n    #stackpage .source ol li {\r\n        font-family: Consolas, \"Courier New\", courier, monospace;\r\n        white-space: pre;\r\n    }\r\n\r\n#stackpage .frame:hover .source .highlight li span {\r\n    color: #fff;\r\n    background: #b20000;\r\n}\r\n\r\n#stackpage .source ol.collapsable li {\r\n    color: #888;\r\n}\r\n\r\n    #stackpage .source ol.collapsable li span {\r\n        color: #606060;\r\n    }\r\n\r\n.page table {\r\n    border-collapse: separate;\r\n    border-spacing: 0;\r\n    margin: 0 0 20px;\r\n}\r\n\r\n.page th {\r\n    vertical-align: bottom;\r\n    padding: 10px 5px 5px 5px;\r\n    font-weight: 400;\r\n    color: #a0a0a0;\r\n    text-align: left;\r\n}\r\n\r\n.page td {\r\n    padding: 3px 10px;\r\n}\r\n\r\n.page th, .page td {\r\n    border-right: 1px #ddd solid;\r\n    border-bottom: 1px #ddd solid;\r\n    border-left: 1px transparent solid;\r\n    border-top: 1px transparent solid;\r\n    box-sizing: border-box;\r\n}\r\n\r\n    .page th:last-child, .page td:last-child {\r\n        border-right: 1px transparent solid;\r\n    }\r\n\r\n    .page .length {\r\n        text-align: right;\r\n    }\r\n\r\na {\r\n    color: #1ba1e2;\r\n    text-decoration: none;\r\n}\r\n\r\n    a:hover {\r\n        color: #13709e;\r\n        text-decoration: underline;\r\n    }\r\n\r\n        </s" +
"tyle>\r\n    </head>\r\n    <body>\r\n        <h1>");
#line 32 "ErrorPage.cshtml"
       Write(Resources.ErrorPageHtml_UnhandledException);

#line default
#line hidden
            WriteLiteral("</h1>\r\n");
#line 33 "ErrorPage.cshtml"
        

#line default
#line hidden

#line 33 "ErrorPage.cshtml"
         if (Model.Options.ShowExceptionDetails)
        {
            foreach (var errorDetail in Model.ErrorDetails)
            {

#line default
#line hidden

            WriteLiteral("                <div class=\"titleerror\">");
#line 37 "ErrorPage.cshtml"
                                   Write(errorDetail.Error.GetType().Name);

#line default
#line hidden
            WriteLiteral(": ");
#line 37 "ErrorPage.cshtml"
                                                                              Output.Write(WebUtility.HtmlEncode(errorDetail.Error.Message).Replace("\r\n", "<br/>").Replace("\r", "<br/>").Replace("\n", "<br/>")); 

#line default
#line hidden

            WriteLiteral("</div>\r\n");
#line 38 "ErrorPage.cshtml"
                

#line default
#line hidden

#line 38 "ErrorPage.cshtml"
                  
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

#line 49 "ErrorPage.cshtml"
                 
                if (!string.IsNullOrEmpty(location) && firstFrame != null && !string.IsNullOrEmpty(firstFrame.File))
                {

#line default
#line hidden

            WriteLiteral("                    <p class=\"location\">");
#line 52 "ErrorPage.cshtml"
                                   Write(location);

#line default
#line hidden
            WriteLiteral(" in <code");
            WriteAttribute("title", Tuple.Create(" title=\"", 2089), Tuple.Create("\"", 2113), 
            Tuple.Create(Tuple.Create("", 2097), Tuple.Create<System.Object, System.Int32>(firstFrame.File, 2097), false));
            WriteLiteral(">");
#line 52 "ErrorPage.cshtml"
                                                                               Write(System.IO.Path.GetFileName(firstFrame.File));

#line default
#line hidden
            WriteLiteral("</code>, line ");
#line 52 "ErrorPage.cshtml"
                                                                                                                                         Write(firstFrame.Line);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 53 "ErrorPage.cshtml"
                }
                else if (!string.IsNullOrEmpty(location))
                {

#line default
#line hidden

            WriteLiteral("                    <p class=\"location\">");
#line 56 "ErrorPage.cshtml"
                                   Write(location);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 57 "ErrorPage.cshtml"
                }
                else
                {

#line default
#line hidden

            WriteLiteral("                    <p class=\"location\">");
#line 60 "ErrorPage.cshtml"
                                   Write(Resources.ErrorPageHtml_UnknownLocation);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 61 "ErrorPage.cshtml"
                }
            }
        }
        else
        {

#line default
#line hidden

            WriteLiteral("            <h2>");
#line 66 "ErrorPage.cshtml"
           Write(Resources.ErrorPageHtml_EnableShowExceptions);

#line default
#line hidden
            WriteLiteral("</h2>\r\n");
#line 67 "ErrorPage.cshtml"
        }

#line default
#line hidden

            WriteLiteral("        <ul id=\"header\">\r\n");
#line 69 "ErrorPage.cshtml"
            

#line default
#line hidden

#line 69 "ErrorPage.cshtml"
             if (Model.Options.ShowExceptionDetails)
            {

#line default
#line hidden

            WriteLiteral("                <li id=\"stack\" tabindex=\"1\" class=\"selected\">\r\n                  " +
"  ");
#line 72 "ErrorPage.cshtml"
               Write(Resources.ErrorPageHtml_StackButton);

#line default
#line hidden
            WriteLiteral("\r\n                </li>\r\n");
#line 74 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("            ");
#line 75 "ErrorPage.cshtml"
             if (Model.Options.ShowQuery)
            {

#line default
#line hidden

            WriteLiteral("                <li id=\"query\" tabindex=\"2\">\r\n                    ");
#line 78 "ErrorPage.cshtml"
               Write(Resources.ErrorPageHtml_QueryButton);

#line default
#line hidden
            WriteLiteral("\r\n                </li>\r\n");
#line 80 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("            ");
#line 81 "ErrorPage.cshtml"
             if (Model.Options.ShowCookies)
            {

#line default
#line hidden

            WriteLiteral("                <li id=\"cookies\" tabindex=\"3\">\r\n                    ");
#line 84 "ErrorPage.cshtml"
               Write(Resources.ErrorPageHtml_CookiesButton);

#line default
#line hidden
            WriteLiteral("\r\n                </li>\r\n");
#line 86 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("            ");
#line 87 "ErrorPage.cshtml"
             if (Model.Options.ShowHeaders)
            {

#line default
#line hidden

            WriteLiteral("                <li id=\"headers\" tabindex=\"4\">\r\n                    ");
#line 90 "ErrorPage.cshtml"
               Write(Resources.ErrorPageHtml_HeadersButton);

#line default
#line hidden
            WriteLiteral("\r\n                </li>\r\n");
#line 92 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("            ");
#line 93 "ErrorPage.cshtml"
             if (Model.Options.ShowEnvironment)
            {

#line default
#line hidden

            WriteLiteral("                <li id=\"environment\" tabindex=\"5\">\r\n                    ");
#line 96 "ErrorPage.cshtml"
               Write(Resources.ErrorPageHtml_EnvironmentButton);

#line default
#line hidden
            WriteLiteral("\r\n                </li>\r\n");
#line 98 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("        </ul>\r\n");
#line 100 "ErrorPage.cshtml"
        

#line default
#line hidden

#line 100 "ErrorPage.cshtml"
         if (Model.Options.ShowExceptionDetails)
        {

#line default
#line hidden

            WriteLiteral("            <div id=\"stackpage\" class=\"page\">\r\n                <ul>\r\n");
#line 104 "ErrorPage.cshtml"
                    

#line default
#line hidden

#line 104 "ErrorPage.cshtml"
                       int tabIndex = 6; 

#line default
#line hidden

            WriteLiteral("\r\n");
#line 105 "ErrorPage.cshtml"
                    

#line default
#line hidden

#line 105 "ErrorPage.cshtml"
                     foreach (var errorDetail in Model.ErrorDetails)
                    {

#line default
#line hidden

            WriteLiteral("                        <li>\r\n                            <h2 class=\"stackerror\">" +
"");
#line 108 "ErrorPage.cshtml"
                                              Write(errorDetail.Error.GetType().Name);

#line default
#line hidden
            WriteLiteral(": ");
#line 108 "ErrorPage.cshtml"
                                                                                 Write(errorDetail.Error.Message);

#line default
#line hidden
            WriteLiteral("</h2>\r\n                            <ul>\r\n");
#line 110 "ErrorPage.cshtml"
                            

#line default
#line hidden

#line 110 "ErrorPage.cshtml"
                             foreach (var frame in errorDetail.StackFrames)
                            {

#line default
#line hidden

            WriteLiteral("                                <li class=\"frame\"");
            WriteAttribute("tabindex", Tuple.Create(" tabindex=\"", 4348), Tuple.Create("\"", 4368), 
            Tuple.Create(Tuple.Create("", 4359), Tuple.Create<System.Object, System.Int32>(tabIndex, 4359), false));
            WriteLiteral(">\r\n");
#line 113 "ErrorPage.cshtml"
                                    

#line default
#line hidden

#line 113 "ErrorPage.cshtml"
                                       tabIndex++; 

#line default
#line hidden

            WriteLiteral("\r\n");
#line 114 "ErrorPage.cshtml"
                                    

#line default
#line hidden

#line 114 "ErrorPage.cshtml"
                                     if (string.IsNullOrEmpty(frame.File))
                                    {

#line default
#line hidden

            WriteLiteral("                                        <h3>");
#line 116 "ErrorPage.cshtml"
                                       Write(frame.Function);

#line default
#line hidden
            WriteLiteral("</h3>\r\n");
#line 117 "ErrorPage.cshtml"
                                    }
                                    else
                                    {

#line default
#line hidden

            WriteLiteral("                                        <h3>");
#line 120 "ErrorPage.cshtml"
                                       Write(frame.Function);

#line default
#line hidden
            WriteLiteral(" in <code");
            WriteAttribute("title", Tuple.Create(" title=\"", 4795), Tuple.Create("\"", 4814), 
            Tuple.Create(Tuple.Create("", 4803), Tuple.Create<System.Object, System.Int32>(frame.File, 4803), false));
            WriteLiteral(">");
#line 120 "ErrorPage.cshtml"
                                                                                    Write(System.IO.Path.GetFileName(frame.File));

#line default
#line hidden
            WriteLiteral("</code></h3>\r\n");
#line 121 "ErrorPage.cshtml"
                                    }

#line default
#line hidden

            WriteLiteral("\r\n");
#line 123 "ErrorPage.cshtml"
                                    

#line default
#line hidden

#line 123 "ErrorPage.cshtml"
                                     if (frame.Line != 0 && frame.ContextCode.Any())
                                    {

#line default
#line hidden

            WriteLiteral("                                        <div class=\"source\">\r\n");
#line 126 "ErrorPage.cshtml"
                                            

#line default
#line hidden

#line 126 "ErrorPage.cshtml"
                                             if (frame.PreContextCode != null)
                                            {

#line default
#line hidden

            WriteLiteral("                                                <ol");
            WriteAttribute("start", Tuple.Create(" start=\"", 5275), Tuple.Create("\"", 5304), 
            Tuple.Create(Tuple.Create("", 5283), Tuple.Create<System.Object, System.Int32>(frame.PreContextLine, 5283), false));
            WriteLiteral(" class=\"collapsible\">\r\n");
#line 129 "ErrorPage.cshtml"
                                                    

#line default
#line hidden

#line 129 "ErrorPage.cshtml"
                                                     foreach (var line in frame.PreContextCode)
                                                    {

#line default
#line hidden

            WriteLiteral("                                                        <li><span>");
#line 131 "ErrorPage.cshtml"
                                                             Write(line);

#line default
#line hidden
            WriteLiteral("</span></li>\r\n");
#line 132 "ErrorPage.cshtml"
                                                    }

#line default
#line hidden

            WriteLiteral("                                                </ol>\r\n");
#line 134 "ErrorPage.cshtml"
                                            } 

#line default
#line hidden

            WriteLiteral("\r\n                                            <ol");
            WriteAttribute("start", Tuple.Create(" start=\"", 5772), Tuple.Create("\"", 5791), 
            Tuple.Create(Tuple.Create("", 5780), Tuple.Create<System.Object, System.Int32>(frame.Line, 5780), false));
            WriteLiteral(" class=\"highlight\">\r\n");
#line 137 "ErrorPage.cshtml"
                                                

#line default
#line hidden

#line 137 "ErrorPage.cshtml"
                                                 foreach (var line in frame.ContextCode)
                                                {

#line default
#line hidden

            WriteLiteral("                                                    <li><span>");
#line 139 "ErrorPage.cshtml"
                                                         Write(line);

#line default
#line hidden
            WriteLiteral("</span></li>\r\n");
#line 140 "ErrorPage.cshtml"
                                                }

#line default
#line hidden

            WriteLiteral("                                            </ol>\r\n\r\n");
#line 143 "ErrorPage.cshtml"
                                            

#line default
#line hidden

#line 143 "ErrorPage.cshtml"
                                             if (frame.PostContextCode != null)
                                            {

#line default
#line hidden

            WriteLiteral("                                                <ol");
            WriteAttribute("start", Tuple.Create(" start=\'", 6318), Tuple.Create("\'", 6343), 
            Tuple.Create(Tuple.Create("", 6326), Tuple.Create<System.Object, System.Int32>(frame.Line + 1, 6326), false));
            WriteLiteral(" class=\"collapsible\">\r\n");
#line 146 "ErrorPage.cshtml"
                                                    

#line default
#line hidden

#line 146 "ErrorPage.cshtml"
                                                     foreach (var line in frame.PostContextCode)
                                                    {

#line default
#line hidden

            WriteLiteral("                                                        <li><span>");
#line 148 "ErrorPage.cshtml"
                                                             Write(line);

#line default
#line hidden
            WriteLiteral("</span></li>\r\n");
#line 149 "ErrorPage.cshtml"
                                                    }

#line default
#line hidden

            WriteLiteral("                                                </ol>\r\n");
#line 151 "ErrorPage.cshtml"
                                            } 

#line default
#line hidden

            WriteLiteral("                                        </div>\r\n");
#line 153 "ErrorPage.cshtml"
                                    } 

#line default
#line hidden

            WriteLiteral("                                </li>\r\n");
#line 155 "ErrorPage.cshtml"
                            }

#line default
#line hidden

            WriteLiteral("                            </ul>\r\n                        </li>\r\n");
#line 158 "ErrorPage.cshtml"
                    }

#line default
#line hidden

            WriteLiteral("                </ul>\r\n            </div>\r\n");
#line 161 "ErrorPage.cshtml"
        }

#line default
#line hidden

            WriteLiteral("        ");
#line 162 "ErrorPage.cshtml"
         if (Model.Options.ShowQuery)
        {

#line default
#line hidden

            WriteLiteral("            <div id=\"querypage\" class=\"page\">\r\n");
#line 165 "ErrorPage.cshtml"
                

#line default
#line hidden

#line 165 "ErrorPage.cshtml"
                 if (Model.Query.Any())
                {

#line default
#line hidden

            WriteLiteral("                    <table>\r\n                        <thead>\r\n                   " +
"         <tr>\r\n                                <th>");
#line 170 "ErrorPage.cshtml"
                               Write(Resources.ErrorPageHtml_VariableColumn);

#line default
#line hidden
            WriteLiteral("</th>\r\n                                <th>");
#line 171 "ErrorPage.cshtml"
                               Write(Resources.ErrorPageHtml_ValueColumn);

#line default
#line hidden
            WriteLiteral("</th>\r\n                            </tr>\r\n                        </thead>\r\n     " +
"                   <tbody>\r\n");
#line 175 "ErrorPage.cshtml"
                            

#line default
#line hidden

#line 175 "ErrorPage.cshtml"
                             foreach (var kv in Model.Query.OrderBy(kv => kv.Key))
                            {
                                foreach (var v in kv.Value)
                                {

#line default
#line hidden

            WriteLiteral("                                    <tr>\r\n                                       " +
" <td>");
#line 180 "ErrorPage.cshtml"
                                       Write(kv.Key);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                        <td>");
#line 181 "ErrorPage.cshtml"
                                       Write(v);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                    </tr>\r\n");
#line 183 "ErrorPage.cshtml"
                                }
                            }

#line default
#line hidden

            WriteLiteral("                        </tbody>\r\n                    </table>\r\n");
#line 187 "ErrorPage.cshtml"
                }
                else
                {

#line default
#line hidden

            WriteLiteral("                    <p>");
#line 190 "ErrorPage.cshtml"
                  Write(Resources.ErrorPageHtml_NoQueryStringData);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 191 "ErrorPage.cshtml"
                }

#line default
#line hidden

            WriteLiteral("            </div>\r\n");
#line 193 "ErrorPage.cshtml"
        }

#line default
#line hidden

            WriteLiteral("        ");
#line 194 "ErrorPage.cshtml"
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
#line 225 "ErrorPage.cshtml"
         if (Model.Options.ShowHeaders)
        {

#line default
#line hidden

            WriteLiteral("            <div id=\"headerspage\" class=\"page\">\r\n");
#line 228 "ErrorPage.cshtml"
                

#line default
#line hidden

#line 228 "ErrorPage.cshtml"
                 if (Model.Headers.Any())
                {

#line default
#line hidden

            WriteLiteral("                    <table>\r\n                        <thead>\r\n                   " +
"         <tr>\r\n                                <th>");
#line 233 "ErrorPage.cshtml"
                               Write(Resources.ErrorPageHtml_VariableColumn);

#line default
#line hidden
            WriteLiteral("</th>\r\n                                <th>");
#line 234 "ErrorPage.cshtml"
                               Write(Resources.ErrorPageHtml_ValueColumn);

#line default
#line hidden
            WriteLiteral("</th>\r\n                            </tr>\r\n                        </thead>\r\n     " +
"                   <tbody>\r\n");
#line 238 "ErrorPage.cshtml"
                            

#line default
#line hidden

#line 238 "ErrorPage.cshtml"
                             foreach (var kv in Model.Headers.OrderBy(kv => kv.Key))
                            {
                                foreach (var v in kv.Value)
                                {

#line default
#line hidden

            WriteLiteral("                                    <tr>\r\n                                       " +
" <td>");
#line 243 "ErrorPage.cshtml"
                                       Write(kv.Key);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                        <td>");
#line 244 "ErrorPage.cshtml"
                                       Write(v);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                    </tr>\r\n");
#line 246 "ErrorPage.cshtml"
                                }
                            }

#line default
#line hidden

            WriteLiteral("                        </tbody>\r\n                    </table>\r\n");
#line 250 "ErrorPage.cshtml"
                }
                else
                {

#line default
#line hidden

            WriteLiteral("                    <p>");
#line 253 "ErrorPage.cshtml"
                  Write(Resources.ErrorPageHtml_NoHeaderData);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 254 "ErrorPage.cshtml"
                }

#line default
#line hidden

            WriteLiteral("            </div>\r\n");
#line 256 "ErrorPage.cshtml"
        }

#line default
#line hidden

            WriteLiteral("        ");
#line 257 "ErrorPage.cshtml"
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

            WriteLiteral("        <script>\r\n            //<!--\r\n            (function (window, undefined) {\r\n    \"use strict\";\r\n\r\n    function $(selector, element) {\r\n        return new NodeCollection(selector, element);\r\n    }\r\n\r\n    function NodeCollection(selector, element) {\r\n        this.items = [];\r\n        element = element || window.document;\r\n\r\n        var nodeList;\r\n\r\n        if (typeof (selector) === \"string\") {\r\n            nodeList = element.querySelectorAll(selector);\r\n            for (var i = 0, l = nodeList.length; i < l; i++) {\r\n                this.items.push(nodeList.item(i));\r\n            }\r\n        } else if (selector.tagName) {\r\n            this.items.push(selector);\r\n        } else if (selector.splice) {\r\n            this.items = this.items.concat(selector);\r\n        }\r\n    }\r\n\r\n    NodeCollection.prototype = {\r\n        each: function (callback) {\r\n            for (var i = 0, l = this.items.length; i < l; i++) {\r\n                callback(this.items[i], i);\r\n            }\r\n            return this;\r\n        },\r\n\r\n        children: function (selector) {\r\n            var children = [];\r\n\r\n            this.each(function (el) {\r\n                children = children.concat($(selector, el).items);\r\n            });\r\n\r\n            return $(children);\r\n        },\r\n\r\n        hide: function () {\r\n            this.each(function (el) {\r\n                el.style.display = \"none\";\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        toggle: function () {\r\n            this.each(function (el) {\r\n                el.style.display = el.style.display === \"none\" ? \"\" : \"none\";\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        show: function () {\r\n            this.each(function (el) {\r\n                el.style.display = \"\";\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        addClass: function (className) {\r\n            this.each(function (el) {\r\n                var existingClassName = el.className,\r\n                    classNames;\r\n                if (!existingClassName) {\r\n                    el.className = className;\r\n                } else {\r\n                    classNames = existingClassName.split(\" \");\r\n                    if (classNames.indexOf(className) < 0) {\r\n                        el.className = existingClassName + \" \" + className;\r\n                    }\r\n                }\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        removeClass: function (className) {\r\n            this.each(function (el) {\r\n                var existingClassName = el.className,\r\n                    classNames, index;\r\n                if (existingClassName === className) {\r\n                    el.className = \"\";\r\n                } else if (existingClassName) {\r\n                    classNames = existingClassName.split(\" \");\r\n                    index = classNames.indexOf(className);\r\n                    if (index > 0) {\r\n                        classNames.splice(index, 1);\r\n                        el.className = classNames.join(\" \");\r\n                    }\r\n                }\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        toggleClass: function (className) {\r\n            this.each(function (el) {\r\n                var classNames = el.className.split(\" \");\r\n                if (classNames.indexOf(className) >= 0) {\r\n                    $(el).removeClass(className);\r\n                } else {\r\n                    $(el).addClass(className);\r\n                }\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        attr: function (name) {\r\n            if (this.items.length === 0) {\r\n                return null;\r\n            }\r\n\r\n            return this.items[0].getAttribute(name);\r\n        },\r\n\r\n        on: function (eventName, handler) {\r\n            this.each(function (el, idx) {\r\n                var callback = function (e) {\r\n                    e = e || window.event;\r\n                    if (!e.which && e.keyCode) {\r\n                        e.which = e.keyCode; // Normalize IE8 key events\r\n                    }\r\n                    handler.apply(el, [e]);\r\n                };\r\n\r\n                if (el.addEventListener) { // DOM Events\r\n                    el.addEventListener(eventName, callback, false);\r\n                } else if (el.attachEvent) { // IE8 events\r\n                    el.attachEvent(\"on\" + eventName, callback);\r\n                } else {\r\n                    el[\"on\" + type] = callback;\r\n                }\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        click: function (handler) {\r\n            return this.on(\"click\", handler);\r\n        },\r\n\r\n        keypress: function (handler) {\r\n            return this.on(\"keypress\", handler);\r\n        }\r\n    };\r\n\r\n    function frame(el) {\r\n        $(el).children(\".source .collapsible\").toggle();\r\n    }\r\n\r\n    function tab(el) {\r\n        var unselected = $(\"#header .selected\").removeClass(\"selected\").attr(\"id\");\r\n        var selected = $(el).addClass(\"selected\").attr(\"id\");\r\n\r\n        $(\"#\" + unselected + \"page\").hide();\r\n        $(\"#\" + selected + \"page\").show();\r\n    }\r\n\r\n    $(\".collapsible\").hide();\r\n    $(\".page\").hide();\r\n    $(\"#stackpage\").show();\r\n\r\n    $(\".frame\")\r\n        .click(function () {\r\n            frame(this);\r\n        })\r\n        .keypress(function (e) {\r\n            if (e.which === 13) {\r\n                frame(this);\r\n            }\r\n        });\r\n\r\n    $(\"#header li\")\r\n        .click(function () {\r\n            tab(this);\r\n        })\r\n        .keypress(function (e) {\r\n            if (e.which === 13) {\r\n                tab(this);\r\n            }\r\n        });\r\n})(window);\r\n " +
"           //-->\r\n        </script>\r\n    </body>\r\n</html>\r\n");
        }
        #pragma warning restore 1998
    }
}
