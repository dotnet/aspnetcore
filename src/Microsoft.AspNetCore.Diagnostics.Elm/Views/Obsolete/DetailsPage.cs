namespace Microsoft.AspNetCore.Diagnostics.Elm.Views
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
using Microsoft.AspNetCore.Diagnostics.Elm.Views

#line default
#line hidden
    ;
#line 6 "DetailsPage.cshtml"
using Microsoft.AspNetCore.DiagnosticsViewPage.Views

#line default
#line hidden
    ;
#line 7 "DetailsPage.cshtml"
using Microsoft.Extensions.Logging

#line default
#line hidden
    ;
    using System.Threading.Tasks;
    [Obsolete("This type is for internal use only and will be removed in a future version.")]
    public class DetailsPage : Microsoft.AspNetCore.DiagnosticsViewPage.Views.BaseView
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
  
    Response.ContentType = "text/html";

#line default
#line hidden

            WriteLiteral(@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>ASP.NET Core Logs</title>
    <script src=""http://ajax.aspnetcdn.com/ajax/jquery/jquery-2.1.1.min.js""></script>
    <style>
        <%$ include: Shared.css % > <%$ include: DetailsPage.css % >
    </style>
</head>
<body>
    <h1>ASP.NET Core Logs</h1>
");
#line 99 "DetailsPage.cshtml"
    

#line default
#line hidden

#line 99 "DetailsPage.cshtml"
      
        var context = Model.Activity?.HttpInfo;
    

#line default
#line hidden

            WriteLiteral("    ");
#line 102 "DetailsPage.cshtml"
     if (context != null)
    {

#line default
#line hidden

            WriteLiteral("        <h2 id=\"requestHeader\">Request Details</h2>\r\n        <table id=\"requestDetails\">\r\n            <colgroup><col id=\"label\" /><col /></colgroup>\r\n\r\n            <tr>\r\n                <th>Path</th>\r\n                <td>");
#line 110 "DetailsPage.cshtml"
               Write(context.Path);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Host</th>\r\n                <td>");
#line 114 "DetailsPage.cshtml"
               Write(context.Host);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Content Type</th>\r\n                <td>");
#line 118 "DetailsPage.cshtml"
               Write(context.ContentType);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Method</th>\r\n                <td>");
#line 122 "DetailsPage.cshtml"
               Write(context.Method);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Protocol</th>\r\n                <td>");
#line 126 "DetailsPage.cshtml"
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
#line 139 "DetailsPage.cshtml"
                            

#line default
#line hidden

#line 139 "DetailsPage.cshtml"
                             foreach (var header in context.Headers)
                            {

#line default
#line hidden

            WriteLiteral("                            <tr>\r\n                                <td>");
#line 142 "DetailsPage.cshtml"
                               Write(header.Key);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                <td>");
#line 143 "DetailsPage.cshtml"
                               Write(string.Join(";", header.Value));

#line default
#line hidden
            WriteLiteral("</td>\r\n                            </tr>\r\n");
#line 145 "DetailsPage.cshtml"
                            }

#line default
#line hidden

            WriteLiteral("                        </tbody>\r\n                    </table>\r\n                </td>\r\n            </tr>\r\n            <tr>\r\n                <th>Status Code</th>\r\n                <td>");
#line 152 "DetailsPage.cshtml"
               Write(context.StatusCode);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>User</th>\r\n                <td>");
#line 156 "DetailsPage.cshtml"
               Write(context.User.Identity.Name);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Claims</th>\r\n                <td>\r\n");
#line 161 "DetailsPage.cshtml"
                    

#line default
#line hidden

#line 161 "DetailsPage.cshtml"
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
#line 171 "DetailsPage.cshtml"
                            

#line default
#line hidden

#line 171 "DetailsPage.cshtml"
                             foreach (var claim in context.User.Claims)
                                {

#line default
#line hidden

            WriteLiteral("                                <tr>\r\n                                    <td>");
#line 174 "DetailsPage.cshtml"
                                   Write(claim.Issuer);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                    <td>");
#line 175 "DetailsPage.cshtml"
                                   Write(claim.Value);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                </tr>\r\n");
#line 177 "DetailsPage.cshtml"
                                }

#line default
#line hidden

            WriteLiteral("                        </tbody>\r\n                    </table>\r\n");
#line 180 "DetailsPage.cshtml"
                    }

#line default
#line hidden

            WriteLiteral("                </td>\r\n            </tr>\r\n            <tr>\r\n                <th>Scheme</th>\r\n                <td>");
#line 185 "DetailsPage.cshtml"
               Write(context.Scheme);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Query</th>\r\n                <td>");
#line 189 "DetailsPage.cshtml"
               Write(context.Query.Value);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Cookies</th>\r\n                <td>\r\n");
#line 194 "DetailsPage.cshtml"
                    

#line default
#line hidden

#line 194 "DetailsPage.cshtml"
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
#line 204 "DetailsPage.cshtml"
                            

#line default
#line hidden

#line 204 "DetailsPage.cshtml"
                             foreach (var cookie in context.Cookies)
                                {

#line default
#line hidden

            WriteLiteral("                                <tr>\r\n                                    <td>");
#line 207 "DetailsPage.cshtml"
                                   Write(cookie.Key);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                    <td>");
#line 208 "DetailsPage.cshtml"
                                   Write(string.Join(";", cookie.Value));

#line default
#line hidden
            WriteLiteral("</td>\r\n                                </tr>\r\n");
#line 210 "DetailsPage.cshtml"
                                }

#line default
#line hidden

            WriteLiteral("                        </tbody>\r\n                    </table>\r\n");
#line 213 "DetailsPage.cshtml"
                    }

#line default
#line hidden

            WriteLiteral("                </td>\r\n            </tr>\r\n        </table>\r\n");
#line 217 "DetailsPage.cshtml"
    }

#line default
#line hidden

            WriteLiteral("    <h2>Logs</h2>\r\n    <form method=\"get\">\r\n        <select name=\"level\">\r\n");
#line 221 "DetailsPage.cshtml"
            

#line default
#line hidden

#line 221 "DetailsPage.cshtml"
             foreach (var severity in Enum.GetValues(typeof(LogLevel)))
            {
                var severityInt = (int)severity;
                if ((int)Model.Options.MinLevel == severityInt)
                {

#line default
#line hidden

            WriteLiteral("                    <option");
            BeginWriteAttribute("value", " value=\"", 7723, "\"", 7743, 1);
#line 226 "DetailsPage.cshtml"
WriteAttributeValue("", 7731, severityInt, 7731, 12, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" selected=\"selected\">");
#line 226 "DetailsPage.cshtml"
                                                                Write(severity);

#line default
#line hidden
            WriteLiteral("</option>\r\n");
#line 227 "DetailsPage.cshtml"
                }
                else
                {

#line default
#line hidden

            WriteLiteral("                    <option");
            BeginWriteAttribute("value", " value=\"", 7872, "\"", 7892, 1);
#line 230 "DetailsPage.cshtml"
WriteAttributeValue("", 7880, severityInt, 7880, 12, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">");
#line 230 "DetailsPage.cshtml"
                                            Write(severity);

#line default
#line hidden
            WriteLiteral("</option>\r\n");
#line 231 "DetailsPage.cshtml"
                }
            }

#line default
#line hidden

            WriteLiteral("        </select>\r\n        <input type=\"text\" name=\"name\"");
            BeginWriteAttribute("value", " value=\"", 8005, "\"", 8038, 1);
#line 234 "DetailsPage.cshtml"
WriteAttributeValue("", 8013, Model.Options.NamePrefix, 8013, 25, false);

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
#line 248 "DetailsPage.cshtml"
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
