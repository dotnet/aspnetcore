namespace Microsoft.AspNet.Diagnostics.Elm.Views
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
using Microsoft.AspNet.Diagnostics.Elm

#line default
#line hidden
    ;
#line 5 "DetailsPage.cshtml"
using Microsoft.AspNet.Diagnostics.Views

#line default
#line hidden
    ;
#line 6 "DetailsPage.cshtml"
using Microsoft.AspNet.Diagnostics.Elm.Views

#line default
#line hidden
    ;
#line 7 "DetailsPage.cshtml"
using Microsoft.Framework.Logging

#line default
#line hidden
    ;
    using System.Threading.Tasks;

    public class DetailsPage : Microsoft.AspNet.Diagnostics.Views.BaseView
    {
public  HelperResult 
#line 19 "DetailsPage.cshtml"
LogRow(LogInfo log)
{

#line default
#line hidden
        return new HelperResult((__razor_helper_writer) => {
#line 20 "DetailsPage.cshtml"
 
    if (log.Severity >= Model.Options.MinLevel &&
        (string.IsNullOrEmpty(Model.Options.NamePrefix) || log.Name.StartsWith(Model.Options.NamePrefix, StringComparison.Ordinal)))
    {

#line default
#line hidden

            WriteLiteralTo(__razor_helper_writer, "        <tr>\r\n            <td>");
#line 25 "DetailsPage.cshtml"
WriteTo(__razor_helper_writer, string.Format("{0:MM/dd/yy}", log.Time));

#line default
#line hidden
            WriteLiteralTo(__razor_helper_writer, "</td>\r\n            <td>");
#line 26 "DetailsPage.cshtml"
WriteTo(__razor_helper_writer, string.Format("{0:H:mm:ss}", log.Time));

#line default
#line hidden
            WriteLiteralTo(__razor_helper_writer, "</td>\r\n            <td");
            WriteAttributeTo(__razor_helper_writer, "class", Tuple.Create(" class=\"", 768), Tuple.Create("\"", 819), 
            Tuple.Create(Tuple.Create("", 776), Tuple.Create<System.Object, System.Int32>(log.Severity.ToString().ToLowerInvariant(), 776), false));
            WriteLiteralTo(__razor_helper_writer, ">");
#line 27 "DetailsPage.cshtml"
                                      WriteTo(__razor_helper_writer, log.Severity);

#line default
#line hidden
            WriteLiteralTo(__razor_helper_writer, "</td>\r\n            <td");
            WriteAttributeTo(__razor_helper_writer, "title", Tuple.Create(" title=\"", 856), Tuple.Create("\"", 873), 
            Tuple.Create(Tuple.Create("", 864), Tuple.Create<System.Object, System.Int32>(log.Name, 864), false));
            WriteLiteralTo(__razor_helper_writer, ">");
#line 28 "DetailsPage.cshtml"
    WriteTo(__razor_helper_writer, log.Name);

#line default
#line hidden
            WriteLiteralTo(__razor_helper_writer, "</td>\r\n            <td");
            WriteAttributeTo(__razor_helper_writer, "title", Tuple.Create(" title=\"", 906), Tuple.Create("\"", 926), 
            Tuple.Create(Tuple.Create("", 914), Tuple.Create<System.Object, System.Int32>(log.Message, 914), false));
            WriteLiteralTo(__razor_helper_writer, " class=\"logState\" width=\"100px\">");
#line 29 "DetailsPage.cshtml"
                                      WriteTo(__razor_helper_writer, log.Message);

#line default
#line hidden
            WriteLiteralTo(__razor_helper_writer, "</td>\r\n            <td");
            WriteAttributeTo(__razor_helper_writer, "title", Tuple.Create(" title=\"", 993), Tuple.Create("\"", 1015), 
            Tuple.Create(Tuple.Create("", 1001), Tuple.Create<System.Object, System.Int32>(log.Exception, 1001), false));
            WriteLiteralTo(__razor_helper_writer, ">");
#line 30 "DetailsPage.cshtml"
         WriteTo(__razor_helper_writer, log.Exception);

#line default
#line hidden
            WriteLiteralTo(__razor_helper_writer, "</td>\r\n        </tr>\r\n");
#line 32 "DetailsPage.cshtml"
    }

#line default
#line hidden

        }
        );
#line 33 "DetailsPage.cshtml"
}

#line default
#line hidden

public  HelperResult 
#line 35 "DetailsPage.cshtml"
Traverse(ScopeNode node)
{

#line default
#line hidden
        return new HelperResult((__razor_helper_writer) => {
#line 36 "DetailsPage.cshtml"
 
    var messageIndex = 0;
    var childIndex = 0;
    while (messageIndex < node.Messages.Count && childIndex < node.Children.Count)
    {
        if (node.Messages[messageIndex].Time < node.Children[childIndex].StartTime)
        {
            

#line default
#line hidden

#line 43 "DetailsPage.cshtml"
WriteTo(__razor_helper_writer, LogRow(node.Messages[messageIndex]));

#line default
#line hidden
#line 43 "DetailsPage.cshtml"
                                                
            messageIndex++;
        }
        else
        {
            

#line default
#line hidden

#line 48 "DetailsPage.cshtml"
WriteTo(__razor_helper_writer, Traverse(node.Children[childIndex]));

#line default
#line hidden
#line 48 "DetailsPage.cshtml"
                                                
            childIndex++;
        }
    }
    if (messageIndex < node.Messages.Count)
    {
        for (var i = messageIndex; i < node.Messages.Count; i++)
        {
            

#line default
#line hidden

#line 56 "DetailsPage.cshtml"
WriteTo(__razor_helper_writer, LogRow(node.Messages[i]));

#line default
#line hidden
#line 56 "DetailsPage.cshtml"
                                     
        }
    }
    else
    {
        for (var i = childIndex; i < node.Children.Count; i++)
        {
            

#line default
#line hidden

#line 63 "DetailsPage.cshtml"
WriteTo(__razor_helper_writer, Traverse(node.Children[i]));

#line default
#line hidden
#line 63 "DetailsPage.cshtml"
                                       
        }
    }

#line default
#line hidden

        }
        );
#line 66 "DetailsPage.cshtml"
}

#line default
#line hidden

#line 10 "DetailsPage.cshtml"

    public DetailsPage(DetailsPageModel model)
    {
        Model = model;
    }

    public DetailsPageModel Model { get; set; }

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
            WriteLiteral("\r\n");
            WriteLiteral("\r\n");
            WriteLiteral(@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>ASP.NET Logs</title>
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

.verbose {
    color: black;
}

.warning {
    color: orange;
}
        body {
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
    <h1>ASP.NET Logs</h1>
");
#line 80 "DetailsPage.cshtml"
    

#line default
#line hidden

#line 80 "DetailsPage.cshtml"
      
        var context = Model.Activity?.HttpInfo;
    

#line default
#line hidden

            WriteLiteral("\r\n");
#line 83 "DetailsPage.cshtml"
    

#line default
#line hidden

#line 83 "DetailsPage.cshtml"
     if (context != null)
    {

#line default
#line hidden

            WriteLiteral("        <h2 id=\"requestHeader\">Request Details</h2>\r\n        <table id=\"requestDe" +
"tails\">\r\n            <colgroup><col id=\"label\" /><col /></colgroup>\r\n\r\n         " +
"   <tr>\r\n                <th>Path</th>\r\n                <td>");
#line 91 "DetailsPage.cshtml"
               Write(context.Path);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Host</th>\r\n      " +
"          <td>");
#line 95 "DetailsPage.cshtml"
               Write(context.Host);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Content Type</th>" +
"\r\n                <td>");
#line 99 "DetailsPage.cshtml"
               Write(context.ContentType);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Method</th>\r\n    " +
"            <td>");
#line 103 "DetailsPage.cshtml"
               Write(context.Method);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Protocol</th>\r\n  " +
"              <td>");
#line 107 "DetailsPage.cshtml"
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
#line 120 "DetailsPage.cshtml"
                            

#line default
#line hidden

#line 120 "DetailsPage.cshtml"
                             foreach (var header in context.Headers)
                            {

#line default
#line hidden

            WriteLiteral("                                <tr>\r\n                                    <td>");
#line 123 "DetailsPage.cshtml"
                                   Write(header.Key);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                    <td>");
#line 124 "DetailsPage.cshtml"
                                   Write(string.Join(";", header.Value));

#line default
#line hidden
            WriteLiteral("</td>\r\n                                </tr>\r\n");
#line 126 "DetailsPage.cshtml"
                            }

#line default
#line hidden

            WriteLiteral("                        </tbody>\r\n                    </table>\r\n                <" +
"/td>\r\n            </tr>\r\n            <tr>\r\n                <th>Status Code</th>\r" +
"\n                <td>");
#line 133 "DetailsPage.cshtml"
               Write(context.StatusCode);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>User</th>\r\n      " +
"          <td>");
#line 137 "DetailsPage.cshtml"
               Write(context.User.Identity.Name);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Claims</th>\r\n    " +
"            <td>\r\n");
#line 142 "DetailsPage.cshtml"
                    

#line default
#line hidden

#line 142 "DetailsPage.cshtml"
                     if (context.User.Claims.Any())
                    {

#line default
#line hidden

            WriteLiteral(@"                        <table id=""claimsTable"">
                            <thead>
                                <tr>
                                    <th>Issuer</th>
                                    <th>Value</th>
                                </tr>
                            </thead>
                            <tbody>
");
#line 152 "DetailsPage.cshtml"
                                

#line default
#line hidden

#line 152 "DetailsPage.cshtml"
                                 foreach (var claim in context.User.Claims)
                                {

#line default
#line hidden

            WriteLiteral("                                    <tr>\r\n                                       " +
" <td>");
#line 155 "DetailsPage.cshtml"
                                       Write(claim.Issuer);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                        <td>");
#line 156 "DetailsPage.cshtml"
                                       Write(claim.Value);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                    </tr>\r\n");
#line 158 "DetailsPage.cshtml"
                                }

#line default
#line hidden

            WriteLiteral("                            </tbody>\r\n                        </table>\r\n");
#line 161 "DetailsPage.cshtml"
                    }

#line default
#line hidden

            WriteLiteral("                </td>\r\n            </tr>\r\n            <tr>\r\n                <th>S" +
"cheme</th>\r\n                <td>");
#line 166 "DetailsPage.cshtml"
               Write(context.Scheme);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Query</th>\r\n     " +
"           <td>");
#line 170 "DetailsPage.cshtml"
               Write(context.Query.Value);

#line default
#line hidden
            WriteLiteral("</td>\r\n            </tr>\r\n            <tr>\r\n                <th>Cookies</th>\r\n   " +
"             <td>\r\n");
#line 175 "DetailsPage.cshtml"
                    

#line default
#line hidden

#line 175 "DetailsPage.cshtml"
                     if (context.Cookies.Any())
                    {

#line default
#line hidden

            WriteLiteral(@"                        <table id=""cookieTable"">
                            <thead>
                                <tr>
                                    <th>Variable</th>
                                    <th>Value</th>
                                </tr>
                            </thead>
                            <tbody>
");
#line 185 "DetailsPage.cshtml"
                                

#line default
#line hidden

#line 185 "DetailsPage.cshtml"
                                 foreach (var cookie in context.Cookies)
                                {

#line default
#line hidden

            WriteLiteral("                                    <tr>\r\n                                       " +
" <td>");
#line 188 "DetailsPage.cshtml"
                                       Write(cookie.Key);

#line default
#line hidden
            WriteLiteral("</td>\r\n                                        <td>");
#line 189 "DetailsPage.cshtml"
                                       Write(string.Join(";", cookie.Value));

#line default
#line hidden
            WriteLiteral("</td>\r\n                                    </tr>\r\n");
#line 191 "DetailsPage.cshtml"
                                }

#line default
#line hidden

            WriteLiteral("                            </tbody>\r\n                        </table>\r\n");
#line 194 "DetailsPage.cshtml"
                    }

#line default
#line hidden

            WriteLiteral("                </td>\r\n            </tr>\r\n        </table>\r\n");
#line 198 "DetailsPage.cshtml"
    }

#line default
#line hidden

            WriteLiteral("    <h2>Logs</h2>\r\n    <form method=\"get\">\r\n        <select name=\"level\">\r\n");
#line 202 "DetailsPage.cshtml"
            

#line default
#line hidden

#line 202 "DetailsPage.cshtml"
             foreach (var severity in Enum.GetValues(typeof(LogLevel)))
            {
                var severityInt = (int)severity;
                if ((int)Model.Options.MinLevel == severityInt)
                {

#line default
#line hidden

            WriteLiteral("                    <option");
            WriteAttribute("value", Tuple.Create(" value=\"", 6703), Tuple.Create("\"", 6723), 
            Tuple.Create(Tuple.Create("", 6711), Tuple.Create<System.Object, System.Int32>(severityInt, 6711), false));
            WriteLiteral(" selected=\"selected\">");
#line 207 "DetailsPage.cshtml"
                                                                Write(severity);

#line default
#line hidden
            WriteLiteral("</option>\r\n");
#line 208 "DetailsPage.cshtml"
                }
                else
                {

#line default
#line hidden

            WriteLiteral("                    <option");
            WriteAttribute("value", Tuple.Create(" value=\"", 6852), Tuple.Create("\"", 6872), 
            Tuple.Create(Tuple.Create("", 6860), Tuple.Create<System.Object, System.Int32>(severityInt, 6860), false));
            WriteLiteral(">");
#line 211 "DetailsPage.cshtml"
                                            Write(severity);

#line default
#line hidden
            WriteLiteral("</option>\r\n");
#line 212 "DetailsPage.cshtml"
                }
            }

#line default
#line hidden

            WriteLiteral("        </select>\r\n        <input type=\"text\" name=\"name\"");
            WriteAttribute("value", Tuple.Create(" value=\"", 6985), Tuple.Create("\"", 7018), 
            Tuple.Create(Tuple.Create("", 6993), Tuple.Create<System.Object, System.Int32>(Model.Options.NamePrefix, 6993), false));
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
#line 229 "DetailsPage.cshtml"
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
