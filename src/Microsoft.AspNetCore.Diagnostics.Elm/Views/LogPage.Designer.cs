namespace Microsoft.AspNetCore.Diagnostics.Elm.RazorViews
{
#line 1 "LogPage.cshtml"
using System

#line default
#line hidden
    ;
#line 2 "LogPage.cshtml"
using System.Collections.Generic

#line default
#line hidden
    ;
#line 3 "LogPage.cshtml"
using System.Globalization

#line default
#line hidden
    ;
#line 4 "LogPage.cshtml"
using System.Linq

#line default
#line hidden
    ;
#line 5 "LogPage.cshtml"
using Microsoft.AspNetCore.Diagnostics.Elm

#line default
#line hidden
    ;
#line 6 "LogPage.cshtml"
using Microsoft.AspNetCore.Diagnostics.Elm.RazorViews

#line default
#line hidden
    ;
#line 7 "LogPage.cshtml"
using Microsoft.Extensions.RazorViews

#line default
#line hidden
    ;
#line 8 "LogPage.cshtml"
using Microsoft.Extensions.Logging

#line default
#line hidden
    ;
    using System.Threading.Tasks;

    internal class LogPage : Microsoft.Extensions.RazorViews.BaseView
    {
#line 11 "LogPage.cshtml"

    public LogPage(LogPageModel model)
    {
        Model = model;
    }

    public LogPageModel Model { get; set; }

    public HelperResult LogRow(LogInfo log, int level)
    {
        return new HelperResult((writer) =>
        {
            if (log.Severity >= Model.Options.MinLevel &&
                (string.IsNullOrEmpty(Model.Options.NamePrefix) || log.Name.StartsWith(Model.Options.NamePrefix, StringComparison.Ordinal)))
            {

                WriteLiteralTo(writer, "        <tr class=\"logRow\">\r\n            <td>");
                WriteTo(writer, string.Format("{0:MM/dd/yy}", log.Time));

                WriteLiteralTo(writer, "</td>\r\n            <td>");
                WriteTo(writer, string.Format("{0:H:mm:ss}", log.Time));

                WriteLiteralTo(writer, $"</td>\r\n            <td title=\"{log.Name}\">");
                WriteTo(writer, log.Name);
                var severity = log.Severity.ToString().ToLowerInvariant();
                WriteLiteralTo(writer, $"</td>\r\n            <td class=\"{severity}\">");
                WriteTo(writer, log.Severity);

                WriteLiteralTo(writer, $"</td>\r\n            <td title=\"{log.Message}\"> \r\n");

                for (var i = 0; i < level; i++)
                {
                    WriteLiteralTo(writer, "                    <span class=\"tab\"></span>\r\n");
                }

                WriteLiteralTo(writer, "                ");
                WriteTo(writer, log.Message);

                WriteLiteralTo(writer, $"\r\n            </td>\r\n            <td title=\"{log.Exception}\">");

                WriteTo(writer, log.Exception);

                WriteLiteralTo(writer, "</td>\r\n        </tr>\r\n");

            }
        });
    }

    public HelperResult Traverse(ScopeNode node, int level, Dictionary<string, int> counts)
    {
        return new HelperResult((writer) => {
            // print start of scope
            WriteTo(writer, LogRow(new LogInfo()
            {
                Name = node.Name,
                Time = node.StartTime,
                Severity = LogLevel.Debug,
                Message = "Beginning " + node.State,
            }, level));

            var messageIndex = 0;
            var childIndex = 0;
            while (messageIndex < node.Messages.Count && childIndex < node.Children.Count)
            {
                if (node.Messages[messageIndex].Time < node.Children[childIndex].StartTime)
                {
                    WriteTo(writer, LogRow(node.Messages[messageIndex], level));

                    counts[node.Messages[messageIndex].Severity.ToString()]++;
                    messageIndex++;
                }
                else
                {
                    WriteTo(writer, Traverse(node.Children[childIndex], level + 1, counts));
                    childIndex++;
                }
            }
            if (messageIndex < node.Messages.Count)
            {
                for (var i = messageIndex; i < node.Messages.Count; i++)
                {
                    WriteTo(writer, LogRow(node.Messages[i], level));
                    counts[node.Messages[i].Severity.ToString()]++;
                }
            }
            else
            {
                for (var i = childIndex; i < node.Children.Count; i++)
                {
                    WriteTo(writer, Traverse(node.Children[i], level + 1, counts));
                }
            }
            // print end of scope
            WriteTo(writer, LogRow(new LogInfo()
            {
                Name = node.Name,
                Time = node.EndTime,
                Severity = LogLevel.Debug,
                Message = string.Format("Completed {0} in {1}ms", node.State, node.EndTime - node.StartTime)
            }, level));
        });
    }

#line default
#line hidden
        #line hidden
        public LogPage()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            WriteLiteral("\r\n");
#line 114 "LogPage.cshtml"
  
    Response.ContentType = "text/html; charset=utf-8";

#line default
#line hidden

            WriteLiteral(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>ASP.NET Core Logs</title>
    <script src=""//ajax.aspnetcdn.com/ajax/jquery/jquery-2.1.1.min.js""></script>
    <style>
        body {
    font-size: .813em;
    white-space: nowrap;
    margin: 20px;
}

col:nth-child(2n) {
    background-color: #FAFAFA;
}

form { 
    display: inline-block; 
}

h1 {
    margin-left: 25px;
}

table {
    margin: 0px auto;
    border-collapse: collapse;
    border-spacing: 0px;
    table-layout: fixed;
    width: 100%;
}

td, th {
    padding: 4px;
}

thead {
    font-size: 1em;
    font-family: Arial;
}

tr {
    height: 23px;
}

#requestHeader {
    border-bottom: solid 1px gray;
    border-top: solid 1px gray;
    margin-bottom: 2px;
    font-size: 1em;
    line-height: 2em;
}

.collapse {
    color: black;
    float: right;
    font-weight: normal;
    width: 1em;
}

.date, .time {
    width: 70px; 
}

.logHeader {
    border-bottom: 1px ");
            WriteLiteral(@"solid lightgray;
    color: gray;
    text-align: left;
}

.logState {
    text-overflow: ellipsis;
    overflow: hidden;
}

.logTd {
    border-left: 1px solid gray;
    padding: 0px;
}

.logs {
    width: 80%;
}

.logRow:hover {
    background-color: #D6F5FF;
}

.requestRow>td {
    border-bottom: solid 1px gray;
}

.severity {
    width: 80px;
}

.summary {
    color: black;
    line-height: 1.8em;
}

.summary>th {
    font-weight: normal;
}

.tab {
    margin-left: 30px;
}

#viewOptions {
    margin: 20px;
}

#viewOptions > * {
    margin: 5px;
}
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
");
            WriteLiteral("\r\n.information {\r\n    color: blue;\r\n}\r\n\r\n.debug {\r\n    color: black;\r\n}\r\n\r\n.warning {\r\n    color: orange;\r\n}\r\n    </style>\r\n</head>\r\n<body>\r\n    <h1>ASP.NET Core Logs</h1>\r\n    <form id=\"viewOptions\" method=\"get\">\r\n        <select name=\"level\">\r\n");
#line 280 "LogPage.cshtml"
            

#line default
#line hidden

#line 280 "LogPage.cshtml"
             foreach (var severity in Enum.GetValues(typeof(LogLevel)))
            {
                var severityInt = (int)severity;
                if ((int)Model.Options.MinLevel == severityInt)
                {

#line default
#line hidden

            WriteLiteral("                    <option");
            BeginWriteAttribute("value", " value=\"", 6825, "\"", 6845, 1);
#line 285 "LogPage.cshtml"
WriteAttributeValue("", 6833, severityInt, 6833, 12, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" selected=\"selected\">");
#line 285 "LogPage.cshtml"
                                                                Write(severity);

#line default
#line hidden
            WriteLiteral("</option>\r\n");
#line 286 "LogPage.cshtml"
                }
                else
                {

#line default
#line hidden

            WriteLiteral("                    <option");
            BeginWriteAttribute("value", " value=\"", 6974, "\"", 6994, 1);
#line 289 "LogPage.cshtml"
WriteAttributeValue("", 6982, severityInt, 6982, 12, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">");
#line 289 "LogPage.cshtml"
                                            Write(severity);

#line default
#line hidden
            WriteLiteral("</option>\r\n");
#line 290 "LogPage.cshtml"
                }
            }

#line default
#line hidden

            WriteLiteral("        </select>\r\n        <input type=\"text\" name=\"name\"");
            BeginWriteAttribute("value", " value=\"", 7107, "\"", 7140, 1);
#line 293 "LogPage.cshtml"
WriteAttributeValue("", 7115, Model.Options.NamePrefix, 7115, 25, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(@" />
        <input type=""submit"" value=""filter"" />
    </form>
    <form id=""clear"" method=""post"" action="""">
        <button type=""submit"" name=""clear"" value=""1"">Clear Logs</button>
    </form>

    <table id=""requestTable"">
        <thead id=""requestHeader"">
            <tr>
                <th class=""path"">Path</th>
                <th class=""method"">Method</th>
                <th class=""host"">Host</th>
                <th class=""statusCode"">Status Code</th>
                <th class=""logs"">Logs</th>
            </tr>
        </thead>
        <colgroup>
            <col />
            <col />
            <col />
            <col />
            <col />
        </colgroup>
");
#line 317 "LogPage.cshtml"
        

#line default
#line hidden

#line 317 "LogPage.cshtml"
         foreach (var activity in Model.Activities.Reverse())
        {

#line default
#line hidden

            WriteLiteral("            <tbody>\r\n                <tr class=\"requestRow\">\r\n");
#line 321 "LogPage.cshtml"
                    

#line default
#line hidden

#line 321 "LogPage.cshtml"
                      
                        var activityPath = Model.Path.Value + "/" + activity.Id;
                        if (activity.HttpInfo != null)
                        {

#line default
#line hidden

            WriteLiteral("                        \t<td><a");
            BeginWriteAttribute("href", " href=\"", 8204, "\"", 8224, 1);
#line 325 "LogPage.cshtml"
WriteAttributeValue("", 8211, activityPath, 8211, 13, false);

#line default
#line hidden
            EndWriteAttribute();
            BeginWriteAttribute("title", " title=\"", 8225, "\"", 8256, 1);
#line 325 "LogPage.cshtml"
WriteAttributeValue("", 8233, activity.HttpInfo.Path, 8233, 23, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">");
#line 325 "LogPage.cshtml"
                                                                                   Write(activity.HttpInfo.Path);

#line default
#line hidden
            WriteLiteral("</a></td>\r\n                            <td>");
#line 326 "LogPage.cshtml"
                           Write(activity.HttpInfo.Method);

#line default
#line hidden
            WriteLiteral("</td>\r\n                            <td>");
#line 327 "LogPage.cshtml"
                           Write(activity.HttpInfo.Host);

#line default
#line hidden
            WriteLiteral("</td>\r\n                            <td>");
#line 328 "LogPage.cshtml"
                           Write(activity.HttpInfo.StatusCode);

#line default
#line hidden
            WriteLiteral("</td>\r\n");
#line 329 "LogPage.cshtml"
                        }
                        else if (activity.RepresentsScope)
                        {

#line default
#line hidden

            WriteLiteral("                            <td colspan=\"4\"><a");
            BeginWriteAttribute("href", " href=\"", 8646, "\"", 8666, 1);
#line 332 "LogPage.cshtml"
WriteAttributeValue("", 8653, activityPath, 8653, 13, false);

#line default
#line hidden
            EndWriteAttribute();
            BeginWriteAttribute("title", " title=\"", 8667, "\"", 8695, 1);
#line 332 "LogPage.cshtml"
WriteAttributeValue("", 8675, activity.Root.State, 8675, 20, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">");
#line 332 "LogPage.cshtml"
                                                                                            Write(activity.Root.State);

#line default
#line hidden
            WriteLiteral("</a></td>\r\n");
#line 333 "LogPage.cshtml"
                        }
                        else
                        {

#line default
#line hidden

            WriteLiteral("                            <td colspan=\"4\"><a");
            BeginWriteAttribute("href", " href=\"", 8858, "\"", 8878, 1);
#line 336 "LogPage.cshtml"
WriteAttributeValue("", 8865, activityPath, 8865, 13, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">Non-scope Log</a></td>\r\n");
#line 337 "LogPage.cshtml"
                        }
                    

#line default
#line hidden

            WriteLiteral(@"                    <td class=""logTd"">
                        <table class=""logTable"">
                            <thead class=""logHeader"">
                                <tr class=""headerRow"">
                                    <th class=""date"">Date</th>
                                    <th class=""time"">Time</th>
                                    <th class=""name"">Name</th>
                                    <th class=""severity"">Severity</th>
                                    <th class=""state"">State</th>
                                    <th>Error<span class=""collapse"">^</span></th>
                                </tr>
                            </thead>
");
#line 351 "LogPage.cshtml"
                            

#line default
#line hidden

#line 351 "LogPage.cshtml"
                              
                                var counts = new Dictionary<string, int>();
                                counts["Critical"] = 0;
                                counts["Error"] = 0;
                                counts["Warning"] = 0;
                                counts["Information"] = 0;
                                counts["Debug"] = 0;
                            

#line default
#line hidden

            WriteLiteral("                            <tbody class=\"logBody\">\r\n");
#line 360 "LogPage.cshtml"
                                

#line default
#line hidden

#line 360 "LogPage.cshtml"
                                 if (!activity.RepresentsScope)
                                {
                                    // message not within a scope
                                    var logInfo = activity.Root.Messages.FirstOrDefault();
                                    

#line default
#line hidden

#line 364 "LogPage.cshtml"
                               Write(LogRow(logInfo, 0));

#line default
#line hidden
#line 364 "LogPage.cshtml"
                                                       
                                    counts[logInfo.Severity.ToString()] = 1;
                                }
                                else
                                {
                                    

#line default
#line hidden

#line 369 "LogPage.cshtml"
                               Write(Traverse(activity.Root, 0, counts));

#line default
#line hidden
#line 369 "LogPage.cshtml"
                                                                       
                                }

#line default
#line hidden

            WriteLiteral("                            </tbody>\r\n                            <tbody class=\"summary\">\r\n                                <tr class=\"logRow\">\r\n                                    <td>");
#line 374 "LogPage.cshtml"
                                   Write(activity.Time.ToString("MM-dd-yyyy HH:mm:ss"));

#line default
#line hidden
            WriteLiteral("</td>\r\n");
#line 375 "LogPage.cshtml"
                                    

#line default
#line hidden

#line 375 "LogPage.cshtml"
                                     foreach (var kvp in counts)
                                    {
                                        if (string.Equals("Debug", kvp.Key)) {

#line default
#line hidden

            WriteLiteral("                                            <td>");
#line 378 "LogPage.cshtml"
                                           Write(kvp.Value);

#line default
#line hidden
            WriteLiteral(" ");
#line 378 "LogPage.cshtml"
                                                      Write(kvp.Key);

#line default
#line hidden
            WriteLiteral("<span class=\"collapse\">v</span></td>\r\n");
#line 379 "LogPage.cshtml"
                                        }
                                        else
                                        {

#line default
#line hidden

            WriteLiteral("                                            <td>");
#line 382 "LogPage.cshtml"
                                           Write(kvp.Value);

#line default
#line hidden
            WriteLiteral(" ");
#line 382 "LogPage.cshtml"
                                                      Write(kvp.Key);

#line default
#line hidden
            WriteLiteral("</td>\r\n");
#line 383 "LogPage.cshtml"
                                        }
                                    }

#line default
#line hidden

            WriteLiteral("                                </tr>\r\n                            </tbody>\r\n                        </table>\r\n                    </td>\r\n                </tr>\r\n            </tbody>\r\n");
#line 391 "LogPage.cshtml"
        }

#line default
#line hidden

            WriteLiteral(@"    </table>
    <script type=""text/javascript"">
        $(document).ready(function () {
            $("".logBody"").hide();
            $("".logTable > thead"").hide();
            $("".logTable > thead"").click(function () {
                $(this).closest("".logTable"").find(""tbody"").hide();
                $(this).closest("".logTable"").find("".summary"").show();
                $(this).hide();
            });
            $("".logTable > .summary"").click(function () {
                $(this).closest("".logTable"").find(""tbody"").show();
                $(this).closest("".logTable"").find(""thead"").show();
                $(this).hide();
            });
        });
    </script>
</body>
</html>");
        }
        #pragma warning restore 1998
    }
}
