namespace Microsoft.AspNetCore.Diagnostics.Views
{
#line 1 "CompilationErrorPage.cshtml"
using System

#line default
#line hidden
    ;
#line 2 "CompilationErrorPage.cshtml"
using System.Globalization

#line default
#line hidden
    ;
#line 3 "CompilationErrorPage.cshtml"
using System.Linq

#line default
#line hidden
    ;
#line 4 "CompilationErrorPage.cshtml"
using System.Net

#line default
#line hidden
    ;
#line 5 "CompilationErrorPage.cshtml"
using Microsoft.AspNetCore.Diagnostics

#line default
#line hidden
    ;
    using System.Threading.Tasks;

    public class CompilationErrorPage : Microsoft.AspNetCore.DiagnosticsViewPage.Views.BaseView
    {
#line 7 "CompilationErrorPage.cshtml"

    public CompilationErrorPageModel Model { get; set; }

#line default
#line hidden
        #line hidden
        public CompilationErrorPage()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 10 "CompilationErrorPage.cshtml"
  
    Response.StatusCode = 500;
    Response.ContentType = "text/html; charset=utf-8";
    Response.ContentLength = null; // Clear any prior Content-Length

#line default
#line hidden

            WriteLiteral("<!DOCTYPE html>\r\n<html>\r\n    <head>\r\n        <meta charset=\"utf-8\" />\r\n        <title>");
#line 19 "CompilationErrorPage.cshtml"
          Write(Resources.ErrorPageHtml_Title);

#line default
#line hidden
            WriteLiteral("</title>\r\n        <style>\r\n            body {\r\n    font-family: 'Segoe UI', Tahoma, Arial, Helvetica, sans-serif;\r\n    font-size: .813em;\r\n    color: #222;\r\n}\r\n\r\nh1, h2, h3, h4, h5 {\r\n    /*font-family: 'Segoe UI',Tahoma,Arial,Helvetica,sans-serif;*/\r\n    font-weight: 100;\r\n}\r\n\r\nh1 {\r\n    color: #44525e;\r\n    margin: 15px 0 15px 0;\r\n}\r\n\r\nh2 {\r\n    margin: 10px 5px 0 0;\r\n}\r\n\r\nh3 {\r\n    color: #363636;\r\n    margin: 5px 5px 0 0;\r\n}\r\n\r\ncode {\r\n    font-family: Consolas, \"Courier New\", courier, monospace;\r\n}\r\n\r\nbody .titleerror {\r\n    padding: 3px 3px 6px 3px;\r\n    display: block;\r\n    font-size: 1.5em;\r\n    font-weight: 100;\r\n}\r\n\r\nbody .location {\r\n    margin: 3px 0 10px 30px;\r\n}\r\n\r\n#header {\r\n    font-size: 18px;\r\n    padding: 15px 0;\r\n    border-top: 1px #ddd solid;\r\n    border-bottom: 1px #ddd solid;\r\n    margin-bottom: 0;\r\n}\r\n\r\n    #header li {\r\n        display: inline;\r\n        margin: 5px;\r\n        padding: 5px;\r\n        color: #a0a0a0;\r\n        cursor: pointer;\r\n    }\r\n\r\n    #header .selected {\r\n        background: #44c5f2;\r\n        color: #fff;\r\n    }\r\n\r\n#stackpage ul {\r\n    list-style: none;\r\n    padding-left: 0;\r\n    margin: 0;\r\n    /*border-bottom: 1px #ddd solid;*/\r\n}\r\n\r\n#stackpage .details {\r\n    font-size: 1.2em;\r\n    padding: 3px;\r\n    color: #000;\r\n}\r\n\r\n#stackpage .stackerror {\r\n    padding: 5px;\r\n    border-bottom: 1px #ddd solid;\r\n}\r\n\r\n\r\n#stackpage .frame {\r\n    padding: 0;\r\n    margin: 0 0 0 30px;\r\n}\r\n\r\n    #stackpage .frame h3 {\r\n        padding: 2px;\r\n        margin: 0;\r\n    }\r\n\r\n#stackpage .source {\r\n    padding: 0 0 0 30px;\r\n}\r\n\r\n    #stackpage .source ol li {\r\n        font-family: Consolas, \"Courier New\", courier, monospace;\r\n        white-space: pre;\r\n        background-color: #fbfbfb;\r\n    }\r\n\r\n#stackpage .frame .source .highlight li span {\r\n    color: #FF0000;\r\n}\r\n\r\n#stackpage .source ol.collapsible li {\r\n    color: #888;\r\n}\r\n\r\n    #stackpage .source ol.collapsible li span {\r\n        color: #606060;\r\n    }\r\n\r\n.page table {\r\n    border-collapse: separate;\r\n    border-spacing: 0;\r\n    margin: 0 0 20px;\r\n}\r\n\r\n.page th {\r\n    vertical-align: bottom;\r\n    padding: 10px 5px 5px 5px;\r\n    font-weight: 400;\r\n    color: #a0a0a0;\r\n    text-align: left;\r\n}\r\n\r\n.page td {\r\n    padding: 3px 10px;\r\n}\r\n\r\n.page th, .page td {\r\n    border-right: 1px #ddd solid;\r\n    border-bottom: 1px #ddd solid;\r\n    border-left: 1px transparent solid;\r\n    border-top: 1px transparent solid;\r\n    box-sizing: border-box;\r\n}\r\n\r\n    .page th:last-child, .page td:last-child {\r\n        border-right: 1px transparent solid;\r\n    }\r\n\r\n.page .length {\r\n    text-align: right;\r\n}\r\n\r\na {\r\n    color: #1ba1e2;\r\n    text-decoration: none;\r\n}\r\n\r\n    a:hover {\r\n        color: #13709e;\r\n        text-decoration: underline;\r\n    }\r\n\r\n.showRawException {\r\n    cursor: pointer;\r\n    color: #44c5f2;\r\n    background-color: transparent;\r\n    font-size: 1.2em;\r\n    text-align: left;\r\n    text-decoration: none;\r\n    display: inline-block;\r\n    border: 0;\r\n    padding: 0;\r\n}\r\n\r\n.rawExceptionStackTrace {\r\n    font-size: 1.2em;\r\n}\r\n\r\n.rawExceptionBlock {\r\n    border-top: 1px #ddd solid;\r\n    border-bottom: 1px #ddd solid;\r\n}\r\n\r\n.showRawExceptionContainer {\r\n    margin-top: 10px;\r\n    margin-bottom: 10px;\r\n}\r\n\r\n.expandCollapseButton {\r\n    cursor: pointer;\r\n    float: left;\r\n    height: 16px;\r\n    width: 16px;\r\n    font-size: 10px;\r\n    position: absolute;\r\n    left: 10px;\r\n    background-color: #eee;\r\n    padding: 0;\r\n    border: 0;\r\n    margin: 0;\r\n}\r\n\r\n        </style>\r\n    </head>\r\n    <body>\r\n        <h1>");
#line 25 "CompilationErrorPage.cshtml"
       Write(Resources.ErrorPageHtml_CompilationException);

#line default
#line hidden
            WriteLiteral("</h1>\r\n");
#line 26 "CompilationErrorPage.cshtml"
        

#line default
#line hidden

#line 26 "CompilationErrorPage.cshtml"
         foreach (var errorDetail in Model.ErrorDetails)
        {

#line default
#line hidden

            WriteLiteral("            <div id=\"stackpage\" class=\"page\">\r\n");
#line 29 "CompilationErrorPage.cshtml"
                

#line default
#line hidden

#line 29 "CompilationErrorPage.cshtml"
                  
                    var stackFrameCount = 0;
                    var frameId = "";
                 

#line default
#line hidden

            WriteLiteral("                ");
#line 33 "CompilationErrorPage.cshtml"
                  
                    var fileName = errorDetail.StackFrames.FirstOrDefault()?.File;
                    if (!string.IsNullOrEmpty(fileName))
                    {

#line default
#line hidden

            WriteLiteral("                        <div class=\"titleerror\">");
#line 37 "CompilationErrorPage.cshtml"
                                           Write(fileName);

#line default
#line hidden
            WriteLiteral("</div>\r\n");
#line 38 "CompilationErrorPage.cshtml"
                    }
                

#line default
#line hidden

            WriteLiteral("                ");
#line 40 "CompilationErrorPage.cshtml"
                 if (!string.IsNullOrEmpty(errorDetail.ErrorMessage))
                {

#line default
#line hidden

            WriteLiteral("                    <div class=\"details\">");
#line 42 "CompilationErrorPage.cshtml"
                                    Write(errorDetail.ErrorMessage);

#line default
#line hidden
            WriteLiteral("</div>\r\n");
#line 43 "CompilationErrorPage.cshtml"
                }

#line default
#line hidden

            WriteLiteral("                <br />\r\n                <ul>\r\n");
#line 46 "CompilationErrorPage.cshtml"
                

#line default
#line hidden

#line 46 "CompilationErrorPage.cshtml"
                 foreach (var frame in errorDetail.StackFrames)
                {
                    

#line default
#line hidden

#line 48 "CompilationErrorPage.cshtml"
                      
                        stackFrameCount++;
                        frameId = "frame" + stackFrameCount;
                    

#line default
#line hidden

#line 51 "CompilationErrorPage.cshtml"
                     

#line default
#line hidden

            WriteLiteral("                    <li class=\"frame\"");
            BeginWriteAttribute("id", " id=\"", 1694, "\"", 1707, 1);
#line 52 "CompilationErrorPage.cshtml"
WriteAttributeValue("", 1699, frameId, 1699, 8, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">\r\n");
#line 53 "CompilationErrorPage.cshtml"
                        

#line default
#line hidden

#line 53 "CompilationErrorPage.cshtml"
                         if (!string.IsNullOrEmpty(frame.ErrorDetails))
                        {

#line default
#line hidden

            WriteLiteral("                            <h3>");
#line 55 "CompilationErrorPage.cshtml"
                           Write(frame.ErrorDetails);

#line default
#line hidden
            WriteLiteral("</h3>\r\n");
#line 56 "CompilationErrorPage.cshtml"
                        }

#line default
#line hidden

            WriteLiteral("\r\n");
#line 58 "CompilationErrorPage.cshtml"
                        

#line default
#line hidden

#line 58 "CompilationErrorPage.cshtml"
                         if (frame.Line != 0 && frame.ContextCode.Any())
                        {

#line default
#line hidden

            WriteLiteral("                            <button class=\"expandCollapseButton\" data-frameId=\"");
#line 60 "CompilationErrorPage.cshtml"
                                                                          Write(frameId);

#line default
#line hidden
            WriteLiteral("\">+</button>\r\n                            <div class=\"source\">\r\n");
#line 62 "CompilationErrorPage.cshtml"
                                

#line default
#line hidden

#line 62 "CompilationErrorPage.cshtml"
                                 if (frame.PreContextCode.Any())
                                {

#line default
#line hidden

            WriteLiteral("                                    <ol");
            BeginWriteAttribute("start", " start=\"", 2290, "\"", 2319, 1);
#line 64 "CompilationErrorPage.cshtml"
WriteAttributeValue("", 2298, frame.PreContextLine, 2298, 21, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" class=\"collapsible\">\r\n");
#line 65 "CompilationErrorPage.cshtml"
                                        

#line default
#line hidden

#line 65 "CompilationErrorPage.cshtml"
                                         foreach (var line in frame.PreContextCode)
                                        {

#line default
#line hidden

            WriteLiteral("                                            <li><span>");
#line 67 "CompilationErrorPage.cshtml"
                                                 Write(line);

#line default
#line hidden
            WriteLiteral("</span></li>\r\n");
#line 68 "CompilationErrorPage.cshtml"
                                        }

#line default
#line hidden

            WriteLiteral("                                    </ol>\r\n");
#line 70 "CompilationErrorPage.cshtml"
                                }

#line default
#line hidden

            WriteLiteral("                                <ol");
            BeginWriteAttribute("start", " start=\"", 2700, "\"", 2719, 1);
#line 71 "CompilationErrorPage.cshtml"
WriteAttributeValue("", 2708, frame.Line, 2708, 11, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" class=\"highlight\">\r\n");
#line 72 "CompilationErrorPage.cshtml"
                                    

#line default
#line hidden

#line 72 "CompilationErrorPage.cshtml"
                                     foreach (var line in frame.ContextCode)
                                    {

#line default
#line hidden

            WriteLiteral("                                        <li><span>");
#line 74 "CompilationErrorPage.cshtml"
                                             Write(line);

#line default
#line hidden
            WriteLiteral("</span></li>\r\n");
#line 75 "CompilationErrorPage.cshtml"
                                    }

#line default
#line hidden

            WriteLiteral("                                </ol>\r\n");
#line 77 "CompilationErrorPage.cshtml"
                                

#line default
#line hidden

#line 77 "CompilationErrorPage.cshtml"
                                 if (frame.PostContextCode.Any())
                                {

#line default
#line hidden

            WriteLiteral("                                    <ol");
            BeginWriteAttribute("start", " start=\'", 3146, "\'", 3171, 1);
#line 79 "CompilationErrorPage.cshtml"
WriteAttributeValue("", 3154, frame.Line + 1, 3154, 17, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" class=\"collapsible\">\r\n");
#line 80 "CompilationErrorPage.cshtml"
                                        

#line default
#line hidden

#line 80 "CompilationErrorPage.cshtml"
                                         foreach (var line in frame.PostContextCode)
                                        {

#line default
#line hidden

            WriteLiteral("                                            <li><span>");
#line 82 "CompilationErrorPage.cshtml"
                                                 Write(line);

#line default
#line hidden
            WriteLiteral("</span></li>\r\n");
#line 83 "CompilationErrorPage.cshtml"
                                        }

#line default
#line hidden

            WriteLiteral("                                    </ol>\r\n");
#line 85 "CompilationErrorPage.cshtml"
                                }

#line default
#line hidden

            WriteLiteral("                            </div>\r\n");
#line 87 "CompilationErrorPage.cshtml"
                        }

#line default
#line hidden

            WriteLiteral("                    </li>\r\n");
#line 89 "CompilationErrorPage.cshtml"
                }

#line default
#line hidden

            WriteLiteral("                </ul>\r\n                <br />\r\n            </div>\r\n");
#line 93 "CompilationErrorPage.cshtml"
        }

#line default
#line hidden

            WriteLiteral("        <script>\r\n            //<!--\r\n            (function (window, undefined) {\r\n    \"use strict\";\r\n\r\n    function ns(selector, element) {\r\n        return new NodeCollection(selector, element);\r\n    }\r\n\r\n    function NodeCollection(selector, element) {\r\n        this.items = [];\r\n        element = element || window.document;\r\n\r\n        var nodeList;\r\n\r\n        if (typeof (selector) === \"string\") {\r\n            nodeList = element.querySelectorAll(selector);\r\n            for (var i = 0, l = nodeList.length; i < l; i++) {\r\n                this.items.push(nodeList.item(i));\r\n            }\r\n        }\r\n    }\r\n\r\n    NodeCollection.prototype = {\r\n        each: function (callback) {\r\n            for (var i = 0, l = this.items.length; i < l; i++) {\r\n                callback(this.items[i], i);\r\n            }\r\n            return this;\r\n        },\r\n\r\n        children: function (selector) {\r\n            var children = [];\r\n\r\n            this.each(function (el) {\r\n                children = children.concat(ns(selector, el).items);\r\n            });\r\n\r\n            return ns(children);\r\n        },\r\n\r\n        hide: function () {\r\n            this.each(function (el) {\r\n                el.style.display = \"none\";\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        toggle: function () {\r\n            this.each(function (el) {\r\n                el.style.display = el.style.display === \"none\" ? \"\" : \"none\";\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        show: function () {\r\n            this.each(function (el) {\r\n                el.style.display = \"\";\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        addClass: function (className) {\r\n            this.each(function (el) {\r\n                var existingClassName = el.className,\r\n                    classNames;\r\n                if (!existingClassName) {\r\n                    el.className = className;\r\n                } else {\r\n                    classNames = existingClassName.split(\" \");\r\n                    if (classNames.indexOf(className) < 0) {\r\n                        el.className = existingClassName + \" \" + className;\r\n                    }\r\n                }\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        removeClass: function (className) {\r\n            this.each(function (el) {\r\n                var existingClassName = el.className,\r\n                    classNames, index;\r\n                if (existingClassName === className) {\r\n                    el.className = \"\";\r\n                } else if (existingClassName) {\r\n                    classNames = existingClassName.split(\" \");\r\n                    index = classNames.indexOf(className);\r\n                    if (index > 0) {\r\n                        classNames.splice(index, 1);\r\n                        el.className = classNames.join(\" \");\r\n                    }\r\n                }\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        attr: function (name) {\r\n            if (this.items.length === 0) {\r\n                return null;\r\n            }\r\n\r\n            return this.items[0].getAttribute(name);\r\n        },\r\n\r\n        on: function (eventName, handler) {\r\n            this.each(function (el, idx) {\r\n                var callback = function (e) {\r\n                    e = e || window.event;\r\n                    if (!e.which && e.keyCode) {\r\n                        e.which = e.keyCode; // Normalize IE8 key events\r\n                    }\r\n                    handler.apply(el, [e]);\r\n                };\r\n\r\n                if (el.addEventListener) { // DOM Events\r\n                    el.addEventListener(eventName, callback, false);\r\n                } else if (el.attachEvent) { // IE8 events\r\n                    el.attachEvent(\"on\" + eventName, callback);\r\n                } else {\r\n                    el[\"on\" + type] = callback;\r\n                }\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        click: function (handler) {\r\n            return this.on(\"click\", handler);\r\n        },\r\n\r\n        keypress: function (handler) {\r\n            return this.on(\"keypress\", handler);\r\n        }\r\n    };\r\n\r\n    function frame(el) {\r\n        ns(\".source .collapsible\", el).toggle();\r\n    }\r\n\r\n    function expandCollapseButton(el) {\r\n        var frameId = el.getAttribute(\"data-frameId\");\r\n        frame(document.getElementById(frameId));\r\n        if (el.innerText === \"+\") {\r\n            el.innerText = \"-\";\r\n        }\r\n        else {\r\n            el.innerText = \"+\";\r\n        }\r\n    }\r\n\r\n    function tab(el) {\r\n        var unselected = ns(\"#header .selected\").removeClass(\"selected\").attr(\"id\");\r\n        var selected = ns(\"#\" + el.id).addClass(\"selected\").attr(\"id\");\r\n\r\n        ns(\"#\" + unselected + \"page\").hide();\r\n        ns(\"#\" + selected + \"page\").show();\r\n    }\r\n\r\n    ns(\".rawExceptionDetails\").hide();\r\n    ns(\".collapsible\").hide();\r\n    ns(\".page\").hide();\r\n    ns(\"#stackpage\").show();\r\n\r\n    ns(\".expandCollapseButton\")\r\n        .click(function () {\r\n            expandCollapseButton(this);\r\n        })\r\n        .keypress(function (e) {\r\n            if (e.which === 13) {\r\n                expandCollapseButton(this);\r\n            }\r\n        });\r\n\r\n    ns(\"#header li\")\r\n        .click(function () {\r\n            tab(this);\r\n        })\r\n        .keypress(function (e) {\r\n            if (e.which === 13) {\r\n                tab(this);\r\n            }\r\n        });\r\n\r\n    ns(\".showRawException\")\r\n        .click(function () {\r\n            var exceptionDetailId = this.getAttribute(\"data-exceptionDetailId\");\r\n            ns(\"#\" + exceptionDetailId).toggle();\r\n        });\r\n})(window);\r\n            //-->\r\n        </script>\r\n    </body>\r\n</html>\r\n");
        }
        #pragma warning restore 1998
    }
}
