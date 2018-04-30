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

        private (Process Process, ConcurrentStringBuilder OutputBuilder, ConcurrentStringBuilder ErrorBuilder) _process;

        public string New(Template template)
        {
            return DotNetUtil.New(template.Name, Path);
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
            _process = DotNetUtil.Run(Path);

            // Extract URLs from output
            while (true)
            {
                var output = _process.OutputBuilder.ToString();
                if (output.Contains("Application started"))
                {
                    var httpUrl = Regex.Match(output, @"Now listening on: (http:\S*)").Groups[1].Value;
                    var httpsUrl = Regex.Match(output, @"Now listening on: (https:\S*)").Groups[1].Value;
                    return (httpUrl, httpsUrl);
                }
                else
                {
                    Thread.Sleep(_sleepBetweenOutputContains);
                }
            }
        }

        public IEnumerable<string> GetObjFiles()
        {
            return IOUtil.GetFiles(System.IO.Path.Combine(Path, "obj"));
        }

        public IEnumerable<string> GetBinFiles()
        {
            return IOUtil.GetFiles(System.IO.Path.Combine(Path, "bin"));
        }

        public override void Dispose()
        {
            // Must stop process to release filehandles before calling base.Dispose() which deletes app dir
            if (_process.Process != null)
            {
                DotNetUtil.StopProcess(_process.Process, _process.OutputBuilder, _process.ErrorBuilder, throwOnError: false);
                _process.Process = null;
            }

            base.Dispose();
        }
    }
}
