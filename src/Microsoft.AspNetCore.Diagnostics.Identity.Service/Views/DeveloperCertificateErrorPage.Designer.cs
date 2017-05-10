namespace Microsoft.AspNetCore.Diagnostics.Identity.Service
{
    #line hidden
#line 1 "DeveloperCertificateErrorPage.cshtml"
using System;

#line default
#line hidden
    using System.Threading.Tasks;
    internal class DeveloperCertificateErrorPage : Microsoft.Extensions.RazorViews.BaseView
    {
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#line 2 "DeveloperCertificateErrorPage.cshtml"
  
    Response.StatusCode = 500;
    Response.ContentType = "text/html; charset=utf-8";
    Response.ContentLength = null; // Clear any prior Content-Length

#line default
#line hidden
            WriteLiteral(@"<!DOCTYPE html>

<html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <meta charset=""utf-8"" />
    <title>Internal Server Error</title>
    <style>
        body {
    font-family: 'Segoe UI', Tahoma, Arial, Helvetica, sans-serif;
    font-size: .813em;
    line-height: 1.4em;
    color: #222;
}

h1, h2, h3, h4, h5 {
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

a {
    color: #1ba1e2;
    text-decoration: none;
}

    a:hover {
        color: #13709e;
        text-decoration: underline;
    }

hr {
    border: 1px #ddd solid;
}

body .titleerror {
    padding: 3px;
}

#createCertificate {
    font-size: 14px;
    background: #44c5f2;
    color: #ffffff;
    display: inline-block;
    padding: 6px 12px;
    margin-bottom: 0;
    font-weight: normal;");
            WriteLiteral(@"
    text-align: center;
    white-space: nowrap;
    vertical-align: middle;
    cursor: pointer;
    border: 1px solid transparent;
}

    #createCertificate:disabled {
        background-color: #a9e4f9;
        border-color: #44c5f2;
    }

.error {
    color: red;
}

.expanded {
    display: block;
}

.collapsed {
    display: none;
}

    </style>
</head>
<body>
    <h1>");
#line 110 "DeveloperCertificateErrorPage.cshtml"
   Write(Strings.CertificateErrorPage_Title);

#line default
#line hidden
            WriteLiteral("</h1>\r\n    <p>\r\n    </p>\r\n    <hr />\r\n\r\n");
#line 115 "DeveloperCertificateErrorPage.cshtml"
     if (!Model.CertificateExists || Model.CertificateIsInvalid)
    {

#line default
#line hidden
            WriteLiteral("        <h2>");
#line 117 "DeveloperCertificateErrorPage.cshtml"
       Write(Strings.MissingOrInvalidCertificate);

#line default
#line hidden
            WriteLiteral("</h2>\r\n        <p>");
#line 118 "DeveloperCertificateErrorPage.cshtml"
      Write(Strings.ManualCertificateGenerationInfo);

#line default
#line hidden
            WriteLiteral("<a");
            BeginWriteAttribute("href", " href=\"", 2170, "\"", 2221, 1);
#line 118 "DeveloperCertificateErrorPage.cshtml"
WriteAttributeValue("", 2177, Strings.ManualCertificateGenerationInfoLink, 2177, 44, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">");
#line 118 "DeveloperCertificateErrorPage.cshtml"
                                                                                                     Write(Strings.ManualCertificateGenerationInfoLink);

#line default
#line hidden
            WriteLiteral("</a></p>\r\n        <br />\r\n        <hr />\r\n        <div>\r\n            <p>\r\n                <button id=\"createCertificate\" onclick=\"CreateCertificate()\">");
#line 123 "DeveloperCertificateErrorPage.cshtml"
                                                                        Write(Strings.CreateCertificate);

#line default
#line hidden
            WriteLiteral(@"</button>
                <span id=""createCertificateError"" class=""error""></span>
                <span id=""createCertificateSuccess""></span>
            </p>
            <script>
                function CreateCertificate() {
                    createCertificate.disabled = true;
                    createCertificateError.innerHTML = """";
                    createCertificate.innerHTML = """);
#line 131 "DeveloperCertificateErrorPage.cshtml"
                                              Write(JavaScriptEncode(Strings.CreateCertificateRunning));

#line default
#line hidden
            WriteLiteral("\";\r\n\r\n                    var req = new XMLHttpRequest();\r\n\r\n                    req.onload = function (e) {\r\n                        if (req.status === 204) {\r\n                            createCertificate.innerHTML = \"");
#line 137 "DeveloperCertificateErrorPage.cshtml"
                                                      Write(JavaScriptEncode(Strings.CreateCertificateDone));

#line default
#line hidden
            WriteLiteral("\";\r\n                            createCertificateSuccess.innerHTML = \"");
#line 138 "DeveloperCertificateErrorPage.cshtml"
                                                             Write(JavaScriptEncode(Strings.CreateCertificateRefresh));

#line default
#line hidden
            WriteLiteral(@""";
                        } else {
                            ErrorCreatingCertificate();
                        }
                    };

                    req.onerror = function (e) {
                        ErrorCreatingCertificate();
                    };

                    var formBody = """";
                    req.open(""POST"", """);
#line 149 "DeveloperCertificateErrorPage.cshtml"
                                 Write(JavaScriptEncode(Model.Options.ListeningEndpoint.Value));

#line default
#line hidden
            WriteLiteral(@""", true);
                    req.setRequestHeader(""Content-type"", ""application/x-www-form-urlencoded"");
                    req.setRequestHeader(""Content-length"", formBody.length);
                    req.setRequestHeader(""Connection"", ""close"");
                    req.send(formBody);
                }

                function ErrorCreatingCertificate() {
                    createCertificate.innerHTML = """);
#line 157 "DeveloperCertificateErrorPage.cshtml"
                                              Write(JavaScriptEncode(Strings.CreateCertificate));

#line default
#line hidden
            WriteLiteral("\";\r\n                    createCertificateError.innerHTML = \"");
#line 158 "DeveloperCertificateErrorPage.cshtml"
                                                   Write(JavaScriptEncode(Strings.CreateCertificateFailed));

#line default
#line hidden
            WriteLiteral("\";\r\n                    createCertificate.disabled = false;\r\n                }\r\n            </script>\r\n        </div>\r\n");
#line 163 "DeveloperCertificateErrorPage.cshtml"
    }

#line default
#line hidden
            WriteLiteral("</body>\r\n</html>");
        }
        #pragma warning restore 1998
#line 8 "DeveloperCertificateErrorPage.cshtml"
 
    public DeveloperCertificateViewModel Model { get; set; }

    public string UrlEncode(string content)
    {
        return UrlEncoder.Encode(content);
    }

    public string JavaScriptEncode(string content)
    {
        return JavaScriptEncoder.Encode(content);
    }

#line default
#line hidden
    }
}
