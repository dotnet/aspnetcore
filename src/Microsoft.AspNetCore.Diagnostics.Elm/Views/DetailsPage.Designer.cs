namespace Microsoft.AspNetCore.Diagnostics.Elm.RazorViews
{
#line 1 "DetailsPage.cshtml"
using System

#line default
#line hidden
    ;
#line 2 "DetailsPage.cshtml"
using System.Globalization

#line default
#line hidden
    ;
#line 3 "DetailsPage.cshtml"
using System.Linq

#line default
#line hidden
    ;
#line 4 "DetailsPage.cshtml"
using Microsoft.AspNetCore.Diagnostics.Elm

#line default
#line hidden
    ;
#line 5 "DetailsPage.cshtml"
using Microsoft.AspNetCore.Diagnostics.Elm.RazorViews

#line default
#line hidden
    ;
#line 6 "DetailsPage.cshtml"
using Microsoft.Extensions.RazorViews

#line default
#line hidden
    ;
#line 7 "DetailsPage.cshtml"
using Microsoft.Extensions.Logging

#line default
#line hidden
    ;
    using System.Threading.Tasks;

    internal class DetailsPage : Microsoft.Extensions.RazorViews.BaseView
    {
#line 10 "DetailsPage.cshtml"

    public DetailsPage(DetailsPageModel model)
    {
        Model = model;
    }

    public DetailsPageModel Model { get; set; }

    public HelperResult LogRow(LogInfo log)
    {
        return new HelperResult((writer) =>
        {
            if (log.Severity >= Model.Options.MinLevel &&
               (string.IsNullOrEmpty(Model.Options.NamePrefix) || log.Name.StartsWith(Model.Options.NamePrefix, StringComparison.Ordinal)))
            {
                WriteLiteralTo(writer, "        <tr>\r\n            <td>");
                WriteTo(writer, string.Format("{0:MM/dd/yy}", log.Time));
                WriteLiteralTo(writer, "</td>\r\n            <td>");
                WriteTo(writer, string.Format("{0:H:mm:ss}", log.Time));
                var severity = log.Severity.ToString().ToLowerInvariant();
                WriteLiteralTo(writer, $"</td>\r\n            <td class=\"{severity}\">");
                WriteTo(writer, log.Severity);

                WriteLiteralTo(writer, $"</td>\r\n            <td title=\"{log.Name}\">");
                WriteTo(writer, log.Name);

                WriteLiteralTo(writer, $"</td>\r\n            <td title=\"{log.Message}\""+
                    "class=\"logState\" width=\"100px\">");
                WriteTo(writer, log.Message);

                WriteLiteralTo(writer, $"</td>\r\n            <td title=\"{log.Exception}\">");
                WriteTo(writer, log.Exception);

                WriteLiteralTo(writer, "</td>\r\n        </tr>\r\n");
            }
        });
    }

    public HelperResult Traverse(ScopeNode node)
    {
        return new HelperResult((writer) =>
        {
            var messageIndex = 0;
            var childIndex = 0;
            while (messageIndex < node.Messages.Count && childIndex < node.Children.Count)
            {
                if (node.Messages[messageIndex].Time < node.Children[childIndex].StartTime)
                {
                    LogRow(node.Messages[messageIndex]);
                    messageIndex++;
                }
                else
                {
                    Traverse(node.Children[childIndex]);
                    childIndex++;
                }
            }
            if (messageIndex < node.Messages.Count)
            {
                for (var i = messageIndex; i < node.Messages.Count; i++)
                {
                    LogRow(node.Messages[i]);
                }
            }
            else
            {
                for (var i = childIndex; i < node.Children.Count; i++)
                {
                    Traverse(node.Children[i]);
                }
            }
        });
    }

#line default
#line hidden
        #line hidden
        public DetailsPage()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            WriteLiteral("\r\n");
#line 84 "DetailsPage.cshtml"
  
    Response.ContentType = "text/html; charset=utf-8";

#line default
#line hidden

            WriteLiteral(@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>ASP.NET Core Logs</title>
    <script src=""http://ajax.aspnetcdn.com/ajax/jquery/jquery-2.1.1.min.js""></script>
    <style>
        body {
    font-family: 'Segoe UI', Tahoma, Arial, Helvtica, sans-serif;
    line-height: 1.4em;
}

h1 {
    font-family: 'Segoe UI', Helvetica, sans-serif;
    font-size: 2.5em;
}

td {
    text-overflow: ellipsis;
    overflow: hidden;
}

tr:nth-child(2n) {
    background-color: #F6F6F6;
}

.critical {
    background-color: red;
    color: white;
}

.error {
    color: red;
}

.information {
    color: blue;
}

.debug {
    color: black;
}

.warning {
    color: orange;
} body {
    font-size: 0.9em;
    width: 90%;
    margin: 0px auto;
}

h1 {
    padding-bottom: 10px;
}

h2 {
    font-weight: normal;
}

table {
    border-spacing: 0px;
    width: 100%;
    border-collapse: collapse;
    border: 1px solid black;
    white-space: pre-wrap;
}
");
            WriteLiteral(@"
th {
    font-family: Arial;
}

td, th {
    padding: 8px;
}

#headerTable, #cookieTable {
    border: none;
    height: 100%;
}

#headerTd {
    white-space: normal;
}

#label {
    width: 20%;
    border-right: 1px solid black;
}

#logs{
    margin-top: 10px;
    margin-bottom: 20px;
}

#logs>tbody>tr>td {
    border-right: 1px dashed lightgray;
}

#logs>thead>tr>th {
    border: 1px solid black;
}
    </style>
</head>
<body>
    <h1>ASP.NET Core Logs</h1>
");
#line 192 "DetailsPage.cshtml"
    

#line default
#line hidden

#line 192 "DetailsPage.cshtml"
      
        var context = Model.Activity?.HttpInfo;
    

#line default
#line hidden

            WriteLiteral("    ");
#line 195 "DetailsPage.cshtml"
     if (context != null)
    {

#line default
#line hidden

            WriteLiteral("        <h2 id=\"requestHeader\">Request Details</h2>\r\n        <table id=\"requestDetails\">\r\n            <colgroup><col id=\"label\" /><col /></colgroup>\r\n\r\n            <tr>\r\n                <th>Path</th>\r\n                <td>");
#line 203 "DetailsPage.cshtml"
               Write(context.Path);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Host</th>\r\n                <td>");
#line 207 "DetailsPage.cshtml"
               Write(context.Host);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Content Type</th>\r\n                <td>");
#line 211 "DetailsPage.cshtml"
               Write(context.ContentType);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Method</th>\r\n                <td>");
#line 215 "DetailsPage.cshtml"
               Write(context.Method);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Protocol</th>\r\n                <td>");
#line 219 "DetailsPage.cshtml"
               Write(context.Protocol);

#line default
#line hidden
            WriteLiteral(@"</td>
            </tr>
            <tr>
                <th>Headers</th>
                <td id=""headerTd"">
                    <table id=""headerTable"">
                        <thead>
                            <tr>
                                <th>Variable</th>
                                <th>Value</th>
                            </tr>
                        </thead>
                        <tbody>
");
#line 232 "DetailsPage.cshtml"
                            

#line default
#line hidden

#line 232 "DetailsPage.cshtml"
                             foreach (var header in context.Headers)
                            {

#line default
#line hidden

            WriteLiteral("                            <tr>\r\n                                <td>");
#line 235 "DetailsPage.cshtml"
                               Write(header.Key);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                <td>");
#line 236 "DetailsPage.cshtml"
                               Write(string.Join(";", header.Value));

#line default
#line hidden
            WriteLiteral("</td>\r\n                            </tr>\r\n");
#line 238 "DetailsPage.cshtml"
                            }

#line default
#line hidden

            WriteLiteral("                        </tbody>\r\n                    </table>\r\n                </td>\r\n            </tr>\r\n            <tr>\r\n                <th>Status Code</th>\r\n                <td>");
#line 245 "DetailsPage.cshtml"
               Write(context.StatusCode);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>User</th>\r\n                <td>");
#line 249 "DetailsPage.cshtml"
               Write(context.User.Identity.Name);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Claims</th>\r\n                <td>\r\n");
#line 254 "DetailsPage.cshtml"
                    

#line default
#line hidden

#line 254 "DetailsPage.cshtml"
                     if (context.User.Claims.Any())
                    {

#line default
#line hidden

            WriteLiteral(@"                    <table id=""claimsTable"">
                        <thead>
                            <tr>
                                <th>Issuer</th>
                                <th>Value</th>
                            </tr>
                        </thead>
                        <tbody>
");
#line 264 "DetailsPage.cshtml"
                            

#line default
#line hidden

#line 264 "DetailsPage.cshtml"
                             foreach (var claim in context.User.Claims)
                                {

#line default
#line hidden

            WriteLiteral("                                <tr>\r\n                                    <td>");
#line 267 "DetailsPage.cshtml"
                                   Write(claim.Issuer);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                    <td>");
#line 268 "DetailsPage.cshtml"
                                   Write(claim.Value);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                </tr>\r\n");
#line 270 "DetailsPage.cshtml"
                                }

#line default
#line hidden

            WriteLiteral("                        </tbody>\r\n                    </table>\r\n");
#line 273 "DetailsPage.cshtml"
                    }

#line default
#line hidden

            WriteLiteral("                </td>\r\n            </tr>\r\n            <tr>\r\n                <th>Scheme</th>\r\n                <td>");
#line 278 "DetailsPage.cshtml"
               Write(context.Scheme);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Query</th>\r\n                <td>");
#line 282 "DetailsPage.cshtml"
               Write(context.Query.Value);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Cookies</th>\r\n                <td>\r\n");
#line 287 "DetailsPage.cshtml"
                    

#line default
#line hidden

#line 287 "DetailsPage.cshtml"
                     if (context.Cookies.Any())
                    {

#line default
#line hidden

            WriteLiteral(@"                    <table id=""cookieTable"">
                        <thead>
                            <tr>
                                <th>Variable</th>
                                <th>Value</th>
                            </tr>
                        </thead>
                        <tbody>
");
#line 297 "DetailsPage.cshtml"
                            

#line default
#line hidden

#line 297 "DetailsPage.cshtml"
                             foreach (var cookie in context.Cookies)
                                {

#line default
#line hidden

            WriteLiteral("                                <tr>\r\n                                    <td>");
#line 300 "DetailsPage.cshtml"
                                   Write(cookie.Key);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                    <td>");
#line 301 "DetailsPage.cshtml"
                                   Write(string.Join(";", cookie.Value));

#line default
#line hidden
            WriteLiteral("</td>\r\n                                </tr>\r\n");
#line 303 "DetailsPage.cshtml"
                                }

#line default
#line hidden

            WriteLiteral("                        </tbody>\r\n                    </table>\r\n");
#line 306 "DetailsPage.cshtml"
                    }

#line default
#line hidden

            WriteLiteral("                </td>\r\n            </tr>\r\n        </table>\r\n");
#line 310 "DetailsPage.cshtml"
    }

#line default
#line hidden

            WriteLiteral("    <h2>Logs</h2>\r\n    <form method=\"get\">\r\n        <select name=\"level\">\r\n");
#line 314 "DetailsPage.cshtml"
            

#line default
#line hidden

#line 314 "DetailsPage.cshtml"
             foreach (var severity in Enum.GetValues(typeof(LogLevel)))
            {
                var severityInt = (int)severity;
                if ((int)Model.Options.MinLevel == severityInt)
                {

#line default
#line hidden

            WriteLiteral("                    <option");
            BeginWriteAttribute("value", " value=\"", 8920, "\"", 8940, 1);
#line 319 "DetailsPage.cshtml"
WriteAttributeValue("", 8928, severityInt, 8928, 12, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" selected=\"selected\">");
#line 319 "DetailsPage.cshtml"
                                                                Write(severity);

#line default
#line hidden
            WriteLiteral("</option>\r\n");
#line 320 "DetailsPage.cshtml"
                }
                else
                {

#line default
#line hidden

            WriteLiteral("                    <option");
            BeginWriteAttribute("value", " value=\"", 9069, "\"", 9089, 1);
#line 323 "DetailsPage.cshtml"
WriteAttributeValue("", 9077, severityInt, 9077, 12, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">");
#line 323 "DetailsPage.cshtml"
                                            Write(severity);

#line default
#line hidden
            WriteLiteral("</option>\r\n");
#line 324 "DetailsPage.cshtml"
                }
            }

#line default
#line hidden

            WriteLiteral("        </select>\r\n        <input type=\"text\" name=\"name\"");
            BeginWriteAttribute("value", " value=\"", 9202, "\"", 9235, 1);
#line 327 "DetailsPage.cshtml"
WriteAttributeValue("", 9210, Model.Options.NamePrefix, 9210, 25, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(@" />
        <input type=""submit"" value=""filter"" />
    </form>
    <table id=""logs"">
        <thead>
            <tr>
                <th>Date</th>
                <th>Time</th>
                <th>Severity</th>
                <th>Name</th>
                <th>State</th>
                <th>Error</th>
            </tr>
        </thead>
        ");
#line 341 "DetailsPage.cshtml"
   Write(Traverse(Model.Activity.Root));

#line default
#line hidden
            WriteLiteral(@"
    </table>
    <script type=""text/javascript"">
        $(document).ready(function () {
            $(""#requestHeader"").click(function () {
                $(""#requestDetails"").toggle();
            });
        });
    </script>
</body>
</html>");
        }
        #pragma warning restore 1998
    }
}
