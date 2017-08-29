using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DependencyUpdater
{
    class Program
    {
        static int Main(string[] args)
        {
            if(args.Length < 2)
            {
                Console.WriteLine(@"Usage:
dotnet DependencyUpdater.dll [path to templates:string] [infer no-timestamp packages from semaphore files] [[paths to package sources]]");
                return -1;
            }

            bool NoTimestamp = bool.Parse(args[1]);
            string root = args[0];
            List<string> sources = new List<string>();

            foreach(string arg in args.Skip(2))
            {
                if (Directory.Exists(arg))
                {
                    sources.Add(arg);
                }
                else if (File.Exists(arg))
                {
                    string sem = File.ReadAllText(arg);

                    foreach (string line in sem.Split('\n'))
                    {
                        string trimmed = line.Trim();
                        string share = trimmed.Substring(trimmed.LastIndexOf(':') + 1).Trim();
                        string source = Path.Combine(share, @"Signed\Packages") + (NoTimestamp ? "-NoTimeStamp" : "");

                        if (Directory.Exists(source))
                        {
                            sources.Add(source);
                        }
                    }
                }
                else
                {
                    Console.Error.WriteLine($"Unknown source: \"{arg}\"");
                }
            }

            Regex versionPattern = new Regex(@"\d+\.\d+\.\d+([-\+].*)?");
            List<Tuple<string, Regex, string>> replacements = new List<Tuple<string, Regex, string>>();

            foreach (string source in sources)
            {
                foreach (string file in Directory.EnumerateFiles(source, "*.nupkg", SearchOption.AllDirectories))
                {
                    string packageIdAndVersion = Path.GetFileNameWithoutExtension(file);
                    Match m = versionPattern.Match(packageIdAndVersion);
                    string packageId = packageIdAndVersion.Substring(0, m.Index).TrimEnd('.');
                    string version = packageIdAndVersion.Substring(m.Index);
                    int versionFirstDot = version.IndexOf('.');
                    int versionSecondDot = version.IndexOf('.', versionFirstDot + 1);
                    string majorMinor = version.Substring(0, versionSecondDot);
                    string majorMinorEscaped = Regex.Escape(majorMinor);

                    Regex rx = new Regex($"(?<=\"{packageId}\"\\s+Version\\s*=\\s*\"){majorMinorEscaped}[^\"]*(?=\")");
                    replacements.Add(Tuple.Create(packageId, rx, version));
                }
            }

            foreach(string project in Directory.EnumerateFiles(root, "*.*proj", SearchOption.AllDirectories))
            {
                if(Path.GetExtension(project).ToUpperInvariant() == ".PROJ" || Path.GetFileName(project).ToUpperInvariant() == "BUILD.CSPROJ")
                {
                    continue;
                }

                string source = File.ReadAllText(project);
                string result = source;
                foreach(Tuple<string, Regex, string> replacement in replacements)
                {
                    string orig = result;
                    result = replacement.Item2.Replace(result, replacement.Item3);

                    if(orig != result)
                    {
                        Console.WriteLine($"{project.Substring(root.Length).TrimStart('\\', '/')}: {replacement.Item1} -> {replacement.Item3}");
                    }
                }

                byte[] data = File.ReadAllBytes(project);
                Encoding encoding = EncodingUtil.Detect(data, data.Length, out byte[] bom);

                if(bom.Length == 0 && Encoding.UTF8.EncodingName == encoding.EncodingName)
                {
                    encoding = new UTF8Encoding(false);
                }

                File.WriteAllText(project, result, encoding);
            }

            return 0;
        }
    }
}
