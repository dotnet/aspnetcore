namespace Microsoft.AspNetCore.Diagnostics.RazorViews
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
using System.Reflection

#line default
#line hidden
    ;
#line 6 "ErrorPage.cshtml"
using Microsoft.AspNetCore.Diagnostics.RazorViews

#line default
#line hidden
    ;
#line 7 "ErrorPage.cshtml"
using Microsoft.AspNetCore.Diagnostics

#line default
#line hidden
    ;
    using System.Threading.Tasks;

    internal class ErrorPage : Microsoft.Extensions.RazorViews.BaseView
    {
#line 9 "ErrorPage.cshtml"

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
#line 17 "ErrorPage.cshtml"
  
    // TODO: Response.ReasonPhrase = "Internal Server Error";
    Response.ContentType = "text/html; charset=utf-8";
    string location = string.Empty;

#line default
#line hidden

            WriteLiteral("<!DOCTYPE html>\r\n<html");
            BeginWriteAttribute("lang", " lang=\"", 536, "\"", 597, 1);
#line 23 "ErrorPage.cshtml"
WriteAttributeValue("", 543, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, 543, 54, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" xmlns=\"http://www.w3.org/1999/xhtml\">\r\n    <head>\r\n        <meta charset=\"utf-8\" />\r\n        <title>");
#line 26 "ErrorPage.cshtml"
          Write(Resources.ErrorPageHtml_Title);

#line default
#line hidden
            WriteLiteral(@"</title>
        <style>
            body {
    font-family: 'Segoe UI', Tahoma, Arial, Helvetica, sans-serif;
    font-size: .813em;
    color: #222;
    background-color: #fff;
}

h1, h2, h3, h4, h5 {
    /*font-family: 'Segoe UI',Tahoma,Arial,Helvetica,sans-serif;*/
    font-weight: 100;
}

h1 {
    color: #44525e;
    margin: 15px 0 15px 0;
}

h2 {
    margin: 10px 5px 0 0;
}

h3 {
    color: #363636;
    margin: 5px 5px 0 0;
}

code {
    font-family: Consolas, ""Courier New"", courier, monospace;
}

body .titleerror {
    padding: 3px 3px 6px 3px;
    display: block;
    font-size: 1.5em;
    font-weight: 100;
}

body .location {
    margin: 3px 0 10px 30px;
}

#header {
    font-size: 18px;
    padding: 15px 0;
    border-top: 1px #ddd solid;
    border-bottom: 1px #ddd solid;
    margin-bottom: 0;
}

    #header li {
        display: inline;
        margin: 5px;
        padding: 5px;
        color: #a0a0a0;
        cursor: pointer;
    }

    #h");
            WriteLiteral(@"eader .selected {
        background: #44c5f2;
        color: #fff;
    }

#stackpage ul {
    list-style: none;
    padding-left: 0;
    margin: 0;
    /*border-bottom: 1px #ddd solid;*/
}

#stackpage .details {
    font-size: 1.2em;
    padding: 3px;
    color: #000;
}

#stackpage .stackerror {
    padding: 5px;
    border-bottom: 1px #ddd solid;
}


#stackpage .frame {
    padding: 0;
    margin: 0 0 0 30px;
}

    #stackpage .frame h3 {
        padding: 2px;
        margin: 0;
    }

#stackpage .source {
    padding: 0 0 0 30px;
}

    #stackpage .source ol li {
        font-family: Consolas, ""Courier New"", courier, monospace;
        white-space: pre;
        background-color: #fbfbfb;
    }

#stackpage .frame .source .highlight li span {
    color: #FF0000;
}

#stackpage .source ol.collapsible li {
    color: #888;
}

    #stackpage .source ol.collapsible li span {
        color: #606060;
    }

.page table {
    border-collapse: separate;
    bo");
            WriteLiteral(@"rder-spacing: 0;
    margin: 0 0 20px;
}

.page th {
    vertical-align: bottom;
    padding: 10px 5px 5px 5px;
    font-weight: 400;
    color: #a0a0a0;
    text-align: left;
}

.page td {
    padding: 3px 10px;
}

.page th, .page td {
    border-right: 1px #ddd solid;
    border-bottom: 1px #ddd solid;
    border-left: 1px transparent solid;
    border-top: 1px transparent solid;
    box-sizing: border-box;
}

    .page th:last-child, .page td:last-child {
        border-right: 1px transparent solid;
    }

.page .length {
    text-align: right;
}

a {
    color: #1ba1e2;
    text-decoration: none;
}

    a:hover {
        color: #13709e;
        text-decoration: underline;
    }

.showRawException {
    cursor: pointer;
    color: #44c5f2;
    background-color: transparent;
    font-size: 1.2em;
    text-align: left;
    text-decoration: none;
    display: inline-block;
    border: 0;
    padding: 0;
}

.rawExceptionStackTrace {
    font-size: 1.2em;
");
            WriteLiteral(@"}

.rawExceptionBlock {
    border-top: 1px #ddd solid;
    border-bottom: 1px #ddd solid;
}

.showRawExceptionContainer {
    margin-top: 10px;
    margin-bottom: 10px;
}

.expandCollapseButton {
    cursor: pointer;
    float: left;
    height: 16px;
    width: 16px;
    font-size: 10px;
    position: absolute;
    left: 10px;
    background-color: #eee;
    padding: 0;
    border: 0;
    margin: 0;
}

        </style>
    </head>
    <body>
        <h1>");
#line 228 "ErrorPage.cshtml"
       Write(Resources.ErrorPageHtml_UnhandledException);

#line default
#line hidden
            WriteLiteral("</h1>\r\n");
#line 229 "ErrorPage.cshtml"
        

#line default
#line hidden

#line 229 "ErrorPage.cshtml"
         foreach (var errorDetail in Model.ErrorDetails)
        {

#line default
#line hidden

            WriteLiteral("            <div class=\"titleerror\">");
#line 231 "ErrorPage.cshtml"
                               Write(errorDetail.Error.GetType().Name);

#line default
#line hidden
            WriteLiteral(": ");
#line 231 "ErrorPage.cshtml"
                                                                          Output.Write(HtmlEncodeAndReplaceLineBreaks(errorDetail.Error.Message)); 

#line default
#line hidden

            WriteLiteral("</div>\r\n");
#line 232 "ErrorPage.cshtml"
            

#line default
#line hidden

#line 232 "ErrorPage.cshtml"
              
                var firstFrame = errorDetail.StackFrames.FirstOrDefault();
                if (firstFrame != null)
                {
                    location = firstFrame.Function;
                }
            

#line default
#line hidden

#line 238 "ErrorPage.cshtml"
             
            if (!string.IsNullOrEmpty(location) && firstFrame != null && !string.IsNullOrEmpty(firstFrame.File))
            {

#line default
#line hidden

            WriteLiteral("                <p class=\"location\">");
#line 241 "ErrorPage.cshtml"
                               Write(location);

#line default
#line hidden
            WriteLiteral(" in <code");
            BeginWriteAttribute("title", " title=\"", 4987, "\"", 5011, 1);
#line 241 "ErrorPage.cshtml"
WriteAttributeValue("", 4995, firstFrame.File, 4995, 16, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">");
#line 241 "ErrorPage.cshtml"
                                                                           Write(System.IO.Path.GetFileName(firstFrame.File));

#line default
#line hidden
            WriteLiteral("</code>, line ");
#line 241 "ErrorPage.cshtml"
                                                                                                                                     Write(firstFrame.Line);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 242 "ErrorPage.cshtml"
            }
            else if (!string.IsNullOrEmpty(location))
            {

#line default
#line hidden

            WriteLiteral("                <p class=\"location\">");
#line 245 "ErrorPage.cshtml"
                               Write(location);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 246 "ErrorPage.cshtml"
            }
            else
            {

#line default
#line hidden

            WriteLiteral("                <p class=\"location\">");
#line 249 "ErrorPage.cshtml"
                               Write(Resources.ErrorPageHtml_UnknownLocation);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 250 "ErrorPage.cshtml"
            }

            var reflectionTypeLoadException = errorDetail.Error as ReflectionTypeLoadException;
            if (reflectionTypeLoadException != null)
            {
                if (reflectionTypeLoadException.LoaderExceptions.Length > 0)
                {

#line default
#line hidden

            WriteLiteral("                    <h3>Loader Exceptions:</h3>\r\n                    <ul>\r\n");
#line 259 "ErrorPage.cshtml"
                        

#line default
#line hidden

#line 259 "ErrorPage.cshtml"
                         foreach (var ex in reflectionTypeLoadException.LoaderExceptions)
                        {

#line default
#line hidden

            WriteLiteral("                            <li>");
#line 261 "ErrorPage.cshtml"
                           Write(ex.Message);

#line default
#line hidden
            WriteLiteral("</li>\r\n");
#line 262 "ErrorPage.cshtml"
                        }

#line default
#line hidden

            WriteLiteral("                    </ul>\r\n");
#line 264 "ErrorPage.cshtml"
                }
            }
        }

#line default
#line hidden

            WriteLiteral("        <ul id=\"header\">\r\n            <li id=\"stack\" tabindex=\"1\" class=\"selected\">\r\n                ");
#line 269 "ErrorPage.cshtml"
           Write(Resources.ErrorPageHtml_StackButton);

#line default
#line hidden
            WriteLiteral("\r\n            </li>\r\n            <li id=\"query\" tabindex=\"2\">\r\n                ");
#line 272 "ErrorPage.cshtml"
           Write(Resources.ErrorPageHtml_QueryButton);

#line default
#line hidden
            WriteLiteral("\r\n            </li>\r\n            <li id=\"cookies\" tabindex=\"3\">\r\n                ");
#line 275 "ErrorPage.cshtml"
           Write(Resources.ErrorPageHtml_CookiesButton);

#line default
#line hidden
            WriteLiteral("\r\n            </li>\r\n            <li id=\"headers\" tabindex=\"4\">\r\n                ");
#line 278 "ErrorPage.cshtml"
           Write(Resources.ErrorPageHtml_HeadersButton);

#line default
#line hidden
            WriteLiteral("\r\n            </li>\r\n        </ul>\r\n\r\n        <div id=\"stackpage\" class=\"page\">\r\n            <ul>\r\n");
#line 284 "ErrorPage.cshtml"
                

#line default
#line hidden

#line 284 "ErrorPage.cshtml"
                  
                    var exceptionCount = 0;
                    var stackFrameCount = 0;
                    var exceptionDetailId = "";
                    var frameId = "";
                

#line default
#line hidden

            WriteLiteral("                ");
#line 290 "ErrorPage.cshtml"
                 foreach (var errorDetail in Model.ErrorDetails)
                {
                    

#line default
#line hidden

#line 292 "ErrorPage.cshtml"
                      
                        exceptionCount++;
                        exceptionDetailId = "exceptionDetail" + exceptionCount;
                    

#line default
#line hidden

#line 295 "ErrorPage.cshtml"
                     

#line default
#line hidden

            WriteLiteral("                    <li>\r\n                        <h2 class=\"stackerror\">");
#line 297 "ErrorPage.cshtml"
                                          Write(errorDetail.Error.GetType().Name);

#line default
#line hidden
            WriteLiteral(": ");
#line 297 "ErrorPage.cshtml"
                                                                             Write(errorDetail.Error.Message);

#line default
#line hidden
            WriteLiteral("</h2>\r\n                        <ul>\r\n");
#line 299 "ErrorPage.cshtml"
                        

#line default
#line hidden

#line 299 "ErrorPage.cshtml"
                         foreach (var frame in errorDetail.StackFrames)
                        {
                            

#line default
#line hidden

#line 301 "ErrorPage.cshtml"
                              
                                stackFrameCount++;
                                frameId = "frame" + stackFrameCount;
                            

#line default
#line hidden

#line 304 "ErrorPage.cshtml"
                             

#line default
#line hidden

            WriteLiteral("                            <li class=\"frame\"");
            BeginWriteAttribute("id", " id=\"", 7545, "\"", 7558, 1);
#line 305 "ErrorPage.cshtml"
WriteAttributeValue("", 7550, frameId, 7550, 8, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">\r\n");
#line 306 "ErrorPage.cshtml"
                                

#line default
#line hidden

#line 306 "ErrorPage.cshtml"
                                 if (string.IsNullOrEmpty(frame.File))
                                {

#line default
#line hidden

            WriteLiteral("                                    <h3>");
#line 308 "ErrorPage.cshtml"
                                   Write(frame.Function);

#line default
#line hidden
            WriteLiteral("</h3>\r\n");
#line 309 "ErrorPage.cshtml"
                                }
                                else
                                {

#line default
#line hidden

            WriteLiteral("                                    <h3>");
#line 312 "ErrorPage.cshtml"
                                   Write(frame.Function);

#line default
#line hidden
            WriteLiteral(" in <code");
            BeginWriteAttribute("title", " title=\"", 7903, "\"", 7922, 1);
#line 312 "ErrorPage.cshtml"
WriteAttributeValue("", 7911, frame.File, 7911, 11, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">");
#line 312 "ErrorPage.cshtml"
                                                                                Write(System.IO.Path.GetFileName(frame.File));

#line default
#line hidden
            WriteLiteral("</code></h3>\r\n");
#line 313 "ErrorPage.cshtml"
                                }

#line default
#line hidden

            WriteLiteral("\r\n");
#line 315 "ErrorPage.cshtml"
                                

#line default
#line hidden

#line 315 "ErrorPage.cshtml"
                                 if (frame.Line != 0 && frame.ContextCode.Any())
                                {

#line default
#line hidden

            WriteLiteral("                                    <button class=\"expandCollapseButton\" data-frameId=\"");
#line 317 "ErrorPage.cshtml"
                                                                                  Write(frameId);

#line default
#line hidden
            WriteLiteral("\">+</button>\r\n                                    <div class=\"source\">\r\n");
#line 319 "ErrorPage.cshtml"
                                        

#line default
#line hidden

#line 319 "ErrorPage.cshtml"
                                         if (frame.PreContextCode.Any())
                                        {

#line default
#line hidden

            WriteLiteral("                                            <ol");
            BeginWriteAttribute("start", " start=\"", 8462, "\"", 8491, 1);
#line 321 "ErrorPage.cshtml"
WriteAttributeValue("", 8470, frame.PreContextLine, 8470, 21, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" class=\"collapsible\">\r\n");
#line 322 "ErrorPage.cshtml"
                                                

#line default
#line hidden

#line 322 "ErrorPage.cshtml"
                                                 foreach (var line in frame.PreContextCode)
                                                {

#line default
#line hidden

            WriteLiteral("                                                    <li><span>");
#line 324 "ErrorPage.cshtml"
                                                         Write(line);

#line default
#line hidden
            WriteLiteral("</span></li>\r\n");
#line 325 "ErrorPage.cshtml"
                                                }

#line default
#line hidden

            WriteLiteral("                                            </ol>\r\n");
#line 327 "ErrorPage.cshtml"
                                        }

#line default
#line hidden

            WriteLiteral("\r\n                                        <ol");
            BeginWriteAttribute("start", " start=\"", 8930, "\"", 8949, 1);
#line 329 "ErrorPage.cshtml"
WriteAttributeValue("", 8938, frame.Line, 8938, 11, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" class=\"highlight\">\r\n");
#line 330 "ErrorPage.cshtml"
                                            

#line default
#line hidden

#line 330 "ErrorPage.cshtml"
                                             foreach (var line in frame.ContextCode)
                                            {

#line default
#line hidden

            WriteLiteral("                                                <li><span>");
#line 332 "ErrorPage.cshtml"
                                                     Write(line);

#line default
#line hidden
            WriteLiteral("</span></li>\r\n");
#line 333 "ErrorPage.cshtml"
                                            }

#line default
#line hidden

            WriteLiteral("                                        </ol>\r\n\r\n");
#line 336 "ErrorPage.cshtml"
                                        

#line default
#line hidden

#line 336 "ErrorPage.cshtml"
                                         if (frame.PostContextCode.Any())
                                        {

#line default
#line hidden

            WriteLiteral("                                            <ol");
            BeginWriteAttribute("start", " start=\'", 9442, "\'", 9467, 1);
#line 338 "ErrorPage.cshtml"
WriteAttributeValue("", 9450, frame.Line + 1, 9450, 17, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" class=\"collapsible\">\r\n");
#line 339 "ErrorPage.cshtml"
                                                

#line default
#line hidden

#line 339 "ErrorPage.cshtml"
                                                 foreach (var line in frame.PostContextCode)
                                                {

#line default
#line hidden

            WriteLiteral("                                                    <li><span>");
#line 341 "ErrorPage.cshtml"
                                                         Write(line);

#line default
#line hidden
            WriteLiteral("</span></li>\r\n");
#line 342 "ErrorPage.cshtml"
                                                }

#line default
#line hidden

            WriteLiteral("                                            </ol>\r\n");
#line 344 "ErrorPage.cshtml"
                                        }

#line default
#line hidden

            WriteLiteral("                                    </div>\r\n");
#line 346 "ErrorPage.cshtml"
                                }

#line default
#line hidden

            WriteLiteral("                            </li>\r\n");
#line 348 "ErrorPage.cshtml"
                        }

#line default
#line hidden

            WriteLiteral(@"                        </ul>
                    </li>
                    <li>
                        <br/>
                        <div class=""rawExceptionBlock"">
                            <div class=""showRawExceptionContainer"">
                                <button class=""showRawException"" data-exceptionDetailId=""");
#line 355 "ErrorPage.cshtml"
                                                                                    Write(exceptionDetailId);

#line default
#line hidden
            WriteLiteral("\">Show raw exception details</button>\r\n                            </div>\r\n                            <div");
            BeginWriteAttribute("id", " id=\"", 10458, "\"", 10481, 1);
#line 357 "ErrorPage.cshtml"
WriteAttributeValue("", 10463, exceptionDetailId, 10463, 18, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" class=\"rawExceptionDetails\">\r\n                                <pre class=\"rawExceptionStackTrace\">");
#line 358 "ErrorPage.cshtml"
                                                               Write(errorDetail.Error.ToString());

#line default
#line hidden
            WriteLiteral("</pre>\r\n                            </div>\r\n                        </div>\r\n                    </li>\r\n");
#line 362 "ErrorPage.cshtml"
                }

#line default
#line hidden

            WriteLiteral("            </ul>\r\n        </div>\r\n\r\n        <div id=\"querypage\" class=\"page\">\r\n");
#line 367 "ErrorPage.cshtml"
            

#line default
#line hidden

#line 367 "ErrorPage.cshtml"
             if (Model.Query.Any())
            {

#line default
#line hidden

            WriteLiteral("                <table>\r\n                    <thead>\r\n                        <tr>\r\n                            <th>");
#line 372 "ErrorPage.cshtml"
                           Write(Resources.ErrorPageHtml_VariableColumn);

#line default
#line hidden
            WriteLiteral("</th>\r\n                            <th>");
#line 373 "ErrorPage.cshtml"
                           Write(Resources.ErrorPageHtml_ValueColumn);

#line default
#line hidden
            WriteLiteral("</th>\r\n                        </tr>\r\n                    </thead>\r\n                    <tbody>\r\n");
#line 377 "ErrorPage.cshtml"
                        

#line default
#line hidden

#line 377 "ErrorPage.cshtml"
                         foreach (var kv in Model.Query.OrderBy(kv => kv.Key))
                        {
                            foreach (var v in kv.Value)
                            {

#line default
#line hidden

            WriteLiteral("                                <tr>\r\n                                    <td>");
#line 382 "ErrorPage.cshtml"
                                   Write(kv.Key);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                    <td>");
#line 383 "ErrorPage.cshtml"
                                   Write(v);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                </tr>\r\n");
#line 385 "ErrorPage.cshtml"
                            }
                        }

#line default
#line hidden

            WriteLiteral("                    </tbody>\r\n                </table>\r\n");
#line 389 "ErrorPage.cshtml"
            }
            else
            {

#line default
#line hidden

            WriteLiteral("                <p>");
#line 392 "ErrorPage.cshtml"
              Write(Resources.ErrorPageHtml_NoQueryStringData);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 393 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("        </div>\r\n\r\n        <div id=\"cookiespage\" class=\"page\">\r\n");
#line 397 "ErrorPage.cshtml"
            

#line default
#line hidden

#line 397 "ErrorPage.cshtml"
             if (Model.Cookies.Any())
            {

#line default
#line hidden

            WriteLiteral("                <table>\r\n                    <thead>\r\n                        <tr>\r\n                            <th>");
#line 402 "ErrorPage.cshtml"
                           Write(Resources.ErrorPageHtml_VariableColumn);

#line default
#line hidden
            WriteLiteral("</th>\r\n                            <th>");
#line 403 "ErrorPage.cshtml"
                           Write(Resources.ErrorPageHtml_ValueColumn);

#line default
#line hidden
            WriteLiteral("</th>\r\n                        </tr>\r\n                    </thead>\r\n                    <tbody>\r\n");
#line 407 "ErrorPage.cshtml"
                        

#line default
#line hidden

#line 407 "ErrorPage.cshtml"
                         foreach (var kv in Model.Cookies.OrderBy(kv => kv.Key))
                        {

#line default
#line hidden

            WriteLiteral("                            <tr>\r\n                                <td>");
#line 410 "ErrorPage.cshtml"
                               Write(kv.Key);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                <td>");
#line 411 "ErrorPage.cshtml"
                               Write(kv.Value);

#line default
#line hidden
            WriteLiteral("</td>\r\n                            </tr>\r\n");
#line 413 "ErrorPage.cshtml"
                        }

#line default
#line hidden

            WriteLiteral("                    </tbody>\r\n                </table>\r\n");
#line 416 "ErrorPage.cshtml"
            }
            else
            {

#line default
#line hidden

            WriteLiteral("                <p>");
#line 419 "ErrorPage.cshtml"
              Write(Resources.ErrorPageHtml_NoCookieData);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 420 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("        </div>\r\n        <div id=\"headerspage\" class=\"page\">\r\n");
#line 423 "ErrorPage.cshtml"
            

#line default
#line hidden

#line 423 "ErrorPage.cshtml"
             if (Model.Headers.Any())
            {

#line default
#line hidden

            WriteLiteral("                <table>\r\n                    <thead>\r\n                        <tr>\r\n                            <th>");
#line 428 "ErrorPage.cshtml"
                           Write(Resources.ErrorPageHtml_VariableColumn);

#line default
#line hidden
            WriteLiteral("</th>\r\n                            <th>");
#line 429 "ErrorPage.cshtml"
                           Write(Resources.ErrorPageHtml_ValueColumn);

#line default
#line hidden
            WriteLiteral("</th>\r\n                        </tr>\r\n                    </thead>\r\n                    <tbody>\r\n");
#line 433 "ErrorPage.cshtml"
                        

#line default
#line hidden

#line 433 "ErrorPage.cshtml"
                         foreach (var kv in Model.Headers.OrderBy(kv => kv.Key))
                        {
                            foreach (var v in kv.Value)
                            {

#line default
#line hidden

            WriteLiteral("                                <tr>\r\n                                    <td>");
#line 438 "ErrorPage.cshtml"
                                   Write(kv.Key);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                    <td>");
#line 439 "ErrorPage.cshtml"
                                   Write(v);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                </tr>\r\n");
#line 441 "ErrorPage.cshtml"
                            }
                        }

#line default
#line hidden

            WriteLiteral("                    </tbody>\r\n                </table>\r\n");
#line 445 "ErrorPage.cshtml"
            }
            else
            {

#line default
#line hidden

            WriteLiteral("                <p>");
#line 448 "ErrorPage.cshtml"
              Write(Resources.ErrorPageHtml_NoHeaderData);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 449 "ErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral(@"        </div>
        <script>
            //<!--
            (function (window, undefined) {
    ""use strict"";

    function ns(selector, element) {
        return new NodeCollection(selector, element);
    }

    function NodeCollection(selector, element) {
        this.items = [];
        element = element || window.document;

        var nodeList;

        if (typeof (selector) === ""string"") {
            nodeList = element.querySelectorAll(selector);
            for (var i = 0, l = nodeList.length; i < l; i++) {
                this.items.push(nodeList.item(i));
            }
        }
    }

    NodeCollection.prototype = {
        each: function (callback) {
            for (var i = 0, l = this.items.length; i < l; i++) {
                callback(this.items[i], i);
            }
            return this;
        },

        children: function (selector) {
            var children = [];

            this.each(function (el) {
                children = children.concat(");
            WriteLiteral(@"ns(selector, el).items);
            });

            return ns(children);
        },

        hide: function () {
            this.each(function (el) {
                el.style.display = ""none"";
            });

            return this;
        },

        toggle: function () {
            this.each(function (el) {
                el.style.display = el.style.display === ""none"" ? """" : ""none"";
            });

            return this;
        },

        show: function () {
            this.each(function (el) {
                el.style.display = """";
            });

            return this;
        },

        addClass: function (className) {
            this.each(function (el) {
                var existingClassName = el.className,
                    classNames;
                if (!existingClassName) {
                    el.className = className;
                } else {
                    classNames = existingClassName.split("" "");
                    if (classNames.i");
            WriteLiteral(@"ndexOf(className) < 0) {
                        el.className = existingClassName + "" "" + className;
                    }
                }
            });

            return this;
        },

        removeClass: function (className) {
            this.each(function (el) {
                var existingClassName = el.className,
                    classNames, index;
                if (existingClassName === className) {
                    el.className = """";
                } else if (existingClassName) {
                    classNames = existingClassName.split("" "");
                    index = classNames.indexOf(className);
                    if (index > 0) {
                        classNames.splice(index, 1);
                        el.className = classNames.join("" "");
                    }
                }
            });

            return this;
        },

        attr: function (name) {
            if (this.items.length === 0) {
                return null;
          ");
            WriteLiteral(@"  }

            return this.items[0].getAttribute(name);
        },

        on: function (eventName, handler) {
            this.each(function (el, idx) {
                var callback = function (e) {
                    e = e || window.event;
                    if (!e.which && e.keyCode) {
                        e.which = e.keyCode; // Normalize IE8 key events
                    }
                    handler.apply(el, [e]);
                };

                if (el.addEventListener) { // DOM Events
                    el.addEventListener(eventName, callback, false);
                } else if (el.attachEvent) { // IE8 events
                    el.attachEvent(""on"" + eventName, callback);
                } else {
                    el[""on"" + type] = callback;
                }
            });

            return this;
        },

        click: function (handler) {
            return this.on(""click"", handler);
        },

        keypress: function (handler) {
           ");
            WriteLiteral(@" return this.on(""keypress"", handler);
        }
    };

    function frame(el) {
        ns("".source .collapsible"", el).toggle();
    }

    function expandCollapseButton(el) {
        var frameId = el.getAttribute(""data-frameId"");
        frame(document.getElementById(frameId));
        if (el.innerText === ""+"") {
            el.innerText = ""-"";
        }
        else {
            el.innerText = ""+"";
        }
    }

    function tab(el) {
        var unselected = ns(""#header .selected"").removeClass(""selected"").attr(""id"");
        var selected = ns(""#"" + el.id).addClass(""selected"").attr(""id"");

        ns(""#"" + unselected + ""page"").hide();
        ns(""#"" + selected + ""page"").show();
    }

    ns("".rawExceptionDetails"").hide();
    ns("".collapsible"").hide();
    ns("".page"").hide();
    ns(""#stackpage"").show();

    ns("".expandCollapseButton"")
        .click(function () {
            expandCollapseButton(this);
        })
        .keypress(function (e) {
            if (e");
            WriteLiteral(@".which === 13) {
                expandCollapseButton(this);
            }
        });

    ns(""#header li"")
        .click(function () {
            tab(this);
        })
        .keypress(function (e) {
            if (e.which === 13) {
                tab(this);
            }
        });

    ns("".showRawException"")
        .click(function () {
            var exceptionDetailId = this.getAttribute(""data-exceptionDetailId"");
            ns(""#"" + exceptionDetailId).toggle();
        });
})(window);
            //-->
        </script>
    </body>
</html>
");
        }
        #pragma warning restore 1998
    }
}
