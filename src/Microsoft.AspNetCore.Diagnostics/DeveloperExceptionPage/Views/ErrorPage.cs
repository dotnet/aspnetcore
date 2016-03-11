namespace Microsoft.AspNetCore.Diagnostics.Views
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
using Microsoft.AspNetCore.Diagnostics.Views

#line default
#line hidden
    ;
#line 6 "ErrorPage.cshtml"
using Microsoft.AspNetCore.Diagnostics

#line default
#line hidden
    ;
    using System.Threading.Tasks;

    public class ErrorPage : Microsoft.AspNetCore.DiagnosticsViewPage.Views.BaseView
    {
#line 8 "ErrorPage.cshtml"

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
#line 16 "ErrorPage.cshtml"
  
    // TODO: Response.ReasonPhrase = "Internal Server Error";
    Response.ContentType = "text/html; charset=utf-8";
    string location = string.Empty;

#line default
#line hidden

            WriteLiteral("<!DOCTYPE html>\r\n<html");
            BeginWriteAttribute("lang", " lang=\"", 505, "\"", 566, 1);
#line 22 "ErrorPage.cshtml"
WriteAttributeValue("", 512, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, 512, 54, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" xmlns=\"http://www.w3.org/1999/xhtml\">\r\n    <head>\r\n        <meta charset=\"utf-8\" />\r\n        <title>");
#line 25 "ErrorPage.cshtml"
          Write(Resources.ErrorPageHtml_Title);

#line default
#line hidden
            WriteLiteral("</title>\r\n        <style>\r\n            body {\r\n    font-family: 'Segoe UI', Tahoma, Arial, Helvetica, sans-serif;\r\n    font-size: .813em;\r\n    line-height: 1.4em;\r\n    color: #222;\r\n}\r\n\r\nh1, h2, h3, h4, h5 {\r\n    /*font-family: 'Segoe UI',Tahoma,Arial,Helvetica,sans-serif;*/\r\n    font-weight: 100;\r\n}\r\n\r\nh1 {\r\n    color: #44525e;\r\n    margin: 15px 0 15px 0;\r\n}\r\n\r\nh2 {\r\n    margin: 10px 5px 0 0;\r\n}\r\n\r\nh3 {\r\n    color: #363636;\r\n    margin: 5px 5px 0 0;\r\n}\r\n\r\ncode {\r\n    font-family: Consolas, \"Courier New\", courier, monospace;\r\n}\r\n\r\nbody .titleerror {\r\n    padding: 3px;\r\n    display: block;\r\n    font-size: 1.5em;\r\n    font-weight: 100;\r\n}\r\n\r\nbody .location {\r\n    margin: 3px 0 10px 30px;\r\n}\r\n\r\n#header {\r\n    font-size: 18px;\r\n    padding: 15px 0;\r\n    border-top: 1px #ddd solid;\r\n    border-bottom: 1px #ddd solid;\r\n    margin-bottom: 0;\r\n}\r\n\r\n    #header li {\r\n        display: inline;\r\n        margin: 5px;\r\n        padding: 5px;\r\n        color: #a0a0a0;\r\n        cursor: pointer;\r\n    }\r\n\r\n        #header li:hover {\r\n            background: #a9e4f9;\r\n            color: #fff;\r\n        }\r\n\r\n        #header .selected {\r\n            background: #44c5f2;\r\n            color: #fff;\r\n        }\r\n\r\n#stackpage ul {\r\n    list-style: none;\r\n    padding-left: 0;\r\n    margin: 0;\r\n    /*border-bottom: 1px #ddd solid;*/\r\n}\r\n\r\n#stackpage .stackerror {\r\n    padding: 5px;\r\n    border-bottom: 1px #ddd solid;\r\n}\r\n\r\n    #stackpage .stackerror:hover {\r\n        background-color: #f0f0f0;\r\n    }\r\n\r\n#stackpage .frame:hover {\r\n    background-color: #f0f0f0;\r\n    text-decoration: none;\r\n}\r\n\r\n#stackpage .frame {\r\n    padding: 2px;\r\n    margin: 0 0 0 30px;\r\n    border-bottom: 1px #ddd solid;\r\n    cursor: pointer;\r\n}\r\n\r\n    #stackpage .frame h3 {\r\n        padding: 5px;\r\n        margin: 0;\r\n    }\r\n\r\n#stackpage .source {\r\n    padding: 0;\r\n}\r\n\r\n    #stackpage .source ol li {\r\n        font-family: Consolas, \"Courier New\", courier, monospace;\r\n        white-space: pre;\r\n    }\r\n\r\n#stackpage .frame:hover .source .highlight li span {\r\n    color: #fff;\r\n    background: #b20000;\r\n}\r\n\r\n#stackpage .source ol.collapsible li {\r\n    color: #888;\r\n}\r\n\r\n    #stackpage .source ol.collapsible li span {\r\n        color: #606060;\r\n    }\r\n\r\n.page table {\r\n    border-collapse: separate;\r\n    border-spacing: 0;\r\n    margin: 0 0 20px;\r\n}\r\n\r\n.page th {\r\n    vertical-align: bottom;\r\n    padding: 10px 5px 5px 5px;\r\n    font-weight: 400;\r\n    color: #a0a0a0;\r\n    text-align: left;\r\n}\r\n\r\n.page td {\r\n    padding: 3px 10px;\r\n}\r\n\r\n.page th, .page td {\r\n    border-right: 1px #ddd solid;\r\n    border-bottom: 1px #ddd solid;\r\n    border-left: 1px transparent solid;\r\n    border-top: 1px transparent solid;\r\n    box-sizing: border-box;\r\n}\r\n\r\n    .page th:last-child, .page td:last-child {\r\n        border-right: 1px transparent solid;\r\n    }\r\n\r\n    .page .length {\r\n        text-align: right;\r\n    }\r\n\r\na {\r\n    color: #1ba1e2;\r\n    text-decoration: none;\r\n}\r\n\r\n    a:hover {\r\n        color: #13709e;\r\n        text-decoration: underline;\r\n    }\r\n\r\n        </style>\r\n    </head>\r\n    <body>\r\n        <h1>");
#line 31 "ErrorPage.cshtml"
       Write(Resources.ErrorPageHtml_UnhandledException);

#line default
#line hidden
            WriteLiteral("</h1>\r\n");
#line 32 "ErrorPage.cshtml"
        

#line default
#line hidden

#line 32 "ErrorPage.cshtml"
         foreach (var errorDetail in Model.ErrorDetails)
        {

#line default
#line hidden

            WriteLiteral("            <div class=\"titleerror\">");
#line 34 "ErrorPage.cshtml"
                               Write(errorDetail.Error.GetType().Name);

#line default
#line hidden
            WriteLiteral(": ");
#line 34 "ErrorPage.cshtml"
                                                                          Output.Write(HtmlEncodeAndReplaceLineBreaks(errorDetail.Error.Message)); 

#line default
#line hidden

            WriteLiteral("</div>\r\n");
#line 35 "ErrorPage.cshtml"
            

#line default
#line hidden

#line 35 "ErrorPage.cshtml"
              
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

#line 46 "ErrorPage.cshtml"
             
            if (!string.IsNullOrEmpty(location) && firstFrame != null && !string.IsNullOrEmpty(firstFrame.File))
            {

#line default
#line hidden

            WriteLiteral("                <p class=\"location\">");
#line 49 "ErrorPage.cshtml"
                               Write(location);

#line default
#line hidden
            WriteLiteral(" in <code");
            BeginWriteAttribute("title", " title=\"", 1882, "\"", 1906, 1);
#line 49 "ErrorPage.cshtml"
WriteAttributeValue("", 1890, firstFrame.File, 1890, 16, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">");
#line 49 "ErrorPage.cshtml"
                                                                           Write(System.IO.Path.GetFileName(firstFrame.File));

#line default
#line hidden
            WriteLiteral("</code>, line ");
#line 49 "ErrorPage.cshtml"
                                                                                                                                     Write(firstFrame.Line);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 50 "ErrorPage.cshtml"
            }
            else if (!string.IsNullOrEmpty(location))
            {

#line default
#line hidden

            WriteLiteral("                <p class=\"location\">");
#line 53 "ErrorPage.cshtml"
                               Write(location);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 54 "ErrorPage.cshtml"
            }
            else
            {

#line default
#line hidden

            WriteLiteral("                <p class=\"location\">");
#line 57 "ErrorPage.cshtml"
                               Write(Resources.ErrorPageHtml_UnknownLocation);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 58 "ErrorPage.cshtml"
            }
        }

#line default
#line hidden

            WriteLiteral("        <ul id=\"header\">\r\n            <li id=\"stack\" tabindex=\"1\" class=\"selected\">\r\n                ");
#line 62 "ErrorPage.cshtml"
           Write(Resources.ErrorPageHtml_StackButton);

#line default
#line hidden
            WriteLiteral("\r\n            </li>\r\n            <li id=\"query\" tabindex=\"2\">\r\n                ");
#line 65 "ErrorPage.cshtml"
           Write(Resources.ErrorPageHtml_QueryButton);

#line default
#line hidden
            WriteLiteral("\r\n            </li>\r\n            <li id=\"cookies\" tabindex=\"3\">\r\n                ");
#line 68 "ErrorPage.cshtml"
           Write(Resources.ErrorPageHtml_CookiesButton);

#line default
#line hidden
            WriteLiteral("\r\n            </li>\r\n            <li id=\"headers\" tabindex=\"4\">\r\n                ");
#line 71 "ErrorPage.cshtml"
           Write(Resources.ErrorPageHtml_HeadersButton);

#line default
#line hidden
            WriteLiteral("\r\n            </li>\r\n        </ul>\r\n\r\n        <div id=\"stackpage\" class=\"page\">\r\n            <ul>\r\n");
#line 77 "ErrorPage.cshtml"
                

#line default
#line hidden

#line 77 "ErrorPage.cshtml"
                   int tabIndex = 6; 

#line default
#line hidden

            WriteLiteral("                ");
#line 78 "ErrorPage.cshtml"
                 foreach (var errorDetail in Model.ErrorDetails)
                {

#line default
#line hidden

            WriteLiteral("                    <li>\r\n                        <h2 class=\"stackerror\">");
#line 81 "ErrorPage.cshtml"
                                          Write(errorDetail.Error.GetType().Name);

#line default
#line hidden
            WriteLiteral(": ");
#line 81 "ErrorPage.cshtml"
                                                                             Write(errorDetail.Error.Message);

#line default
#line hidden
            WriteLiteral("</h2>\r\n                        <ul>\r\n");
#line 83 "ErrorPage.cshtml"
                        

#line default
#line hidden

#line 83 "ErrorPage.cshtml"
                         foreach (var frame in errorDetail.StackFrames)
                        {

#line default
#line hidden

            WriteLiteral("                            <li class=\"frame\"");
            BeginWriteAttribute("tabindex", " tabindex=\"", 3310, "\"", 3330, 1);
#line 85 "ErrorPage.cshtml"
WriteAttributeValue("", 3321, tabIndex, 3321, 9, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">\r\n");
#line 86 "ErrorPage.cshtml"
                                

#line default
#line hidden

#line 86 "ErrorPage.cshtml"
                                   tabIndex++; 

#line default
#line hidden

            WriteLiteral("                                ");
#line 87 "ErrorPage.cshtml"
                                 if (string.IsNullOrEmpty(frame.File))
                                {

#line default
#line hidden

            WriteLiteral("                                    <h3>");
#line 89 "ErrorPage.cshtml"
                                   Write(frame.Function);

#line default
#line hidden
            WriteLiteral("</h3>\r\n");
#line 90 "ErrorPage.cshtml"
                                }
                                else
                                {

#line default
#line hidden

            WriteLiteral("                                    <h3>");
#line 93 "ErrorPage.cshtml"
                                   Write(frame.Function);

#line default
#line hidden
            WriteLiteral(" in <code");
            BeginWriteAttribute("title", " title=\"", 3725, "\"", 3744, 1);
#line 93 "ErrorPage.cshtml"
WriteAttributeValue("", 3733, frame.File, 3733, 11, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">");
#line 93 "ErrorPage.cshtml"
                                                                                Write(System.IO.Path.GetFileName(frame.File));

#line default
#line hidden
            WriteLiteral("</code></h3>\r\n");
#line 94 "ErrorPage.cshtml"
                                }

#line default
#line hidden

            WriteLiteral("\r\n");
#line 96 "ErrorPage.cshtml"
                                

#line default
#line hidden

#line 96 "ErrorPage.cshtml"
                                 if (frame.Line != 0 && frame.ContextCode.Any())
                                {

#line default
#line hidden

            WriteLiteral("                                    <div class=\"source\">\r\n");
#line 99 "ErrorPage.cshtml"
                                        

#line default
#line hidden

#line 99 "ErrorPage.cshtml"
                                         if (frame.PreContextCode.Any())
                                        {

#line default
#line hidden

            WriteLiteral("                                            <ol");
            BeginWriteAttribute("start", " start=\"", 4175, "\"", 4204, 1);
#line 101 "ErrorPage.cshtml"
WriteAttributeValue("", 4183, frame.PreContextLine, 4183, 21, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" class=\"collapsible\">\r\n");
#line 102 "ErrorPage.cshtml"
                                                

#line default
#line hidden

#line 102 "ErrorPage.cshtml"
                                                 foreach (var line in frame.PreContextCode)
                                                {

#line default
#line hidden

            WriteLiteral("                                                    <li><span>");
#line 104 "ErrorPage.cshtml"
                                                         Write(line);

#line default
#line hidden
            WriteLiteral("</span></li>\r\n");
#line 105 "ErrorPage.cshtml"
                                                }

#line default
#line hidden

            WriteLiteral("                                            </ol>\r\n");
#line 107 "ErrorPage.cshtml"
                                        }

#line default
#line hidden

            WriteLiteral("\r\n                                        <ol");
            BeginWriteAttribute("start", " start=\"", 4643, "\"", 4662, 1);
#line 109 "ErrorPage.cshtml"
WriteAttributeValue("", 4651, frame.Line, 4651, 11, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" class=\"highlight\">\r\n");
#line 110 "ErrorPage.cshtml"
                                            

#line default
#line hidden

#line 110 "ErrorPage.cshtml"
                                             foreach (var line in frame.ContextCode)
                                            {

#line default
#line hidden

            WriteLiteral("                                                <li><span>");
#line 112 "ErrorPage.cshtml"
                                                     Write(line);

#line default
#line hidden
            WriteLiteral("</span></li>\r\n");
#line 113 "ErrorPage.cshtml"
                                            }

#line default
#line hidden

            WriteLiteral("                                        </ol>\r\n\r\n");
#line 116 "ErrorPage.cshtml"
                                        

#line default
#line hidden

#line 116 "ErrorPage.cshtml"
                                         if (frame.PostContextCode.Any())
                                        {

#line default
#line hidden

            WriteLiteral("                                            <ol");
            BeginWriteAttribute("start", " start=\'", 5155, "\'", 5180, 1);
#line 118 "ErrorPage.cshtml"
WriteAttributeValue("", 5163, frame.Line + 1, 5163, 17, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" class=\"collapsible\">\r\n");
#line 119 "ErrorPage.cshtml"
                                                

#line default
#line hidden

#line 119 "ErrorPage.cshtml"
                                                 foreach (var line in frame.PostContextCode)
                                                {

#line default
#line hidden

            WriteLiteral("                                                    <li><span>");
#line 121 "ErrorPage.cshtml"
                                                         Write(line);

#line default
#line hidden
            WriteLiteral("</span></li>\r\n");
#line 122 "ErrorPage.cshtml"
                                                }

#line default
#line hidden

            WriteLiteral("                                            </ol>\r\n");
#line 124 "ErrorPage.cshtml"
                                        }

#line default
#line hidden

            WriteLiteral("                                    </div>\r\n");
#line 126 "ErrorPage.cshtml"
                                }

#line default
#line hidden

            WriteLiteral("                            </li>\r\n");
#line 128 "ErrorPage.cshtml"
                        }

#line default
#line hidden

            WriteLiteral(@"                        </ul>
                    </li>
                    <li>
                        <br/>
                        <div class=""rawExceptionBlock"">
                            <div>
                                <a href=""#"" id=""rawExceptionButton"">Show raw exception details</a>
                            </div>
                            <div id=""rawException"" style=""display:none"">
                                <pre>");
#line 138 "ErrorPage.cshtml"
                                Write(errorDetail.Error.ToString());

#line default
#line hidden
            WriteLiteral("</pre>\r\n                            </div>\r\n                        </div>\r\n                    </li>\r\n");
#line 142 "ErrorPage.cshtml"
                }

#line default
#line hidden

            WriteLiteral("            </ul>\r\n        </div>\r\n\r\n        <div id=\"querypage\" class=\"page\">\r\n");
#line 147 "ErrorPage.cshtml"
            

#line default
#line hidden

#line 147 "ErrorPage.cshtml"
             if (Model.Query.Any())
            {

#line default
#line hidden

            WriteLiteral("                <table>\r\n                    <thead>\r\n                        <tr>\r\n                            <th>");
#line 152 "ErrorPage.cshtml"
                           Write(Resources.ErrorPageHtml_VariableColumn);

#line default
#line hidden
            WriteLiteral("</th>\r\n                            <th>");
#line 153 "ErrorPage.cshtml"
                           Write(Resources.ErrorPageHtml_ValueColumn);

#line default
#line hidden
            WriteLiteral("</th>\r\n                        </tr>\r\n                    </thead>\r\n                    <tbody>\r\n");
#line 157 "ErrorPage.cshtml"
                        

#line default
#line hidden

#line 157 "ErrorPage.cshtml"
                         foreach (var kv in Model.Query.OrderBy(kv => kv.Key))
                        {
                            foreach (var v in kv.Value)
                            {

#line default
#line hidden

            WriteLiteral("                                <tr>\r\n                                    <td>");
#line 162 "ErrorPage.cshtml"
                                   Write(kv.Key);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                    <td>");
#line 163 "ErrorPage.cshtml"
                                   Write(v);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                </tr>\r\n");
#line 165 "ErrorPage.cshtml"
                            }
                        }

#line default
#line hidden

            WriteLiteral("                    </tbody>\r\n                </table>\r\n");
#line 169 "ErrorPage.cshtml"
            }
            else
            {

#line default
#line hidden

            WriteLiteral("                <p>");
#line 172 "ErrorPage.cshtml"
              Write(Resources.ErrorPageHtml_NoQueryStringData);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 173 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("        </div>\r\n\r\n        <div id=\"cookiespage\" class=\"page\">\r\n");
#line 177 "ErrorPage.cshtml"
            

#line default
#line hidden

#line 177 "ErrorPage.cshtml"
             if (Model.Cookies.Any())
            {

#line default
#line hidden

            WriteLiteral("                <table>\r\n                    <thead>\r\n                        <tr>\r\n                            <th>");
#line 182 "ErrorPage.cshtml"
                           Write(Resources.ErrorPageHtml_VariableColumn);

#line default
#line hidden
            WriteLiteral("</th>\r\n                            <th>");
#line 183 "ErrorPage.cshtml"
                           Write(Resources.ErrorPageHtml_ValueColumn);

#line default
#line hidden
            WriteLiteral("</th>\r\n                        </tr>\r\n                    </thead>\r\n                    <tbody>\r\n");
#line 187 "ErrorPage.cshtml"
                        

#line default
#line hidden

#line 187 "ErrorPage.cshtml"
                         foreach (var kv in Model.Cookies.OrderBy(kv => kv.Key))
                        {

#line default
#line hidden

            WriteLiteral("                            <tr>\r\n                                <td>");
#line 190 "ErrorPage.cshtml"
                               Write(kv.Key);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                <td>");
#line 191 "ErrorPage.cshtml"
                               Write(kv.Value);

#line default
#line hidden
            WriteLiteral("</td>\r\n                            </tr>\r\n");
#line 193 "ErrorPage.cshtml"
                        }

#line default
#line hidden

            WriteLiteral("                    </tbody>\r\n                </table>\r\n");
#line 196 "ErrorPage.cshtml"
            }
            else
            {

#line default
#line hidden

            WriteLiteral("                <p>");
#line 199 "ErrorPage.cshtml"
              Write(Resources.ErrorPageHtml_NoCookieData);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 200 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("        </div>\r\n        <div id=\"headerspage\" class=\"page\">\r\n");
#line 203 "ErrorPage.cshtml"
            

#line default
#line hidden

#line 203 "ErrorPage.cshtml"
             if (Model.Headers.Any())
            {

#line default
#line hidden

            WriteLiteral("                <table>\r\n                    <thead>\r\n                        <tr>\r\n                            <th>");
#line 208 "ErrorPage.cshtml"
                           Write(Resources.ErrorPageHtml_VariableColumn);

#line default
#line hidden
            WriteLiteral("</th>\r\n                            <th>");
#line 209 "ErrorPage.cshtml"
                           Write(Resources.ErrorPageHtml_ValueColumn);

#line default
#line hidden
            WriteLiteral("</th>\r\n                        </tr>\r\n                    </thead>\r\n                    <tbody>\r\n");
#line 213 "ErrorPage.cshtml"
                        

#line default
#line hidden

#line 213 "ErrorPage.cshtml"
                         foreach (var kv in Model.Headers.OrderBy(kv => kv.Key))
                        {
                            foreach (var v in kv.Value)
                            {

#line default
#line hidden

            WriteLiteral("                                <tr>\r\n                                    <td>");
#line 218 "ErrorPage.cshtml"
                                   Write(kv.Key);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                    <td>");
#line 219 "ErrorPage.cshtml"
                                   Write(v);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                </tr>\r\n");
#line 221 "ErrorPage.cshtml"
                            }
                        }

#line default
#line hidden

            WriteLiteral("                    </tbody>\r\n                </table>\r\n");
#line 225 "ErrorPage.cshtml"
            }
            else
            {

#line default
#line hidden

            WriteLiteral("                <p>");
#line 228 "ErrorPage.cshtml"
              Write(Resources.ErrorPageHtml_NoHeaderData);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 229 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("        </div>\r\n        <script>\r\n            //<!--\r\n            (function (window, undefined) {\r\n    \"use strict\";\r\n\r\n    function ns(selector, element) {\r\n        return new NodeCollection(selector, element);\r\n    }\r\n\r\n    function NodeCollection(selector, element) {\r\n        this.items = [];\r\n        element = element || window.document;\r\n\r\n        var nodeList;\r\n\r\n        if (typeof (selector) === \"string\") {\r\n            nodeList = element.querySelectorAll(selector);\r\n            for (var i = 0, l = nodeList.length; i < l; i++) {\r\n                this.items.push(nodeList.item(i));\r\n            }\r\n        }\r\n    }\r\n\r\n    NodeCollection.prototype = {\r\n        each: function (callback) {\r\n            for (var i = 0, l = this.items.length; i < l; i++) {\r\n                callback(this.items[i], i);\r\n            }\r\n            return this;\r\n        },\r\n\r\n        children: function (selector) {\r\n            var children = [];\r\n\r\n            this.each(function (el) {\r\n                children = children.concat(ns(selector, el).items);\r\n            });\r\n\r\n            return ns(children);\r\n        },\r\n\r\n        hide: function () {\r\n            this.each(function (el) {\r\n                el.style.display = \"none\";\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        toggle: function () {\r\n            this.each(function (el) {\r\n                el.style.display = el.style.display === \"none\" ? \"\" : \"none\";\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        show: function () {\r\n            this.each(function (el) {\r\n                el.style.display = \"\";\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        addClass: function (className) {\r\n            this.each(function (el) {\r\n                var existingClassName = el.className,\r\n                    classNames;\r\n                if (!existingClassName) {\r\n                    el.className = className;\r\n                } else {\r\n                    classNames = existingClassName.split(\" \");\r\n                    if (classNames.indexOf(className) < 0) {\r\n                        el.className = existingClassName + \" \" + className;\r\n                    }\r\n                }\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        removeClass: function (className) {\r\n            this.each(function (el) {\r\n                var existingClassName = el.className,\r\n                    classNames, index;\r\n                if (existingClassName === className) {\r\n                    el.className = \"\";\r\n                } else if (existingClassName) {\r\n                    classNames = existingClassName.split(\" \");\r\n                    index = classNames.indexOf(className);\r\n                    if (index > 0) {\r\n                        classNames.splice(index, 1);\r\n                        el.className = classNames.join(\" \");\r\n                    }\r\n                }\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        attr: function (name) {\r\n            if (this.items.length === 0) {\r\n                return null;\r\n            }\r\n\r\n            return this.items[0].getAttribute(name);\r\n        },\r\n\r\n        on: function (eventName, handler) {\r\n            this.each(function (el, idx) {\r\n                var callback = function (e) {\r\n                    e = e || window.event;\r\n                    if (!e.which && e.keyCode) {\r\n                        e.which = e.keyCode; // Normalize IE8 key events\r\n                    }\r\n                    handler.apply(el, [e]);\r\n                };\r\n\r\n                if (el.addEventListener) { // DOM Events\r\n                    el.addEventListener(eventName, callback, false);\r\n                } else if (el.attachEvent) { // IE8 events\r\n                    el.attachEvent(\"on\" + eventName, callback);\r\n                } else {\r\n                    el[\"on\" + type] = callback;\r\n                }\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        click: function (handler) {\r\n            return this.on(\"click\", handler);\r\n        },\r\n\r\n        keypress: function (handler) {\r\n            return this.on(\"keypress\", handler);\r\n        }\r\n    };\r\n\r\n    function frame(el) {\r\n        ns(el).children(\".source .collapsible\").toggle();\r\n    }\r\n\r\n    function tab(el) {\r\n        var unselected = ns(\"#header .selected\").removeClass(\"selected\").attr(\"id\");\r\n        var selected = ns(\"#\" + el.id).addClass(\"selected\").attr(\"id\");\r\n\r\n        ns(\"#\" + unselected + \"page\").hide();\r\n        ns(\"#\" + selected + \"page\").show();\r\n    }\r\n\r\n    ns(\".collapsible\").hide();\r\n    ns(\".page\").hide();\r\n    ns(\"#stackpage\").show();\r\n\r\n    ns(\"#rawExceptionButton\").click(function (event) {\r\n        var div = document.getElementById('rawException');\r\n        div.style.display = 'inline-block';\r\n        div.scrollIntoView(true);\r\n        event.preventDefault();\r\n        event.stopPropagation();\r\n\r\n        return false;\r\n    });\r\n\r\n    ns(\".frame\")\r\n        .click(function () {\r\n            frame(this);\r\n        })\r\n        .keypress(function (e) {\r\n            if (e.which === 13) {\r\n                frame(this);\r\n            }\r\n        });\r\n\r\n    ns(\"#header li\")\r\n        .click(function () {\r\n            tab(this);\r\n        })\r\n        .keypress(function (e) {\r\n            if (e.which === 13) {\r\n                tab(this);\r\n            }\r\n        });\r\n})(window);\r\n            //-->\r\n        </script>\r\n    </body>\r\n</html>\r\n");
        }
        #pragma warning restore 1998
    }
}
