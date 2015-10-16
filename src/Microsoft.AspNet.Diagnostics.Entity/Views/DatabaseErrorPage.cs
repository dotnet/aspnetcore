namespace Microsoft.AspNet.Diagnostics.Entity.Views
{
#line 1 "DatabaseErrorPage.cshtml"
using System

#line default
#line hidden
    ;
#line 2 "DatabaseErrorPage.cshtml"
using System.Linq

#line default
#line hidden
    ;
#line 3 "DatabaseErrorPage.cshtml"
using Microsoft.AspNet.Diagnostics.Entity

#line default
#line hidden
    ;
#line 4 "DatabaseErrorPage.cshtml"
using Microsoft.AspNet.Diagnostics.Entity.Views

#line default
#line hidden
    ;
    using System.Threading.Tasks;

    public class DatabaseErrorPage : Microsoft.AspNet.Diagnostics.Views.BaseView
    {
#line 11 "DatabaseErrorPage.cshtml"

    public DatabaseErrorPageModel Model { get; set; }

    public string UrlEncode(string content)
    {
        return UrlEncoder.UrlEncode(content);
    }

    public string JavaScriptEncode(string content)
    {
        return JavaScriptStringEncoder.JavaScriptStringEncode(content);
    }

#line default
#line hidden
        #line hidden
        public DatabaseErrorPage()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 5 "DatabaseErrorPage.cshtml"
  
    Response.StatusCode = 500;
    Response.ContentType = "text/html";
    Response.ContentLength = null; // Clear any prior Content-Length

#line default
#line hidden

            WriteLiteral("<!DOCTYPE html>\r\n\r\n<html lang=\"en\" xmlns=\"http://www.w3.org/1999/xhtml\">\r\n<head>\r" +
"\n    <meta charset=\"utf-8\" />\r\n    <title>Internal Server Error</title>\r\n    <st" +
"yle>\r\n            body {\r\n    font-family: 'Segoe UI', Tahoma, Arial, Helvetica, sans-serif;\r\n    font-size: .813em;\r\n    line-height: 1.4em;\r\n    color: #222;\r\n}\r\n\r\nh1, h2, h3, h4, h5 {\r\n    font-weight: 100;\r\n}\r\n\r\nh1 {\r\n    color: #44525e;\r\n    margin: 15px 0 15px 0;\r\n}\r\n\r\nh2 {\r\n    margin: 10px 5px 0 0;\r\n}\r\n\r\nh3 {\r\n    color: #363636;\r\n    margin: 5px 5px 0 0;\r\n}\r\n\r\ncode {\r\n    font-family: Consolas, \"Courier New\", courier, monospace;\r\n}\r\n\r\na {\r\n    color: #1ba1e2;\r\n    text-decoration: none;\r\n}\r\n\r\n    a:hover {\r\n        color: #13709e;\r\n        text-decoration: underline;\r\n    }\r\n\r\nhr {\r\n    border: 1px #ddd solid;\r\n}\r\n\r\nbody .titleerror {\r\n    padding: 3px;\r\n}\r\n\r\n#applyMigrations {\r\n    font-size: 14px;\r\n    background: #44c5f2;\r\n    color: #ffffff;\r\n    display: inline-block;\r\n    padding: 6px 12px;\r\n    margin-bottom: 0;\r\n    font-weight: normal;\r\n    text-align: center;\r\n    white-space: nowrap;\r\n    vertical-align: middle;\r\n    cursor: pointer;\r\n    border: 1px solid transparent;\r\n}\r\n\r\n    #applyMigrations:disabled {\r\n        background-color: #a9e4f9;\r\n        border-color: #44c5f2;\r\n    }\r\n\r\n.error {\r\n    color: red;\r\n}\r\n\r\n.expanded {\r\n    display: block;\r\n}\r\n\r\n.collapsed {\r\n    display: none;\r\n}\r\n\r\n    </style>\r\n</head>\r\n<body>\r\n" +
"    <h1>");
#line 35 "DatabaseErrorPage.cshtml"
   Write(Strings.DatabaseErrorPage_Title);

#line default
#line hidden
            WriteLiteral("</h1>\r\n");
#line 36 "DatabaseErrorPage.cshtml"
    

#line default
#line hidden

#line 36 "DatabaseErrorPage.cshtml"
     if (Model.Options.ShowExceptionDetails)
    {

#line default
#line hidden

            WriteLiteral("        <p>\r\n");
#line 39 "DatabaseErrorPage.cshtml"
            

#line default
#line hidden

#line 39 "DatabaseErrorPage.cshtml"
             for (Exception ex = Model.Exception; ex != null; ex = ex.InnerException)
                {

#line default
#line hidden

            WriteLiteral("                <span>");
#line 41 "DatabaseErrorPage.cshtml"
                 Write(ex.GetType().Name);

#line default
#line hidden
            WriteLiteral(": ");
#line 41 "DatabaseErrorPage.cshtml"
                                     Write(ex.Message);

#line default
#line hidden
            WriteLiteral("</span>\r\n                <br />\r\n");
#line 43 "DatabaseErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("        </p>\r\n        <hr />\r\n");
#line 46 "DatabaseErrorPage.cshtml"
    }

#line default
#line hidden

            WriteLiteral("\r\n");
#line 48 "DatabaseErrorPage.cshtml"
    

#line default
#line hidden

#line 48 "DatabaseErrorPage.cshtml"
     if (!Model.DatabaseExists && !Model.PendingMigrations.Any())
    {

#line default
#line hidden

            WriteLiteral("        <h2>");
#line 50 "DatabaseErrorPage.cshtml"
       Write(Strings.FormatDatabaseErrorPage_NoDbOrMigrationsTitle(Model.ContextType.Name));

#line default
#line hidden
            WriteLiteral("</h2>\r\n        <p>");
#line 51 "DatabaseErrorPage.cshtml"
      Write(Strings.DatabaseErrorPage_NoDbOrMigrationsInfo);

#line default
#line hidden
            WriteLiteral("</p>\r\n        <code> ");
#line 52 "DatabaseErrorPage.cshtml"
          Write(Strings.DatabaseErrorPage_AddMigrationCommand);

#line default
#line hidden
            WriteLiteral(" </code>\r\n        <br />\r\n        <code> ");
#line 54 "DatabaseErrorPage.cshtml"
          Write(Strings.DatabaseErrorPage_ApplyMigrationsCommand);

#line default
#line hidden
            WriteLiteral(" </code>\r\n        <hr />\r\n");
#line 56 "DatabaseErrorPage.cshtml"
    }
    else if (Model.PendingMigrations.Any())
    {

#line default
#line hidden

            WriteLiteral("        <div>\r\n            <h2>");
#line 60 "DatabaseErrorPage.cshtml"
           Write(Strings.FormatDatabaseErrorPage_PendingMigrationsTitle(Model.ContextType.Name));

#line default
#line hidden
            WriteLiteral("</h2>\r\n            <p>");
#line 61 "DatabaseErrorPage.cshtml"
          Write(Strings.FormatDatabaseErrorPage_PendingMigrationsInfo(Model.ContextType.Name));

#line default
#line hidden
            WriteLiteral("</p>\r\n\r\n");
#line 63 "DatabaseErrorPage.cshtml"
            

#line default
#line hidden

#line 63 "DatabaseErrorPage.cshtml"
             if (Model.Options.ListMigrations)
            {

#line default
#line hidden

            WriteLiteral("                <ul>\r\n");
#line 66 "DatabaseErrorPage.cshtml"
                    

#line default
#line hidden

#line 66 "DatabaseErrorPage.cshtml"
                     foreach (var migration in Model.PendingMigrations)
                    {

#line default
#line hidden

            WriteLiteral("                        <li>");
#line 68 "DatabaseErrorPage.cshtml"
                       Write(migration);

#line default
#line hidden
            WriteLiteral("</li>\r\n");
#line 69 "DatabaseErrorPage.cshtml"
                    }

#line default
#line hidden

            WriteLiteral("                </ul>\r\n");
#line 71 "DatabaseErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("\r\n");
#line 73 "DatabaseErrorPage.cshtml"
            

#line default
#line hidden

#line 73 "DatabaseErrorPage.cshtml"
             if (Model.Options.EnableMigrationCommands)
            {

#line default
#line hidden

            WriteLiteral("                <p>\r\n                    <button id=\"applyMigrations\" onclick=\"Ap" +
"plyMigrations()\">");
#line 76 "DatabaseErrorPage.cshtml"
                                                                        Write(Strings.DatabaseErrorPage_ApplyMigrationsButton);

#line default
#line hidden
            WriteLiteral(@"</button>
                    <span id=""applyMigrationsError"" class=""error""></span>
                    <span id=""applyMigrationsSuccess""></span>
                </p>
                <script>
                    function ApplyMigrations() {
                        applyMigrations.disabled = true;
                        applyMigrationsError.innerHTML = """";
                        applyMigrations.innerHTML = """);
#line 84 "DatabaseErrorPage.cshtml"
                                                Write(JavaScriptEncode(Strings.DatabaseErrorPage_ApplyMigrationsButtonRunning));

#line default
#line hidden
            WriteLiteral("\";\r\n\r\n                        var req = new XMLHttpRequest();\r\n\r\n                " +
"        req.onload = function (e) {\r\n                            if (req.status " +
"=== 204) {\r\n                                applyMigrations.innerHTML = \"");
#line 90 "DatabaseErrorPage.cshtml"
                                                        Write(JavaScriptEncode(Strings.DatabaseErrorPage_ApplyMigrationsButtonDone));

#line default
#line hidden
            WriteLiteral("\";\r\n                                applyMigrationsSuccess.innerHTML = \"");
#line 91 "DatabaseErrorPage.cshtml"
                                                               Write(JavaScriptEncode(Strings.DatabaseErrorPage_MigrationsAppliedRefresh));

#line default
#line hidden
            WriteLiteral(@""";
                            } else {
                                ErrorApplyingMigrations();
                            }
                        };

                        req.onerror = function (e) {
                            ErrorApplyingMigrations();
                        };

                        var formBody = ""context=");
#line 101 "DatabaseErrorPage.cshtml"
                                           Write(JavaScriptEncode(UrlEncode(Model.ContextType.AssemblyQualifiedName)));

#line default
#line hidden
            WriteLiteral("\";\r\n                        req.open(\"POST\", \"");
#line 102 "DatabaseErrorPage.cshtml"
                                     Write(JavaScriptEncode(Model.Options.MigrationsEndPointPath.Value));

#line default
#line hidden
            WriteLiteral(@""", true);
                        req.setRequestHeader(""Content-type"", ""application/x-www-form-urlencoded"");
                        req.setRequestHeader(""Content-length"", formBody.length);
                        req.setRequestHeader(""Connection"", ""close"");
                        req.send(formBody);
                    }

                    function ErrorApplyingMigrations() {
                        applyMigrations.innerHTML = """);
#line 110 "DatabaseErrorPage.cshtml"
                                                Write(JavaScriptEncode(Strings.DatabaseErrorPage_ApplyMigrationsButton));

#line default
#line hidden
            WriteLiteral("\";\r\n                        applyMigrationsError.innerHTML = \"");
#line 111 "DatabaseErrorPage.cshtml"
                                                     Write(JavaScriptEncode(Strings.DatabaseErrorPage_ApplyMigrationsFailed));

#line default
#line hidden
            WriteLiteral("\";\r\n                        applyMigrations.disabled = false;\r\n                  " +
"  }\r\n                </script>\r\n");
#line 115 "DatabaseErrorPage.cshtml"
            }

#line default
#line hidden

            WriteLiteral("\r\n            <p>");
#line 117 "DatabaseErrorPage.cshtml"
          Write(Strings.DatabaseErrorPage_HowToApplyFromCmd);

#line default
#line hidden
            WriteLiteral("</p>\r\n            <code>");
#line 118 "DatabaseErrorPage.cshtml"
             Write(Strings.DatabaseErrorPage_ApplyMigrationsCommand);

#line default
#line hidden
            WriteLiteral("</code>\r\n            <hr />\r\n        </div>\r\n");
#line 121 "DatabaseErrorPage.cshtml"
    }
    else if (Model.PendingModelChanges)
    {

#line default
#line hidden

            WriteLiteral("        <div>\r\n            <h2>");
#line 125 "DatabaseErrorPage.cshtml"
           Write(Strings.FormatDatabaseErrorPage_PendingChangesTitle(Model.ContextType.Name));

#line default
#line hidden
            WriteLiteral("</h2>\r\n            <p>");
#line 126 "DatabaseErrorPage.cshtml"
          Write(Strings.DatabaseErrorPage_PendingChangesInfo);

#line default
#line hidden
            WriteLiteral("</p>\r\n            <code>");
#line 127 "DatabaseErrorPage.cshtml"
             Write(Strings.DatabaseErrorPage_AddMigrationCommand);

#line default
#line hidden
            WriteLiteral("</code>\r\n            <br />\r\n            <code>");
#line 129 "DatabaseErrorPage.cshtml"
             Write(Strings.DatabaseErrorPage_ApplyMigrationsCommand);

#line default
#line hidden
            WriteLiteral("</code>\r\n            <hr />\r\n        </div>\r\n");
#line 132 "DatabaseErrorPage.cshtml"
    }

#line default
#line hidden

            WriteLiteral("</body>\r\n</html>");
        }
        #pragma warning restore 1998
    }
}
