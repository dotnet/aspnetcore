namespace Microsoft.AspNetCore.Diagnostics.Elm.Views
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
using Microsoft.AspNetCore.Diagnostics.Elm.Views

#line default
#line hidden
    ;
#line 7 "LogPage.cshtml"
using Microsoft.AspNetCore.DiagnosticsViewPage.Views

#line default
#line hidden
    ;
#line 8 "LogPage.cshtml"
using Microsoft.Extensions.Logging

#line default
#line hidden
    ;
    using System.Threading.Tasks;
    [Obsolete("This type is for internal use only and will be removed in a future version.")]
    public class LogPage : Microsoft.AspNetCore.DiagnosticsViewPage.Views.BaseView
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
   
    Response.ContentType = "text/html";

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
        body {\r\n    font-size: .813em;\r\n    white-space: nowrap;\r\n    margin: 20px;\r\n}\r\n\r\ncol:nth-child(2n) {\r\n    background-color: #FAFAFA;\r\n}\r\n\r\nform { \r\n    display: inline-block; \r\n}\r\n\r\nh1 {\r\n    margin-left: 25px;\r\n}\r\n\r\ntable {\r\n    margin: 0px auto;\r\n    border-collapse: collapse;\r\n    border-spacing: 0px;\r\n    table-layout: fixed;\r\n    width: 100%;\r\n}\r\n\r\ntd, th {\r\n    padding: 4px;\r\n}\r\n\r\nthead {\r\n    font-size: 1em;\r\n    font-family: Arial;\r\n}\r\n\r\ntr {\r\n    height: 23px;\r\n}\r\n\r\n#requestHeader {\r\n    border-bottom: solid 1px gray;\r\n    border-top: solid 1px gray;\r\n    margin-bottom: 2px;\r\n    font-size: 1em;\r\n    line-height: 2em;\r\n}\r\n\r\n.collapse {\r\n    color: black;\r\n    float: right;\r\n    font-weight: normal;\r\n    width: 1em;\r\n}\r\n\r\n.date, .time {\r\n    width: 70px; \r\n}\r\n\r\n.logHeader {\r\n    border-bottom: 1px solid lightgray;\r\n    color: gray;\r\n    text-align: left;\r\n}\r\n\r\n.logState {\r\n    text-overflow: ellipsis;\r\n    overflow: hidden;\r\n}\r\n\r\n.logTd {\r\n    border-left: 1px solid gray;\r\n    padding: 0px;\r\n}\r\n\r\n.logs {\r\n    width: 80%;\r\n}\r\n\r\n.logRow:hover {\r\n    background-color: #D6F5FF;\r\n}\r\n\r\n.requestRow>td {\r\n    border-bottom: solid 1px gray;\r\n}\r\n\r\n.severity {\r\n    width: 80px;\r\n}\r\n\r\n.summary {\r\n    color: black;\r\n    line-height: 1.8em;\r\n}\r\n\r\n.summary>th {\r\n    font-weight: normal;\r\n}\r\n\r\n.tab {\r\n    margin-left: 30px;\r\n}\r\n\r\n#viewOptions {\r\n    margin: 20px;\r\n}\r\n\r\n#viewOptions > * {\r\n    margin: 5px;\r\n}
        body {\r\n    font-family: 'Segoe UI', Tahoma, Arial, Helvtica, sans-serif;\r\n    line-height: 1.4em;\r\n}\r\n\r\nh1 {\r\n    font-family: 'Segoe UI', Helvetica, sans-serif;\r\n    font-size: 2.5em;\r\n}\r\n\r\ntd {\r\n    text-overflow: ellipsis;\r\n    overflow: hidden;\r\n}\r\n\r\ntr:nth-child(2n) {\r\n    background-color: #F6F6F6;\r\n}\r\n\r\n.critical {\r\n    background-color: red;\r\n    color: white;\r\n}\r\n\r\n.error {\r\n    color: red;\r\n}\r\n\r\n.information {\r\n    color: blue;\r\n}\r\n\r\n.debug {\r\n    color: black;\r\n}\r\n\r\n.warning {\r\n    color: orange;\r\n}
    </style>
</head>
<body>
    <h1>ASP.NET Core Logs</h1>
    <form id=""viewOptions"" method=""get"">
        <select name=""level"">
");
#line 133 "LogPage.cshtml"
            

#line default
#line hidden

#line 133 "LogPage.cshtml"
             foreach (var severity in Enum.GetValues(typeof(LogLevel)))
            {
                var severityInt = (int)severity;
                if ((int)Model.Options.MinLevel == severityInt)
                {

#line default
#line hidden

            WriteLiteral("                    <option");
            BeginWriteAttribute("value", " value=\"", 4934, "\"", 4954, 1);
#line 138 "LogPage.cshtml"
WriteAttributeValue("", 4942, severityInt, 4942, 12, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(" selected=\"selected\">");
#line 138 "LogPage.cshtml"
                                                                Write(severity);

#line default
#line hidden
            WriteLiteral("</option>\r\n");
#line 139 "LogPage.cshtml"
                }
                else
                {

#line default
#line hidden

            WriteLiteral("                    <option");
            BeginWriteAttribute("value", " value=\"", 5083, "\"", 5103, 1);
#line 142 "LogPage.cshtml"
WriteAttributeValue("", 5091, severityInt, 5091, 12, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">");
#line 142 "LogPage.cshtml"
                                            Write(severity);

#line default
#line hidden
            WriteLiteral("</option>\r\n");
#line 143 "LogPage.cshtml"
                }
            }

#line default
#line hidden

            WriteLiteral("        </select>\r\n        <input type=\"text\" name=\"name\"");
            BeginWriteAttribute("value", " value=\"", 5216, "\"", 5249, 1);
#line 146 "LogPage.cshtml"
WriteAttributeValue("", 5224, Model.Options.NamePrefix, 5224, 25, false);

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
#line 170 "LogPage.cshtml"
        

#line default
#line hidden

#line 170 "LogPage.cshtml"
         foreach (var activity in Model.Activities.Reverse())
        {

#line default
#line hidden

            WriteLiteral("            <tbody>\r\n                <tr class=\"requestRow\">\r\n");
#line 174 "LogPage.cshtml"
                    

#line default
#line hidden

#line 174 "LogPage.cshtml"
                      
                        var activityPath = Model.Path.Value + "/" + activity.Id;
                        if (activity.HttpInfo != null)
                        {

#line default
#line hidden

            WriteLiteral("                        \t<td><a");
            BeginWriteAttribute("href", " href=\"", 6313, "\"", 6333, 1);
#line 178 "LogPage.cshtml"
WriteAttributeValue("", 6320, activityPath, 6320, 13, false);

#line default
#line hidden
            EndWriteAttribute();
            BeginWriteAttribute("title", " title=\"", 6334, "\"", 6365, 1);
#line 178 "LogPage.cshtml"
WriteAttributeValue("", 6342, activity.HttpInfo.Path, 6342, 23, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">");
#line 178 "LogPage.cshtml"
                                                                                   Write(activity.HttpInfo.Path);

#line default
#line hidden
            WriteLiteral("</a></td>\r\n                            <td>");
#line 179 "LogPage.cshtml"
                           Write(activity.HttpInfo.Method);

#line default
#line hidden
            WriteLiteral("</td>\r\n                            <td>");
#line 180 "LogPage.cshtml"
                           Write(activity.HttpInfo.Host);

#line default
#line hidden
            WriteLiteral("</td>\r\n                            <td>");
#line 181 "LogPage.cshtml"
                           Write(activity.HttpInfo.StatusCode);

#line default
#line hidden
            WriteLiteral("</td>\r\n");
#line 182 "LogPage.cshtml"
                        }
                        else if (activity.RepresentsScope)
                        {

#line default
#line hidden

            WriteLiteral("                            <td colspan=\"4\"><a");
            BeginWriteAttribute("href", " href=\"", 6755, "\"", 6775, 1);
#line 185 "LogPage.cshtml"
WriteAttributeValue("", 6762, activityPath, 6762, 13, false);

#line default
#line hidden
            EndWriteAttribute();
            BeginWriteAttribute("title", " title=\"", 6776, "\"", 6804, 1);
#line 185 "LogPage.cshtml"
WriteAttributeValue("", 6784, activity.Root.State, 6784, 20, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">");
#line 185 "LogPage.cshtml"
                                                                                            Write(activity.Root.State);

#line default
#line hidden
            WriteLiteral("</a></td>\r\n");
#line 186 "LogPage.cshtml"
                        }
                        else
                        {

#line default
#line hidden

            WriteLiteral("                            <td colspan=\"4\"><a");
            BeginWriteAttribute("href", " href=\"", 6967, "\"", 6987, 1);
#line 189 "LogPage.cshtml"
WriteAttributeValue("", 6974, activityPath, 6974, 13, false);

#line default
#line hidden
            EndWriteAttribute();
            WriteLiteral(">Non-scope Log</a></td>\r\n");
#line 190 "LogPage.cshtml"
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
#line 204 "LogPage.cshtml"
                            

#line default
#line hidden

#line 204 "LogPage.cshtml"
                              
                                var counts = new Dictionary<string, int>();
                                counts["Critical"] = 0;
                                counts["Error"] = 0;
                                counts["Warning"] = 0;                                
                                counts["Information"] = 0;
                                counts["Debug"] = 0;
                            

#line default
#line hidden

            WriteLiteral("                            <tbody class=\"logBody\">\r\n");
#line 213 "LogPage.cshtml"
                                

#line default
#line hidden

#line 213 "LogPage.cshtml"
                                 if (!activity.RepresentsScope)
                                {
                                    // message not within a scope
                                    var logInfo = activity.Root.Messages.FirstOrDefault();
                                    

#line default
#line hidden

#line 217 "LogPage.cshtml"
                               Write(LogRow(logInfo, 0));

#line default
#line hidden
#line 217 "LogPage.cshtml"
                                                       
                                    counts[logInfo.Severity.ToString()] = 1;
                                }
                                else
                                {
                                    

#line default
#line hidden

#line 222 "LogPage.cshtml"
                               Write(Traverse(activity.Root, 0, counts));

#line default
#line hidden
#line 222 "LogPage.cshtml"
                                                                       
                                }

#line default
#line hidden

            WriteLiteral("                            </tbody>\r\n                            <tbody class=\"summary\">\r\n                                <tr class=\"logRow\">\r\n                                    <td>");
#line 227 "LogPage.cshtml"
                                   Write(activity.Time.ToString("MM-dd-yyyy HH:mm:ss"));

#line default
#line hidden
            WriteLiteral("</td>\r\n");
#line 228 "LogPage.cshtml"
                                    

#line default
#line hidden

#line 228 "LogPage.cshtml"
                                     foreach (var kvp in counts)
                                    {
                                        if (string.Equals("Debug", kvp.Key)) {

#line default
#line hidden

            WriteLiteral("                                            <td>");
#line 231 "LogPage.cshtml"
                                           Write(kvp.Value);

#line default
#line hidden
            WriteLiteral(" ");
#line 231 "LogPage.cshtml"
                                                      Write(kvp.Key);

#line default
#line hidden
            WriteLiteral("<span class=\"collapse\">v</span></td>\r\n");
#line 232 "LogPage.cshtml"
                                        }
                                        else
                                        {

#line default
#line hidden

            WriteLiteral("                                            <td>");
#line 235 "LogPage.cshtml"
                                           Write(kvp.Value);

#line default
#line hidden
            WriteLiteral(" ");
#line 235 "LogPage.cshtml"
                                                      Write(kvp.Key);

#line default
#line hidden
            WriteLiteral("</td>\r\n");
#line 236 "LogPage.cshtml"
                                        }
                                    }

#line default
#line hidden

            WriteLiteral("                                </tr>\r\n                            </tbody>\r\n                        </table>\r\n                    </td>\r\n                </tr>\r\n            </tbody>\r\n");
#line 244 "LogPage.cshtml"
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
