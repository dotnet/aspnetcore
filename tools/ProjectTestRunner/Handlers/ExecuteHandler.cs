using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using ProjectTestRunner.HandlerResults;
using ProjectTestRunner.Helpers;

namespace ProjectTestRunner.Handlers
{
    public class ExecuteHandler : IHandler
    {
        public static string Handler => "execute";

        public string HandlerName => Handler;

        public IHandlerResult Execute(IReadOnlyDictionary<string, string> tokens, IReadOnlyList<IHandlerResult> results, JObject json)
        {
            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                string args = json["args"]?.ToString() ?? string.Empty;

                foreach (KeyValuePair<string, string> entry in tokens)
                {
                    args = args.Replace($"%{entry.Key}%", entry.Value);
                }

                string command = json["command"].ToString();

                foreach (KeyValuePair<string, string> entry in tokens)
                {
                    command = command.Replace($"%{entry.Key}%", entry.Value);
                }

                ProcessEx p = Proc.Run(command, args);
                string name = json["name"]?.ToString();

                if (json["noExit"]?.Value<bool>() ?? false)
                {
                    if (p.WaitForExit(json["exitTimeout"]?.Value<int>() ?? 1000))
                    {
                        return new ExecuteHandlerResult(watch.Elapsed, false, "Process exited unexpectedly", name: name);
                    }

                    return new ExecuteHandlerResult(watch.Elapsed, true, null, p, name);
                }
                else
                {
                    p.WaitForExit();
                    int expectedExitCode = json["exitCode"]?.Value<int>() ?? 0;
                    bool success = expectedExitCode == p.ExitCode;

                    if (!success)
                    {
                        return new ExecuteHandlerResult(watch.Elapsed, false, $"Process exited with code {p.ExitCode} instead of {expectedExitCode}", name: name);
                    }

                    JArray expectations = json["expectations"]?.Value<JArray>();

                    if(expectations != null)
                    {
                        foreach(JObject expectation in expectations.Children().OfType<JObject>())
                        {
                            string assertion = expectation["assertion"]?.Value<string>()?.ToUpperInvariant();
                            string s;
                            StringComparison c;

                            switch (assertion)
                            {
                                case "OUTPUT_CONTAINS":
                                    s = expectation["text"]?.Value<string>();
                                    if(!Enum.TryParse(expectation["comparison"]?.Value<string>() ?? "OrdinalIgnoreCase", out c))
                                    {
                                        c = StringComparison.OrdinalIgnoreCase;
                                    }

                                    if(p.Output.IndexOf(s, c) < 0)
                                    {
                                        return new ExecuteHandlerResult(watch.Elapsed, false, $"Expected output to contain \"{s}\" ({c}), but it did not", name: name);
                                    }

                                    break;
                                case "OUTPUT_DOES_NOT_CONTAIN":
                                    s = expectation["text"]?.Value<string>();
                                    if (!Enum.TryParse(expectation["comparison"]?.Value<string>() ?? "OrdinalIgnoreCase", out c))
                                    {
                                        c = StringComparison.OrdinalIgnoreCase;
                                    }

                                    if (p.Output.IndexOf(s, c) > -1)
                                    {
                                        return new ExecuteHandlerResult(watch.Elapsed, false, $"Expected output to NOT contain \"{s}\" ({c}), but it did", name: name);
                                    }

                                    break;
                                default:
                                    return new ExecuteHandlerResult(watch.Elapsed, false, $"Unkown assertion: {assertion}", name: name);
                            }
                        }
                    }

                    return new ExecuteHandlerResult(watch.Elapsed, true, null, name: name);
                }
            }
            finally
            {
                watch.Stop();
            }
        }

        public string Summarize(IReadOnlyDictionary<string, string> tokens, JObject json)
        {
            string args = json["args"]?.ToString() ?? string.Empty;

            foreach (KeyValuePair<string, string> entry in tokens)
            {
                args = args.Replace($"%{entry.Key}%", entry.Value);
            }

            string command = json["command"].ToString();

            foreach (KeyValuePair<string, string> entry in tokens)
            {
                command = command.Replace($"%{entry.Key}%", entry.Value);
            }

            return $"Execute {command} {args}";
        }
    }
}
