using AspNetCoreSdkTests.Templates;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace AspNetCoreSdkTests.Util
{
    public class DotNetContext : TempDir
    {
        private static readonly TimeSpan _sleepBetweenOutputContains = TimeSpan.FromMilliseconds(100);

        private (Process Process, ConcurrentStringBuilder OutputBuilder, ConcurrentStringBuilder ErrorBuilder) _runProcess;
        private (Process Process, ConcurrentStringBuilder OutputBuilder, ConcurrentStringBuilder ErrorBuilder) _execProcess;

        public DotNetContext(Template template) { Template = template; }

        public Template Template { get; }

        public string New()
        {
            return DotNetUtil.New(Template.Name, Path);
        }

        public string Restore(NuGetConfig config)
        {
            return DotNetUtil.Restore(Path, config);
        }

        public string Build()
        {
            return DotNetUtil.Build(Path);
        }

        public (string httpUrl, string httpsUrl) Run()
        {
            _runProcess = DotNetUtil.Run(Path);
            return ScrapeUrls(_runProcess);
        }

        public (string httpUrl, string httpsUrl) Exec()
        {
            _execProcess = DotNetUtil.Exec(Path, Template.Name);
            return ScrapeUrls(_execProcess);
        }

        private (string httpUrl, string httpsUrl) ScrapeUrls(
            (Process Process, ConcurrentStringBuilder OutputBuilder, ConcurrentStringBuilder ErrorBuilder) process)
        {
            // Extract URLs from output
            while (true)
            {
                var output = process.OutputBuilder.ToString();
                if (output.Contains("Application started"))
                {
                    var httpUrl = Regex.Match(output, @"Now listening on: (http:\S*)").Groups[1].Value;
                    var httpsUrl = Regex.Match(output, @"Now listening on: (https:\S*)").Groups[1].Value;
                    return (httpUrl, httpsUrl);
                }
                else if (process.Process.HasExited)
                {
                    var startInfo = process.Process.StartInfo;
                    throw new InvalidOperationException(
                        $"Failed to start process '{startInfo.FileName} {startInfo.Arguments}'" + Environment.NewLine + output);
                }
                else
                {
                    Thread.Sleep(_sleepBetweenOutputContains);
                }
            }
        }

        public string Publish()
        {
            return DotNetUtil.Publish(Path);
        }

        public IEnumerable<string> GetObjFiles()
        {
            return IOUtil.GetFiles(System.IO.Path.Combine(Path, "obj"));
        }

        public IEnumerable<string> GetBinFiles()
        {
            return IOUtil.GetFiles(System.IO.Path.Combine(Path, "bin"));
        }

        public IEnumerable<string> GetPublishFiles()
        {
            return IOUtil.GetFiles(System.IO.Path.Combine(Path, DotNetUtil.PublishOutput));
        }

        public override void Dispose()
        {
            // Must stop processes to release filehandles before calling base.Dispose() which deletes app dir
            Dispose(_runProcess);
            Dispose(_execProcess);

            base.Dispose();
        }

        private static void Dispose((Process Process, ConcurrentStringBuilder OutputBuilder, ConcurrentStringBuilder ErrorBuilder) process)
        {
            if (process.Process != null)
            {
                DotNetUtil.StopProcess(process.Process, process.OutputBuilder, process.ErrorBuilder, throwOnError: false);
                process.Process = null;
            }
        }
    }
}
