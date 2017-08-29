using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using ProjectTestRunner.HandlerResults;

namespace ProjectTestRunner.Handlers
{
    // file: rel path to file
    //      Note: For filenames that get modified by the template creation name, use the literal "%targetPathName%".
    //              It gets replaced with the creation name.
    //          Example: "filename": "%targetPathName%.csproj"
    // expectations:
    //      assertion: exists | does_not_exist | contains | does_not_contain
    //      text: text to check for when using (file_contains | file_does_not_contain)
    //      comparison: a StringComparison enum value

    // Example:
    // {
    //    "handler": "fileInspect",
    //    "file": "filename"
    //    "expectations": [
    //      {
    //        "assertion": "exists"
    //      },
    //      {
    //        "assertion": "contains",
    //        "text": "netcoreapp1.0",
    //        "comparison": "Ordinal",
    //      },
    //      {
    //        "assertion": "does_not_contain",
    //        "text": "TargetFrameworkOverride",
    //      }

    public class FileInspectHandler : IHandler
    {
        public static string Handler => "fileInspect";

        public string HandlerName => Handler;

        public IHandlerResult Execute(IReadOnlyDictionary<string, string> tokens, IReadOnlyList<IHandlerResult> results, JObject json)
        {
            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                string basePath = tokens["targetPath"];
                string outputName = tokens["targetPathName"];

                string name = json["name"]?.ToString();
                string filename = json["file"].ToString();

                foreach (KeyValuePair<string, string> entry in tokens)
                {
                    filename = filename.Replace($"%{entry.Key}%", entry.Value);
                }

                string pathToFile = Path.Combine(basePath, filename);
                bool doesFileExist = File.Exists(pathToFile);

                string fileContent = null;

                JArray expectations = json["expectations"]?.Value<JArray>();

                if (expectations != null)
                {
                    foreach (JObject expectation in expectations.Children().OfType<JObject>())
                    {
                        string assertion = expectation["assertion"]?.Value<string>()?.ToUpperInvariant();
                        string text;
                        StringComparison comparison;

                        switch (assertion)
                        {
                            case "EXISTS":
                                if (!doesFileExist)
                                {
                                    return new ExecuteHandlerResult(watch.Elapsed, false, $"Expected file \"{filename}\" to exist, but it did not", name: name);
                                }
                                break;
                            case "DOES_NOT_EXIST":
                                if (doesFileExist)
                                {
                                    return new ExecuteHandlerResult(watch.Elapsed, false, $"Expected file \"{filename}\" to not exist, but it did", name: name);
                                }
                                break;
                            case "CONTAINS":
                                text = expectation["text"]?.Value<string>();
                                if (!Enum.TryParse(expectation["comparison"]?.Value<string>() ?? "OrdinalIgnoreCase", out comparison))
                                {
                                    comparison = StringComparison.OrdinalIgnoreCase;
                                }

                                if (!doesFileExist)
                                {
                                    return new ExecuteHandlerResult(watch.Elapsed, false, $"Expected file \"{filename}\" to contain \"{text}\" ({comparison}), but file did not exist", name: name);
                                }

                                if (fileContent == null)
                                {
                                    fileContent = File.ReadAllText(pathToFile);
                                }

                                if (fileContent.IndexOf(text, comparison) < 0)
                                {
                                    return new ExecuteHandlerResult(watch.Elapsed, false, $"Expected file \"{filename}\" to contain \"{text}\" ({comparison}), but it did not", name: name);
                                }

                                break;
                            case "DOES_NOT_CONTAIN":
                                text = expectation["text"].Value<string>();
                                if (!Enum.TryParse(expectation["comparison"]?.Value<string>() ?? "OrdinalIgnoreCase", out comparison))
                                {
                                    comparison = StringComparison.OrdinalIgnoreCase;
                                }

                                if (!doesFileExist)
                                {
                                    return new ExecuteHandlerResult(watch.Elapsed, false, $"Expected file \"{filename}\" to not contain \"{text}\" ({comparison}), but file did not exist", name: name);
                                }

                                if (fileContent == null)
                                {
                                    fileContent = File.ReadAllText(pathToFile);
                                }

                                if (fileContent.IndexOf(text, comparison) >= 0)
                                {
                                    return new ExecuteHandlerResult(watch.Elapsed, false, $"Expected file \"{filename}\" to not contain \"{text}\" ({comparison}), but it did", name: name);
                                }

                                break;
                        }

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
            string filename = json["file"].ToString();

            return $"File Inspection - inspecting file = \"{filename}\"";
        }
    }
}
