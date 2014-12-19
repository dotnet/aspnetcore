namespace Microsoft.AspNet.Diagnostics.Views
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
using Views

#line default
#line hidden
    ;
    using System.Threading.Tasks;

    public class CompilationErrorPage : Microsoft.AspNet.Diagnostics.Views.BaseView
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
  
    var errorDetail = Model.ErrorDetails;

    Response.StatusCode = 500;
    Response.ContentType = "text/html";
    Response.ContentLength = null; // Clear any prior Content-Length

#line default
#line hidden

            WriteLiteral("\r\n<!DOCTYPE html>\r\n<html>\r\n    <head>\r\n        <meta charset=\"utf-8\" />\r\n        " +
"<title>");
#line 21 "CompilationErrorPage.cshtml"
          Write(Resources.ErrorPageHtml_Title);

#line default
#line hidden
            WriteLiteral("</title>\r\n        <style>\r\n            body {\r\n    font-family: 'Segoe UI', Tahoma, Arial, Helvetica, sans-serif;\r\n    font-size: .813em;\r\n    line-height: 1.4em;\r\n    color: #222;\r\n}\r\n\r\nh1, h2, h3, h4, h5 {\r\n    /*font-family: 'Segoe UI',Tahoma,Arial,Helvetica,sans-serif;*/\r\n    font-weight: 100;\r\n}\r\n\r\nh1 {\r\n    color: #44525e;\r\n    margin: 15px 0 15px 0;\r\n}\r\n\r\nh2 {\r\n    margin: 10px 5px 0 0;\r\n}\r\n\r\nh3 {\r\n    color: #363636;\r\n    margin: 5px 5px 0 0;\r\n}\r\n\r\ncode {\r\n    font-family: Consolas, \"Courier New\", courier, monospace;\r\n}\r\n\r\nbody .titleerror {\r\n    padding: 3px;\r\n    display: block;\r\n    font-size: 1.5em;\r\n    font-weight: 100;\r\n}\r\n\r\nbody .location {\r\n    margin: 3px 0 10px 30px;\r\n}\r\n\r\n#header {\r\n    font-size: 18px;\r\n    padding: 15px 0;\r\n    border-top: 1px #ddd solid;\r\n    border-bottom: 1px #ddd solid;\r\n    margin-bottom: 0;\r\n}\r\n\r\n    #header li {\r\n        display: inline;\r\n        margin: 5px;\r\n        padding: 5px;\r\n        color: #a0a0a0;\r\n        cursor: pointer;\r\n    }\r\n\r\n        #header li:hover {\r\n            background: #a9e4f9;\r\n            color: #fff;\r\n        }\r\n\r\n        #header .selected {\r\n            background: #44c5f2;\r\n            color: #fff;\r\n        }\r\n\r\n#stackpage ul {\r\n    list-style: none;\r\n    padding-left: 0;\r\n    margin: 0;\r\n    /*border-bottom: 1px #ddd solid;*/\r\n}\r\n\r\n#stackpage .stackerror {\r\n    padding: 5px;\r\n    border-bottom: 1px #ddd solid;\r\n}\r\n\r\n    #stackpage .stackerror:hover {\r\n        background-color: #f0f0f0;\r\n    }\r\n\r\n#stackpage .frame:hover {\r\n    background-color: #f0f0f0;\r\n    text-decoration: none;\r\n}\r\n\r\n#stackpage .frame {\r\n    padding: 2px;\r\n    margin: 0 0 0 30px;\r\n    border-bottom: 1px #ddd solid;\r\n    cursor: pointer;\r\n}\r\n\r\n    #stackpage .frame h3 {\r\n        padding: 5px;\r\n        margin: 0;\r\n    }\r\n\r\n#stackpage .source {\r\n    padding: 0;\r\n}\r\n\r\n    #stackpage .source ol li {\r\n        font-family: Consolas, \"Courier New\", courier, monospace;\r\n        white-space: pre;\r\n    }\r\n\r\n#stackpage .frame:hover .source .highlight li span {\r\n    color: #fff;\r\n    background: #b20000;\r\n}\r\n\r\n#stackpage .source ol.collapsable li {\r\n    color: #888;\r\n}\r\n\r\n    #stackpage .source ol.collapsable li span {\r\n        color: #606060;\r\n    }\r\n\r\n.page table {\r\n    border-collapse: separate;\r\n    border-spacing: 0;\r\n    margin: 0 0 20px;\r\n}\r\n\r\n.page th {\r\n    vertical-align: bottom;\r\n    padding: 10px 5px 5px 5px;\r\n    font-weight: 400;\r\n    color: #a0a0a0;\r\n    text-align: left;\r\n}\r\n\r\n.page td {\r\n    padding: 3px 10px;\r\n}\r\n\r\n.page th, .page td {\r\n    border-right: 1px #ddd solid;\r\n    border-bottom: 1px #ddd solid;\r\n    border-left: 1px transparent solid;\r\n    border-top: 1px transparent solid;\r\n    box-sizing: border-box;\r\n}\r\n\r\n    .page th:last-child, .page td:last-child {\r\n        border-right: 1px transparent solid;\r\n    }\r\n\r\n    .page .length {\r\n        text-align: right;\r\n    }\r\n\r\na {\r\n    color: #1ba1e2;\r\n    text-decoration: none;\r\n}\r\n\r\n    a:hover {\r\n        color: #13709e;\r\n        text-decoration: underline;\r\n    }\r\n\r\n        </s" +
"tyle>\r\n    </head>\r\n    <body>\r\n        <h1>");
#line 27 "CompilationErrorPage.cshtml"
       Write(Resources.ErrorPageHtml_CompilationException);

#line default
#line hidden
            WriteLiteral("</h1>\r\n");
#line 28 "CompilationErrorPage.cshtml"
        

#line default
#line hidden

#line 28 "CompilationErrorPage.cshtml"
         if (Model.Options.ShowExceptionDetails)
        {

#line default
#line hidden

            WriteLiteral("            <div class=\"titleerror\">");
#line 30 "CompilationErrorPage.cshtml"
                               Write(errorDetail.Error.GetType().Name);

#line default
#line hidden
            WriteLiteral(": ");
#line 30 "CompilationErrorPage.cshtml"
                                                                          Output.Write(WebUtility.HtmlEncode(errorDetail.Error.Message).Replace("\r\n", "<br/>").Replace("\r", "<br/>").Replace("\n", "<br/>")); 

#line default
#line hidden

            WriteLiteral("</div>\r\n");
#line 31 "CompilationErrorPage.cshtml"
        }
        else
        {

#line default
#line hidden

            WriteLiteral("            <h2>");
#line 34 "CompilationErrorPage.cshtml"
           Write(Resources.ErrorPageHtml_EnableShowExceptions);

#line default
#line hidden
            WriteLiteral("</h2>\r\n");
#line 35 "CompilationErrorPage.cshtml"
        }

#line default
#line hidden

            WriteLiteral("        ");
#line 36 "CompilationErrorPage.cshtml"
         if (Model.Options.ShowExceptionDetails)
        {

#line default
#line hidden

            WriteLiteral("            <div id=\"stackpage\" class=\"page\">\r\n");
#line 39 "CompilationErrorPage.cshtml"
                

#line default
#line hidden

#line 39 "CompilationErrorPage.cshtml"
                   int tabIndex = 6; 

#line default
#line hidden

            WriteLiteral("\r\n                <br />\r\n                <ul>\r\n");
#line 42 "CompilationErrorPage.cshtml"
                

#line default
#line hidden

#line 42 "CompilationErrorPage.cshtml"
                 foreach (var frame in errorDetail.StackFrames)
                {

#line default
#line hidden

            WriteLiteral("                    <li class=\"frame\"");
            WriteAttribute("tabindex", Tuple.Create(" tabindex=\"", 1370), Tuple.Create("\"", 1390), 
            Tuple.Create(Tuple.Create("", 1381), Tuple.Create<System.Object, System.Int32>(tabIndex, 1381), false));
            WriteLiteral(">\r\n");
#line 45 "CompilationErrorPage.cshtml"
                        

#line default
#line hidden

#line 45 "CompilationErrorPage.cshtml"
                           tabIndex++; 

#line default
#line hidden

            WriteLiteral("\r\n");
#line 46 "CompilationErrorPage.cshtml"
                        

#line default
#line hidden

#line 46 "CompilationErrorPage.cshtml"
                         if (!string.IsNullOrEmpty(frame.ErrorDetails))
                        {

#line default
#line hidden

            WriteLiteral("                            <h3>");
#line 48 "CompilationErrorPage.cshtml"
                           Write(frame.ErrorDetails);

#line default
#line hidden
            WriteLiteral("</h3>\r\n");
#line 49 "CompilationErrorPage.cshtml"
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(frame.File))
                            {

#line default
#line hidden

            WriteLiteral("                                <h3>");
#line 54 "CompilationErrorPage.cshtml"
                               Write(frame.Function);

#line default
#line hidden
            WriteLiteral("</h3>\r\n");
#line 55 "CompilationErrorPage.cshtml"
                            }
                            else
                            {

#line default
#line hidden

            WriteLiteral("                                <h3>");
#line 58 "CompilationErrorPage.cshtml"
                               Write(frame.Function);

#line default
#line hidden
            WriteLiteral(" in <code");
            WriteAttribute("title", Tuple.Create(" title=\"", 1990), Tuple.Create("\"", 2009), 
            Tuple.Create(Tuple.Create("", 1998), Tuple.Create<System.Object, System.Int32>(frame.File, 1998), false));
            WriteLiteral(">");
#line 58 "CompilationErrorPage.cshtml"
                                                                            Write(System.IO.Path.GetFileName(frame.File));

#line default
#line hidden
            WriteLiteral("</code></h3>\r\n");
#line 59 "CompilationErrorPage.cshtml"
                            }
                        }

#line default
#line hidden

            WriteLiteral("                                    \r\n");
#line 62 "CompilationErrorPage.cshtml"
                        

#line default
#line hidden

#line 62 "CompilationErrorPage.cshtml"
                         if (frame.Line != 0 && frame.ContextCode.Any())
                        {

#line default
#line hidden

            WriteLiteral("                            <div class=\"source\">\r\n");
#line 65 "CompilationErrorPage.cshtml"
                                

#line default
#line hidden

#line 65 "CompilationErrorPage.cshtml"
                                 if (frame.PreContextCode != null)
                                {

#line default
#line hidden

            WriteLiteral("                                    <ol");
            WriteAttribute("start", Tuple.Create(" start=\"", 2453), Tuple.Create("\"", 2482), 
            Tuple.Create(Tuple.Create("", 2461), Tuple.Create<System.Object, System.Int32>(frame.PreContextLine, 2461), false));
            WriteLiteral(" class=\"collapsible\">\r\n");
#line 68 "CompilationErrorPage.cshtml"
                                        

#line default
#line hidden

#line 68 "CompilationErrorPage.cshtml"
                                         foreach (var line in frame.PreContextCode)
                                        {

#line default
#line hidden

            WriteLiteral("                                            <li><span>");
#line 70 "CompilationErrorPage.cshtml"
                                                 Write(line);

#line default
#line hidden
            WriteLiteral("</span></li>\r\n");
#line 71 "CompilationErrorPage.cshtml"
                                        }

#line default
#line hidden

            WriteLiteral("                                    </ol>\r\n");
#line 73 "CompilationErrorPage.cshtml"
                                } 

#line default
#line hidden

            WriteLiteral("\r\n                                <ol");
            WriteAttribute("start", Tuple.Create(" start=\"", 2866), Tuple.Create("\"", 2885), 
            Tuple.Create(Tuple.Create("", 2874), Tuple.Create<System.Object, System.Int32>(frame.Line, 2874), false));
            WriteLiteral(" class=\"highlight\">\r\n");
#line 76 "CompilationErrorPage.cshtml"
                                    

#line default
#line hidden

#line 76 "CompilationErrorPage.cshtml"
                                     foreach (var line in frame.ContextCode)
                                    {

#line default
#line hidden

            WriteLiteral("                                        <li><span>");
#line 78 "CompilationErrorPage.cshtml"
                                             Write(line);

#line default
#line hidden
            WriteLiteral("</span></li>\r\n");
#line 79 "CompilationErrorPage.cshtml"
                                    }

#line default
#line hidden

            WriteLiteral("                                </ol>                                    \r\n\r\n");
#line 82 "CompilationErrorPage.cshtml"
                                

#line default
#line hidden

#line 82 "CompilationErrorPage.cshtml"
                                 if (frame.PostContextCode != null)
                                {

#line default
#line hidden

            WriteLiteral("                                    <ol");
            WriteAttribute("start", Tuple.Create(" start=\'", 3352), Tuple.Create("\'", 3377), 
            Tuple.Create(Tuple.Create("", 3360), Tuple.Create<System.Object, System.Int32>(frame.Line + 1, 3360), false));
            WriteLiteral(" class=\"collapsible\">\r\n");
#line 85 "CompilationErrorPage.cshtml"
                                        

#line default
#line hidden

#line 85 "CompilationErrorPage.cshtml"
                                         foreach (var line in frame.PostContextCode)
                                        {

#line default
#line hidden

            WriteLiteral("                                            <li><span>");
#line 87 "CompilationErrorPage.cshtml"
                                                 Write(line);

#line default
#line hidden
            WriteLiteral("</span></li>\r\n");
#line 88 "CompilationErrorPage.cshtml"
                                        }

#line default
#line hidden

            WriteLiteral("                                    </ol>\r\n");
#line 90 "CompilationErrorPage.cshtml"
                                } 

#line default
#line hidden

            WriteLiteral("                            </div>\r\n");
#line 92 "CompilationErrorPage.cshtml"
                        } 

#line default
#line hidden

            WriteLiteral("                    </li>\r\n");
#line 94 "CompilationErrorPage.cshtml"
                }

#line default
#line hidden

            WriteLiteral("                </ul>\r\n            </div>\r\n");
#line 97 "CompilationErrorPage.cshtml"
        }

#line default
#line hidden

            WriteLiteral("        <script>\r\n            //<!--\r\n            (function (window, undefined) {\r\n    \"use strict\";\r\n\r\n    function $(selector, element) {\r\n        return new NodeCollection(selector, element);\r\n    }\r\n\r\n    function NodeCollection(selector, element) {\r\n        this.items = [];\r\n        element = element || window.document;\r\n\r\n        var nodeList;\r\n\r\n        if (typeof (selector) === \"string\") {\r\n            nodeList = element.querySelectorAll(selector);\r\n            for (var i = 0, l = nodeList.length; i < l; i++) {\r\n                this.items.push(nodeList.item(i));\r\n            }\r\n        } else if (selector.tagName) {\r\n            this.items.push(selector);\r\n        } else if (selector.splice) {\r\n            this.items = this.items.concat(selector);\r\n        }\r\n    }\r\n\r\n    NodeCollection.prototype = {\r\n        each: function (callback) {\r\n            for (var i = 0, l = this.items.length; i < l; i++) {\r\n                callback(this.items[i], i);\r\n            }\r\n            return this;\r\n        },\r\n\r\n        children: function (selector) {\r\n            var children = [];\r\n\r\n            this.each(function (el) {\r\n                children = children.concat($(selector, el).items);\r\n            });\r\n\r\n            return $(children);\r\n        },\r\n\r\n        hide: function () {\r\n            this.each(function (el) {\r\n                el.style.display = \"none\";\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        toggle: function () {\r\n            this.each(function (el) {\r\n                el.style.display = el.style.display === \"none\" ? \"\" : \"none\";\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        show: function () {\r\n            this.each(function (el) {\r\n                el.style.display = \"\";\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        addClass: function (className) {\r\n            this.each(function (el) {\r\n                var existingClassName = el.className,\r\n                    classNames;\r\n                if (!existingClassName) {\r\n                    el.className = className;\r\n                } else {\r\n                    classNames = existingClassName.split(\" \");\r\n                    if (classNames.indexOf(className) < 0) {\r\n                        el.className = existingClassName + \" \" + className;\r\n                    }\r\n                }\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        removeClass: function (className) {\r\n            this.each(function (el) {\r\n                var existingClassName = el.className,\r\n                    classNames, index;\r\n                if (existingClassName === className) {\r\n                    el.className = \"\";\r\n                } else if (existingClassName) {\r\n                    classNames = existingClassName.split(\" \");\r\n                    index = classNames.indexOf(className);\r\n                    if (index > 0) {\r\n                        classNames.splice(index, 1);\r\n                        el.className = classNames.join(\" \");\r\n                    }\r\n                }\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        toggleClass: function (className) {\r\n            this.each(function (el) {\r\n                var classNames = el.className.split(\" \");\r\n                if (classNames.indexOf(className) >= 0) {\r\n                    $(el).removeClass(className);\r\n                } else {\r\n                    $(el).addClass(className);\r\n                }\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        attr: function (name) {\r\n            if (this.items.length === 0) {\r\n                return null;\r\n            }\r\n\r\n            return this.items[0].getAttribute(name);\r\n        },\r\n\r\n        on: function (eventName, handler) {\r\n            this.each(function (el, idx) {\r\n                var callback = function (e) {\r\n                    e = e || window.event;\r\n                    if (!e.which && e.keyCode) {\r\n                        e.which = e.keyCode; // Normalize IE8 key events\r\n                    }\r\n                    handler.apply(el, [e]);\r\n                };\r\n\r\n                if (el.addEventListener) { // DOM Events\r\n                    el.addEventListener(eventName, callback, false);\r\n                } else if (el.attachEvent) { // IE8 events\r\n                    el.attachEvent(\"on\" + eventName, callback);\r\n                } else {\r\n                    el[\"on\" + type] = callback;\r\n                }\r\n            });\r\n\r\n            return this;\r\n        },\r\n\r\n        click: function (handler) {\r\n            return this.on(\"click\", handler);\r\n        },\r\n\r\n        keypress: function (handler) {\r\n            return this.on(\"keypress\", handler);\r\n        }\r\n    };\r\n\r\n    function frame(el) {\r\n        $(el).children(\".source .collapsible\").toggle();\r\n    }\r\n\r\n    function tab(el) {\r\n        var unselected = $(\"#header .selected\").removeClass(\"selected\").attr(\"id\");\r\n        var selected = $(el).addClass(\"selected\").attr(\"id\");\r\n\r\n        $(\"#\" + unselected + \"page\").hide();\r\n        $(\"#\" + selected + \"page\").show();\r\n    }\r\n\r\n    $(\".collapsible\").hide();\r\n    $(\".page\").hide();\r\n    $(\"#stackpage\").show();\r\n\r\n    $(\".frame\")\r\n        .click(function () {\r\n            frame(this);\r\n        })\r\n        .keypress(function (e) {\r\n            if (e.which === 13) {\r\n                frame(this);\r\n            }\r\n        });\r\n\r\n    $(\"#header li\")\r\n        .click(function () {\r\n            tab(this);\r\n        })\r\n        .keypress(function (e) {\r\n            if (e.which === 13) {\r\n                tab(this);\r\n            }\r\n        });\r\n})(window);\r\n " +
"           //-->\r\n        </script>\r\n    </body>\r\n</html>\r\n");
        }
        #pragma warning restore 1998
    }
}
