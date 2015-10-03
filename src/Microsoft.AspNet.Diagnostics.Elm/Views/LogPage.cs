namespace Microsoft.AspNet.Diagnostics.Elm.Views
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
using Microsoft.AspNet.Diagnostics.Elm.Views

#line default
#line hidden
    ;
#line 6 "LogPage.cshtml"
using Microsoft.AspNet.Diagnostics.Elm

#line default
#line hidden
    ;
#line 7 "LogPage.cshtml"
using Microsoft.AspNet.Diagnostics.Views

#line default
#line hidden
    ;
#line 8 "LogPage.cshtml"
using Microsoft.Extensions.Logging

#line default
#line hidden
    ;
    using System.Threading.Tasks;

    public class LogPage : Microsoft.AspNet.Diagnostics.Views.BaseView
    {
public  HelperResult 
#line 21 "LogPage.cshtml"
LogRow(LogInfo log, int level) {

#line default
#line hidden
        return new HelperResult((__razor_helper_writer) => {
#line 21 "LogPage.cshtml"
                                        
    if (log.Severity >= Model.Options.MinLevel && 
        (string.IsNullOrEmpty(Model.Options.NamePrefix) || log.Name.StartsWith(Model.Options.NamePrefix, StringComparison.Ordinal)))
    {

#line default
#line hidden

            WriteLiteralTo(__razor_helper_writer, "        <tr class=\"logRow\">\r\n            <td>");
#line 26 "LogPage.cshtml"
WriteTo(__razor_helper_writer, string.Format("{0:MM/dd/yy}", log.Time));

#line default
#line hidden
            WriteLiteralTo(__razor_helper_writer, "</td>\r\n            <td>");
#line 27 "LogPage.cshtml"
WriteTo(__razor_helper_writer, string.Format("{0:H:mm:ss}", log.Time));

#line default
#line hidden
            WriteLiteralTo(__razor_helper_writer, "</td>\r\n            <td");
            WriteAttributeTo(__razor_helper_writer, "title", Tuple.Create(" title=\"", 871), Tuple.Create("\"", 888), 
            Tuple.Create(Tuple.Create("", 879), Tuple.Create<System.Object, System.Int32>(log.Name, 879), false));
            WriteLiteralTo(__razor_helper_writer, ">");
#line 28 "LogPage.cshtml"
    WriteTo(__razor_helper_writer, log.Name);

#line default
#line hidden
            WriteLiteralTo(__razor_helper_writer, "</td>\r\n            <td");
            WriteAttributeTo(__razor_helper_writer, "class", Tuple.Create(" class=\"", 921), Tuple.Create("\"", 972), 
            Tuple.Create(Tuple.Create("", 929), Tuple.Create<System.Object, System.Int32>(log.Severity.ToString().ToLowerInvariant(), 929), false));
            WriteLiteralTo(__razor_helper_writer, ">");
#line 29 "LogPage.cshtml"
                                      WriteTo(__razor_helper_writer, log.Severity);

#line default
#line hidden
            WriteLiteralTo(__razor_helper_writer, "</td>\r\n            <td");
            WriteAttributeTo(__razor_helper_writer, "title", Tuple.Create(" title=\"", 1009), Tuple.Create("\"", 1029), 
            Tuple.Create(Tuple.Create("", 1017), Tuple.Create<System.Object, System.Int32>(log.Message, 1017), false));
            WriteLiteralTo(__razor_helper_writer, ">\r\n");
#line 31 "LogPage.cshtml"
                

#line default
#line hidden

#line 31 "LogPage.cshtml"
                 for (var i = 0; i < level; i++)
                {

#line default
#line hidden

            WriteLiteralTo(__razor_helper_writer, "                    <span class=\"tab\"></span>\r\n");
#line 34 "LogPage.cshtml"
                }

#line default
#line hidden

            WriteLiteralTo(__razor_helper_writer, "                ");
#line 35 "LogPage.cshtml"
WriteTo(__razor_helper_writer, log.Message);

#line default
#line hidden
            WriteLiteralTo(__razor_helper_writer, "\r\n            </td>\r\n            <td");
            WriteAttributeTo(__razor_helper_writer, "title", Tuple.Create(" title=\"", 1232), Tuple.Create("\"", 1254), 
            Tuple.Create(Tuple.Create("", 1240), Tuple.Create<System.Object, System.Int32>(log.Exception, 1240), false));
            WriteLiteralTo(__razor_helper_writer, ">");
#line 37 "LogPage.cshtml"
         WriteTo(__razor_helper_writer, log.Exception);

#line default
#line hidden
            WriteLiteralTo(__razor_helper_writer, "</td>\r\n        </tr>\r\n");
#line 39 "LogPage.cshtml"
    }

#line default
#line hidden

        }
        );
#line 40 "LogPage.cshtml"
}

#line default
#line hidden

public  HelperResult 
#line 42 "LogPage.cshtml"
Traverse(ScopeNode node, int level, Dictionary<string, int> counts)
{

#line default
#line hidden
        return new HelperResult((__razor_helper_writer) => {
#line 43 "LogPage.cshtml"
 
    // print start of scope
    

#line default
#line hidden

#line 45 "LogPage.cshtml"
WriteTo(__razor_helper_writer, LogRow(new LogInfo()
    {
        Name = node.Name,
        Time = node.StartTime,
        Severity = LogLevel.Verbose,
        Message = "Beginning " + node.State,
    }, level));

#line default
#line hidden
#line 51 "LogPage.cshtml"
             ;
    var messageIndex = 0;
    var childIndex = 0;
    while (messageIndex < node.Messages.Count && childIndex < node.Children.Count)
    {
        if (node.Messages[messageIndex].Time < node.Children[childIndex].StartTime)
        {
            

#line default
#line hidden

#line 58 "LogPage.cshtml"
WriteTo(__razor_helper_writer, LogRow(node.Messages[messageIndex], level));

#line default
#line hidden
#line 58 "LogPage.cshtml"
                                                       
            counts[node.Messages[messageIndex].Severity.ToString()]++;
            messageIndex++;
        }
        else
        {
            

#line default
#line hidden

#line 64 "LogPage.cshtml"
WriteTo(__razor_helper_writer, Traverse(node.Children[childIndex], level + 1, counts));

#line default
#line hidden
#line 64 "LogPage.cshtml"
                                                                   
            childIndex++;
        }
    }
    if (messageIndex < node.Messages.Count)
    {
        for (var i = messageIndex; i < node.Messages.Count; i++)
        {
            

#line default
#line hidden

#line 72 "LogPage.cshtml"
WriteTo(__razor_helper_writer, LogRow(node.Messages[i], level));

#line default
#line hidden
#line 72 "LogPage.cshtml"
                                            
            counts[node.Messages[i].Severity.ToString()]++;
        }
    }
    else
    {
        for (var i = childIndex; i < node.Children.Count; i++)
        {
            

#line default
#line hidden

#line 80 "LogPage.cshtml"
WriteTo(__razor_helper_writer, Traverse(node.Children[i], level + 1, counts));

#line default
#line hidden
#line 80 "LogPage.cshtml"
                                                          
        }
    }
    // print end of scope
    

#line default
#line hidden

#line 84 "LogPage.cshtml"
WriteTo(__razor_helper_writer, LogRow(new LogInfo()
    {
        Name = node.Name,
        Time = node.EndTime,
        Severity = LogLevel.Verbose,
        Message = string.Format("Completed {0} in {1}ms", node.State, node.EndTime - node.StartTime)
    }, level));

#line default
#line hidden
#line 90 "LogPage.cshtml"
             ;

#line default
#line hidden

        }
        );
#line 91 "LogPage.cshtml"
}

#line default
#line hidden

#line 11 "LogPage.cshtml"

    public LogPage(LogPageModel model)
    {
        Model = model;
    }

    public LogPageModel Model { get; set; }

#line default
#line hidden
        #line hidden
        public LogPage()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            Response.ContentType = "text/html; charset=utf-8";
            WriteLiteral("\r\n");
            WriteLiteral("\r\n\r\n");
            WriteLiteral("\r\n");
            WriteLiteral(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>ASP.NET Logs</title>
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
    border-bottom: 1px solid lightgray;
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

.information {
    color: blue;
}

.verbose {
    color: black;
}

.warning {
    color: orange;
}
    </style>
</head>
<body>
    <h1>ASP.NET Logs</h1>
    <form id=""viewOptions"" method=""get"">
        <select name=""level"">
");
#line 108 "LogPage.cshtml"
            

#line default
#line hidden

#line 108 "LogPage.cshtml"
             foreach (var severity in Enum.GetValues(typeof(LogLevel)))
            {
                var severityInt = (int)severity;
                if ((int)Model.Options.MinLevel == severityInt)
                {

#line default
#line hidden

            WriteLiteral("                    <option");
            WriteAttribute("value", Tuple.Create(" value=\"", 3500), Tuple.Create("\"", 3520), 
            Tuple.Create(Tuple.Create("", 3508), Tuple.Create<System.Object, System.Int32>(severityInt, 3508), false));
            WriteLiteral(" selected=\"selected\">");
#line 113 "LogPage.cshtml"
                                                                Write(severity);

#line default
#line hidden
            WriteLiteral("</option>\r\n");
#line 114 "LogPage.cshtml"
                }
                else
                {

#line default
#line hidden

            WriteLiteral("                    <option");
            WriteAttribute("value", Tuple.Create(" value=\"", 3649), Tuple.Create("\"", 3669), 
            Tuple.Create(Tuple.Create("", 3657), Tuple.Create<System.Object, System.Int32>(severityInt, 3657), false));
            WriteLiteral(">");
#line 117 "LogPage.cshtml"
                                            Write(severity);

#line default
#line hidden
            WriteLiteral("</option>\r\n");
#line 118 "LogPage.cshtml"
                }
            }

#line default
#line hidden

            WriteLiteral("        </select>\r\n        <input type=\"text\" name=\"name\"");
            WriteAttribute("value", Tuple.Create(" value=\"", 3782), Tuple.Create("\"", 3815), 
            Tuple.Create(Tuple.Create("", 3790), Tuple.Create<System.Object, System.Int32>(Model.Options.NamePrefix, 3790), false));
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
#line 145 "LogPage.cshtml"
        

#line default
#line hidden

#line 145 "LogPage.cshtml"
         foreach (var activity in Model.Activities.Reverse())
        {

#line default
#line hidden

            WriteLiteral("            <tbody>\r\n                <tr class=\"requestRow\">\r\n");
#line 149 "LogPage.cshtml"
                    

#line default
#line hidden

#line 149 "LogPage.cshtml"
                      
                        var activityPath = Model.Path.Value + "/" + activity.Id;
                        if (activity.HttpInfo != null)
                        {

#line default
#line hidden

            WriteLiteral("                        \t<td><a");
            WriteAttribute("href", Tuple.Create(" href=\"", 4879), Tuple.Create("\"", 4899), 
            Tuple.Create(Tuple.Create("", 4886), Tuple.Create<System.Object, System.Int32>(activityPath, 4886), false));
            WriteAttribute("title", Tuple.Create(" title=\"", 4900), Tuple.Create("\"", 4931), 
            Tuple.Create(Tuple.Create("", 4908), Tuple.Create<System.Object, System.Int32>(activity.HttpInfo.Path, 4908), false));
            WriteLiteral(">");
#line 153 "LogPage.cshtml"
                                                                                   Write(activity.HttpInfo.Path);

#line default
#line hidden
            WriteLiteral("</a></td>\r\n                            <td>");
#line 154 "LogPage.cshtml"
                           Write(activity.HttpInfo.Method);

#line default
#line hidden
            WriteLiteral("</td>\r\n                            <td>");
#line 155 "LogPage.cshtml"
                           Write(activity.HttpInfo.Host);

#line default
#line hidden
            WriteLiteral("</td>\r\n                            <td>");
#line 156 "LogPage.cshtml"
                           Write(activity.HttpInfo.StatusCode);

#line default
#line hidden
            WriteLiteral("</td>\r\n");
#line 157 "LogPage.cshtml"
                        }
                        else if (activity.RepresentsScope)
                        {

#line default
#line hidden

            WriteLiteral("                            <td colspan=\"4\"><a");
            WriteAttribute("href", Tuple.Create(" href=\"", 5321), Tuple.Create("\"", 5341), 
            Tuple.Create(Tuple.Create("", 5328), Tuple.Create<System.Object, System.Int32>(activityPath, 5328), false));
            WriteAttribute("title", Tuple.Create(" title=\"", 5342), Tuple.Create("\"", 5370), 
            Tuple.Create(Tuple.Create("", 5350), Tuple.Create<System.Object, System.Int32>(activity.Root.State, 5350), false));
            WriteLiteral(">");
#line 160 "LogPage.cshtml"
                                                                                            Write(activity.Root.State);

#line default
#line hidden
            WriteLiteral("</a></td>\r\n");
#line 161 "LogPage.cshtml"
                        }
                        else
                        {

#line default
#line hidden

            WriteLiteral("                            <td colspan=\"4\"><a");
            WriteAttribute("href", Tuple.Create(" href=\"", 5533), Tuple.Create("\"", 5553), 
            Tuple.Create(Tuple.Create("", 5540), Tuple.Create<System.Object, System.Int32>(activityPath, 5540), false));
            WriteLiteral(">Non-scope Log</a></td>\r\n");
#line 165 "LogPage.cshtml"
                        }
                    

#line default
#line hidden

            WriteLiteral(@"
                    <td class=""logTd"">
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
#line 179 "LogPage.cshtml"
                            

#line default
#line hidden

#line 179 "LogPage.cshtml"
                              
                                var counts = new Dictionary<string, int>();
                                counts["Critical"] = 0;
                                counts["Error"] = 0;
                                counts["Warning"] = 0;                                
                                counts["Information"] = 0;
                                counts["Verbose"] = 0;
                            

#line default
#line hidden

            WriteLiteral("\r\n                            <tbody class=\"logBody\">\r\n");
#line 188 "LogPage.cshtml"
                                

#line default
#line hidden

#line 188 "LogPage.cshtml"
                                 if (!activity.RepresentsScope)
                                {
                                    // message not within a scope
                                    var logInfo = activity.Root.Messages.FirstOrDefault();
                                    

#line default
#line hidden

#line 192 "LogPage.cshtml"
                               Write(LogRow(logInfo, 0));

#line default
#line hidden
#line 192 "LogPage.cshtml"
                                                       
                                    counts[logInfo.Severity.ToString()] = 1;
                                }
                                else
                                {
                                    

#line default
#line hidden

#line 197 "LogPage.cshtml"
                               Write(Traverse(activity.Root, 0, counts));

#line default
#line hidden
#line 197 "LogPage.cshtml"
                                                                       
                                }

#line default
#line hidden

            WriteLiteral("                            </tbody>\r\n                            <tbody class=\"s" +
"ummary\">\r\n                                <tr class=\"logRow\">\r\n                 " +
"                   <td>");
#line 202 "LogPage.cshtml"
                                   Write(activity.Time.ToString("MM-dd-yyyy HH:mm:ss"));

#line default
#line hidden
            WriteLiteral("</td>\r\n");
#line 203 "LogPage.cshtml"
                                    

#line default
#line hidden

#line 203 "LogPage.cshtml"
                                     foreach (var kvp in counts)
                                    {
                                        if (string.Equals("Verbose", kvp.Key)) {

#line default
#line hidden

            WriteLiteral("                                            <td>");
#line 206 "LogPage.cshtml"
                                           Write(kvp.Value);

#line default
#line hidden
            WriteLiteral(" ");
#line 206 "LogPage.cshtml"
                                                      Write(kvp.Key);

#line default
#line hidden
            WriteLiteral("<span class=\"collapse\">v</span></td>\r\n");
#line 207 "LogPage.cshtml"
                                        }
                                        else
                                        {

#line default
#line hidden

            WriteLiteral("                                            <td>");
#line 210 "LogPage.cshtml"
                                           Write(kvp.Value);

#line default
#line hidden
            WriteLiteral(" ");
#line 210 "LogPage.cshtml"
                                                      Write(kvp.Key);

#line default
#line hidden
            WriteLiteral("</td>\r\n");
#line 211 "LogPage.cshtml"
                                        }
                                    }

#line default
#line hidden

            WriteLiteral("                                </tr>\r\n                            </tbody>\r\n    " +
"                    </table>\r\n                    </td>\r\n                </tr>\r\n" +
"            </tbody>\r\n");
#line 219 "LogPage.cshtml"
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
