using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;
using ProjectTestRunner.HandlerResults;

namespace ProjectTestRunner.Handlers
{
    //
    // directory: rel path to directory
    // assertion: exists | does_not_exist

    public class DirectoryInspectHandler : IHandler
    {
        public static string Handler => "directoryInspect";

        public string HandlerName => Handler;

        public IHandlerResult Execute(IReadOnlyDictionary<string, string> tokens, IReadOnlyList<IHandlerResult> results, JObject json)
        {
            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                string basePath = tokens["targetPath"];

                string name = json["name"]?.ToString();
                string directoryName = json["directory"].ToString();
                string pathToDirectory = Path.Combine(basePath, directoryName);
                string assertion = json["assertion"].ToString();
                bool doesDirectoryExist = Directory.Exists(pathToDirectory);

                if (string.Equals(assertion, "exists", StringComparison.OrdinalIgnoreCase))
                {
                    if (!doesDirectoryExist)
                    {
                        return new ExecuteHandlerResult(watch.Elapsed, false, $"Expected directory {directoryName} to exist, but it did not", name: name);
                    }
                }
                else if (string.Equals(assertion, "does_not_exist", StringComparison.OrdinalIgnoreCase))
                {
                    if (doesDirectoryExist)
                    {
                        return new ExecuteHandlerResult(watch.Elapsed, false, $"Expected directory {directoryName} to not exist, but it did", name: name);
                    }
                }

                return new GenericHandlerResult(watch.Elapsed, true, null);
            }
            catch (Exception ex)
            {
                return new GenericHandlerResult(watch.Elapsed, false, ex.Message);
            }
        }

        public string Summarize(IReadOnlyDictionary<string, string> tokens, JObject json)
        {
            string directoryName = json["directory"].ToString();
            string assertion = json["assertion"].ToString().ToLowerInvariant().Replace("_", " ");

            return $"Directory inspection - checking if directory \"{directoryName}\" {assertion}";
        }
    }
}
