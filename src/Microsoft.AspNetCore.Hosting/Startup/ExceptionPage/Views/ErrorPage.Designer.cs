namespace Microsoft.AspNetCore.Hosting.Views
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
using Microsoft.AspNetCore.Hosting.Views

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
            WriteLiteral("\r\n");
#line 17 "ErrorPage.cshtml"
  
    Response.ContentType = "text/html; charset=utf-8";
    var location = string.Empty;

#line default
#line hidden

            WriteLiteral("<!DOCTYPE html>\r\n<html");
            BeginWriteAttribute("lang", " lang=\"", 422, "\"", 483, 1);
#line 22 "ErrorPage.cshtml"
WriteAttributeValue("", 429, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, 429, 54, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" xmlns=\"http://www.w3.org/1999/xhtml\">\r\n    <head>\r\n        <meta charset=\"utf-8\" />\r\n        <title>");
#line 25 "ErrorPage.cshtml"
          Write(Resources.ErrorPageHtml_Title);

#line default
#line hidden
            WriteLiteral(@"</title>
        <style>
            body {
    font-family: 'Segoe UI', Tahoma, Arial, Helvetica, sans-serif;
    font-size: .813em;
    color: #222;
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

    #header .selected {
        ba");
            WriteLiteral(@"ckground: #44c5f2;
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
    border-spacing: 0;
    margin:");
            WriteLiteral(@" 0 0 20px;
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
}

.rawExceptionBlock {
  ");
            WriteLiteral(@"  border-top: 1px #ddd solid;
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
#line 226 "ErrorPage.cshtml"
       Write(Resources.ErrorPageHtml_UnhandledException);

#line default
#line hidden
            WriteLiteral("</h1>\r\n");
#line 227 "ErrorPage.cshtml"
        

#line default
#line hidden

#line 227 "ErrorPage.cshtml"
         foreach (var errorDetail in Model.ErrorDetails)
        {

#line default
#line hidden

            WriteLiteral("            <div class=\"titleerror\">");
#line 229 "ErrorPage.cshtml"
                               Write(errorDetail.Error.GetType().Name);

#line default
#line hidden
            WriteLiteral(": ");
#line 229 "ErrorPage.cshtml"
                                                                          Output.Write(HtmlEncodeAndReplaceLineBreaks(errorDetail.Error.Message)); 

#line default
#line hidden

            WriteLiteral("</div>\r\n");
#line 230 "ErrorPage.cshtml"
            

#line default
#line hidden

#line 230 "ErrorPage.cshtml"
              
                var firstFrame = errorDetail.StackFrames.FirstOrDefault();
                if (firstFrame != null)
                {
                    location = firstFrame.Function;
                }
            

#line default
#line hidden

#line 236 "ErrorPage.cshtml"
             
            if (!string.IsNullOrEmpty(location) && firstFrame != null && !string.IsNullOrEmpty(firstFrame.File))
            {

#line default
#line hidden

            WriteLiteral("                <p class=\"location\">");
#line 239 "ErrorPage.cshtml"
                               Write(location);

#line default
#line hidden
            WriteLiteral(" in <code");
            BeginWriteAttribute("title", " title=\"", 4844, "\"", 4868, 1);
#line 239 "ErrorPage.cshtml"
WriteAttributeValue("", 4852, firstFrame.File, 4852, 16, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">");
#line 239 "ErrorPage.cshtml"
                                                                           Write(System.IO.Path.GetFileName(firstFrame.File));

#line default
#line hidden
            WriteLiteral("</code>, line ");
#line 239 "ErrorPage.cshtml"
                                                                                                                                     Write(firstFrame.Line);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 240 "ErrorPage.cshtml"
            }
            else if (!string.IsNullOrEmpty(location))
            {

#line default
#line hidden

            WriteLiteral("                <p class=\"location\">");
#line 243 "ErrorPage.cshtml"
                               Write(location);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 244 "ErrorPage.cshtml"
            }
            else
            {

#line default
#line hidden

            WriteLiteral("                <p class=\"location\">");
#line 247 "ErrorPage.cshtml"
                               Write(Resources.ErrorPageHtml_UnknownLocation);

#line default
#line hidden
            WriteLiteral("</p>\r\n");
#line 248 "ErrorPage.cshtml"
            }

            var reflectionTypeLoadException = errorDetail.Error as ReflectionTypeLoadException;
            if (reflectionTypeLoadException != null)
            {
                if (reflectionTypeLoadException.LoaderExceptions.Length > 0)
                {

#line default
#line hidden

            WriteLiteral("                    <h3>Loader Exceptions:</h3>\r\n                    <ul>\r\n");
#line 257 "ErrorPage.cshtml"
                        

#line default
#line hidden

#line 257 "ErrorPage.cshtml"
                         foreach (var ex in reflectionTypeLoadException.LoaderExceptions)
                        {

#line default
#line hidden

            WriteLiteral("                            <li>");
#line 259 "ErrorPage.cshtml"
                           Write(ex.Message);

#line default
#line hidden
            WriteLiteral("</li>\r\n");
#line 260 "ErrorPage.cshtml"
                        }

#line default
#line hidden

            WriteLiteral("                    </ul>\r\n");
#line 262 "ErrorPage.cshtml"
                }
            }
        }

#line default
#line hidden

            WriteLiteral("        <div id=\"stackpage\" class=\"page\">\r\n            <ul>\r\n");
#line 267 "ErrorPage.cshtml"
                

#line default
#line hidden

#line 267 "ErrorPage.cshtml"
                  
                    var exceptionCount = 0;
                    var stackFrameCount = 0;
                    var exceptionDetailId = "";
                    var frameId = "";
                

#line default
#line hidden

            WriteLiteral("                ");
#line 273 "ErrorPage.cshtml"
                 foreach (var errorDetail in Model.ErrorDetails)
                {
                    

#line default
#line hidden

#line 275 "ErrorPage.cshtml"
                      
                        exceptionCount++;
                        exceptionDetailId = "exceptionDetail" + exceptionCount;
                    

#line default
#line hidden

#line 278 "ErrorPage.cshtml"
                     

#line default
#line hidden

            WriteLiteral("                    <li>\r\n                        <h2 class=\"stackerror\">");
#line 280 "ErrorPage.cshtml"
                                          Write(errorDetail.Error.GetType().Name);

#line default
#line hidden
            WriteLiteral(": ");
#line 280 "ErrorPage.cshtml"
                                                                             Write(errorDetail.Error.Message);

#line default
#line hidden
            WriteLiteral("</h2>\r\n                        <ul>\r\n");
#line 282 "ErrorPage.cshtml"
                        

#line default
#line hidden

#line 282 "ErrorPage.cshtml"
                         foreach (var frame in errorDetail.StackFrames)
                        {
                            

#line default
#line hidden

#line 284 "ErrorPage.cshtml"
                              
                                stackFrameCount++;
                                frameId = "frame" + stackFrameCount;
                            

#line default
#line hidden

#line 287 "ErrorPage.cshtml"
                             

#line default
#line hidden

            WriteLiteral("                            <li class=\"frame\"");
            BeginWriteAttribute("id", " id=\"", 6874, "\"", 6887, 1);
#line 288 "ErrorPage.cshtml"
WriteAttributeValue("", 6879, frameId, 6879, 8, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">\r\n");
#line 289 "ErrorPage.cshtml"
                                

#line default
#line hidden

#line 289 "ErrorPage.cshtml"
                                 if (string.IsNullOrEmpty(frame.File))
                                {

#line default
#line hidden

            WriteLiteral("                                    <h3>");
#line 291 "ErrorPage.cshtml"
                                   Write(frame.Function);

#line default
#line hidden
            WriteLiteral("</h3>\r\n");
#line 292 "ErrorPage.cshtml"
                                }
                                else
                                {

#line default
#line hidden

            WriteLiteral("                                    <h3>");
#line 295 "ErrorPage.cshtml"
                                   Write(frame.Function);

#line default
#line hidden
            WriteLiteral(" in <code");
            BeginWriteAttribute("title", " title=\"", 7232, "\"", 7251, 1);
#line 295 "ErrorPage.cshtml"
WriteAttributeValue("", 7240, frame.File, 7240, 11, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">");
#line 295 "ErrorPage.cshtml"
                                                                                Write(System.IO.Path.GetFileName(frame.File));

#line default
#line hidden
            WriteLiteral("</code></h3>\r\n");
#line 296 "ErrorPage.cshtml"
                                }

#line default
#line hidden

            WriteLiteral("\r\n");
#line 298 "ErrorPage.cshtml"
                                

#line default
#line hidden

#line 298 "ErrorPage.cshtml"
                                 if (frame.Line != 0 && frame.ContextCode.Any())
                                {

#line default
#line hidden

            WriteLiteral("                                    <button class=\"expandCollapseButton\" data-frameId=\"");
#line 300 "ErrorPage.cshtml"
                                                                                  Write(frameId);

#line default
#line hidden
            WriteLiteral("\">+</button>\r\n                                    <div class=\"source\">\r\n");
#line 302 "ErrorPage.cshtml"
                                        

#line default
#line hidden

#line 302 "ErrorPage.cshtml"
                                         if (frame.PreContextCode.Any())
                                        {

#line default
#line hidden

            WriteLiteral("                                            <ol");
            BeginWriteAttribute("start", " start=\"", 7791, "\"", 7820, 1);
#line 304 "ErrorPage.cshtml"
WriteAttributeValue("", 7799, frame.PreContextLine, 7799, 21, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" class=\"collapsible\">\r\n");
#line 305 "ErrorPage.cshtml"
                                                

#line default
#line hidden

#line 305 "ErrorPage.cshtml"
                                                 foreach (var line in frame.PreContextCode)
                                                {

#line default
#line hidden

            WriteLiteral("                                                    <li><span>");
#line 307 "ErrorPage.cshtml"
                                                         Write(line);

#line default
#line hidden
            WriteLiteral("</span></li>\r\n");
#line 308 "ErrorPage.cshtml"
                                                }

#line default
#line hidden

            WriteLiteral("                                            </ol>\r\n");
#line 310 "ErrorPage.cshtml"
                                        }

#line default
#line hidden

            WriteLiteral("\r\n                                        <ol");
            BeginWriteAttribute("start", " start=\"", 8259, "\"", 8278, 1);
#line 312 "ErrorPage.cshtml"
WriteAttributeValue("", 8267, frame.Line, 8267, 11, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" class=\"highlight\">\r\n");
#line 313 "ErrorPage.cshtml"
                                            

#line default
#line hidden

#line 313 "ErrorPage.cshtml"
                                             foreach (var line in frame.ContextCode)
                                            {

#line default
#line hidden

            WriteLiteral("                                                <li><span>");
#line 315 "ErrorPage.cshtml"
                                                     Write(line);

#line default
#line hidden
            WriteLiteral("</span></li>\r\n");
#line 316 "ErrorPage.cshtml"
                                            }

#line default
#line hidden

            WriteLiteral("                                        </ol>\r\n\r\n");
#line 319 "ErrorPage.cshtml"
                                        

#line default
#line hidden

#line 319 "ErrorPage.cshtml"
                                         if (frame.PostContextCode.Any())
                                        {

#line default
#line hidden

            WriteLiteral("                                            <ol");
            BeginWriteAttribute("start", " start=\'", 8771, "\'", 8796, 1);
#line 321 "ErrorPage.cshtml"
WriteAttributeValue("", 8779, frame.Line + 1, 8779, 17, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" class=\"collapsible\">\r\n");
#line 322 "ErrorPage.cshtml"
                                                

#line default
#line hidden

#line 322 "ErrorPage.cshtml"
                                                 foreach (var line in frame.PostContextCode)
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

            WriteLiteral("                                    </div>\r\n");
#line 329 "ErrorPage.cshtml"
                                }

#line default
#line hidden

            WriteLiteral("                            </li>\r\n");
#line 331 "ErrorPage.cshtml"
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
#line 338 "ErrorPage.cshtml"
                                                                                    Write(exceptionDetailId);

#line default
#line hidden
            WriteLiteral("\">Show raw exception details</button>\r\n                            </div>\r\n                            <div");
            BeginWriteAttribute("id", " id=\"", 9787, "\"", 9810, 1);
#line 340 "ErrorPage.cshtml"
WriteAttributeValue("", 9792, exceptionDetailId, 9792, 18, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" class=\"rawExceptionDetails\">\r\n                                <pre class=\"rawExceptionStackTrace\">");
#line 341 "ErrorPage.cshtml"
                                                               Write(errorDetail.Error.ToString());

#line default
#line hidden
            WriteLiteral("</pre>\r\n                            </div>\r\n                        </div>\r\n                    </li>\r\n");
#line 345 "ErrorPage.cshtml"
                }

#line default
#line hidden

            WriteLiteral("            </ul>\r\n        </div>\r\n        <footer>\r\n            ");
#line 349 "ErrorPage.cshtml"
       Write(Model.RuntimeDisplayName);

#line default
#line hidden
            WriteLiteral(" ");
#line 349 "ErrorPage.cshtml"
                                 Write(Model.RuntimeArchitecture);

#line default
#line hidden
            WriteLiteral(" v");
#line 349 "ErrorPage.cshtml"
                                                              Write(Model.ClrVersion);

#line default
#line hidden
            WriteLiteral(" &nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;Microsoft.AspNetCore.Hosting version ");
#line 349 "ErrorPage.cshtml"
                                                                                                                                                           Write(Model.CurrentAssemblyVesion);

#line default
#line hidden
            WriteLiteral(" &nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp; ");
#line 349 "ErrorPage.cshtml"
                                                                                                                                                                                                                              Write(Model.OperatingSystemDescription);

#line default
#line hidden
            WriteLiteral(@" &nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;<a href=""http://go.microsoft.com/fwlink/?LinkId=517394"">Need help?</a>
        </footer>
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
   ");
            WriteLiteral(@"         var children = [];

            this.each(function (el) {
                children = children.concat(ns(selector, el).items);
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
             ");
            WriteLiteral(@"   } else {
                    classNames = existingClassName.split("" "");
                    if (classNames.indexOf(className) < 0) {
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

    ");
            WriteLiteral(@"    attr: function (name) {
            if (this.items.length === 0) {
                return null;
            }

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

        click: function (handler) {");
            WriteLiteral(@"
            return this.on(""click"", handler);
        },

        keypress: function (handler) {
            return this.on(""keypress"", handler);
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
        .click(functi");
            WriteLiteral(@"on () {
            expandCollapseButton(this);
        })
        .keypress(function (e) {
            if (e.which === 13) {
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
