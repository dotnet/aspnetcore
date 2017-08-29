using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using ProjectTestRunner.HandlerResults;

namespace ProjectTestRunner.Handlers
{
    public class FindProcessHandler : IHandler
    {
        public static string Handler => "find";

        public string HandlerName => Handler;

        public IHandlerResult Execute(IReadOnlyDictionary<string, string> tokens, IReadOnlyList<IHandlerResult> results, JObject json)
        {
            Stopwatch watch = Stopwatch.StartNew();

            try
            {
                string[] args = json["args"].Values<string>().ToArray();

                for (int i = 0; i < args.Length; ++i)
                {
                    if (tokens.TryGetValue(args[i].Trim('%'), out string val))
                    {
                        args[i] = val;
                    }
                }

                string name = json["name"].ToString();
                Process p = Process.GetProcesses().FirstOrDefault(x =>
                {
                    ProcessStartInfo info = null;

                    try
                    {
                        info = x.StartInfo;
                    }
                    catch
                    {
                        return false;
                    }

                    return args.All(y => info.Arguments.Contains(y));
                });
                return new ExecuteHandlerResult(watch.Elapsed, p != null, p != null ? null : "Unable to find process", p, name);
            }
            finally
            {
                watch.Stop();
            }
        }

        public string Summarize(IReadOnlyDictionary<string, string> tokens, JObject json)
        {
            string[] args = json["args"].Values<string>().ToArray();

            for (int i = 0; i < args.Length; ++i)
            {
                if (tokens.TryGetValue(args[i].Trim('%'), out string val))
                {
                    args[i] = val;
                }
            }

            return $"Find process with args [{string.Join(", ", args)}]";
        }
    }
}
