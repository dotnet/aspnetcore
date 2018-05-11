using AspNetCoreSdkTests.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;

namespace AspNetCoreSdkTests.Templates
{
    public abstract class Template
    {
        private static readonly TimeSpan _sleepBetweenHttpRequests = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan _sleepBetweenOutputContains = TimeSpan.FromMilliseconds(100);

        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler()
        {
            // Allow self-signed certs
            ServerCertificateCustomValidationCallback = (m, c, ch, p) => true
        });

        private static ConcurrentDictionary<(Type Type, NuGetPackageSource NuGetPackageSource, RuntimeIdentifier RuntimeIdentifier), Template> _templates =
            new ConcurrentDictionary<(Type Type, NuGetPackageSource NuGetPackageSource, RuntimeIdentifier RuntimeIdentifier), Template>();

        public static T GetInstance<T>(NuGetPackageSource nuGetPackageSource, RuntimeIdentifier runtimeIdentifier) where T : Template, new()
        {
            return (T)_templates.GetOrAdd((typeof(T), nuGetPackageSource, runtimeIdentifier),
                (k) => new T() { NuGetPackageSource = nuGetPackageSource, RuntimeIdentifier = runtimeIdentifier });
        }

        private Lazy<IEnumerable<string>> _objFilesAfterRestore;
        private Lazy<(IEnumerable<string> ObjFiles, IEnumerable<string> BinFiles)> _filesAfterBuild;
        private Lazy<IEnumerable<string>> _filesAfterPublish;
        private Lazy<(HttpResponseMessage Http, HttpResponseMessage Https, string ServerOutput, string ServerError )> _httpResponsesAfterRun;
        private Lazy<(HttpResponseMessage Http, HttpResponseMessage Https, string ServerOutput, string ServerError)> _httpResponsesAfterExec;

        public NuGetPackageSource NuGetPackageSource { get; private set; }
        public RuntimeIdentifier RuntimeIdentifier { get; private set; }

        protected Template()
        {
            _objFilesAfterRestore = new Lazy<IEnumerable<string>>(
                GetObjFilesAfterRestore, LazyThreadSafetyMode.ExecutionAndPublication);

            _filesAfterBuild = new Lazy<(IEnumerable<string> ObjFiles, IEnumerable<string> BinFiles)>(
                GetFilesAfterBuild, LazyThreadSafetyMode.ExecutionAndPublication);

            _filesAfterPublish = new Lazy<IEnumerable<string>>(
                GetFilesAfterPublish, LazyThreadSafetyMode.ExecutionAndPublication);

            _httpResponsesAfterRun = new Lazy<(HttpResponseMessage Http, HttpResponseMessage Https, string ServerOutput, string ServerError)>(
                GetHttpResponsesAfterRun, LazyThreadSafetyMode.ExecutionAndPublication);

            _httpResponsesAfterExec = new Lazy<(HttpResponseMessage Http, HttpResponseMessage Https, string ServerOutput, string ServerError)>(
                GetHttpResponsesAfterExec, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public override string ToString() => $"{Name}, source: {NuGetPackageSource}, rid: {RuntimeIdentifier}";

        private string TempDir => Path.Combine(AssemblySetUp.TempDir, Name, NuGetPackageSource.Name, RuntimeIdentifier.Name );

        public abstract string Name { get; }
        public abstract string OutputPath { get; }
        public abstract TemplateType Type { get; }
        public virtual string RelativeUrl => string.Empty;

        public IEnumerable<string> ObjFilesAfterRestore => _objFilesAfterRestore.Value;
        public IEnumerable<string> ObjFilesAfterBuild => _filesAfterBuild.Value.ObjFiles;
        public IEnumerable<string> BinFilesAfterBuild => _filesAfterBuild.Value.BinFiles;
        public virtual IEnumerable<string> FilesAfterPublish => _filesAfterPublish.Value;
        public HttpResponseMessage HttpResponseAfterRun => _httpResponsesAfterRun.Value.Http;
        public HttpResponseMessage HttpsResponseAfterRun => _httpResponsesAfterRun.Value.Https;
        public string ServerOutputAfterRun => _httpResponsesAfterRun.Value.ServerOutput;
        public string ServerErrorAfterRun => _httpResponsesAfterRun.Value.ServerError;
        public HttpResponseMessage HttpResponseAfterExec => _httpResponsesAfterExec.Value.Http;
        public HttpResponseMessage HttpsResponseAfterExec => _httpResponsesAfterExec.Value.Https;
        public string ServerOutputAfterExec => _httpResponsesAfterExec.Value.ServerOutput;
        public string ServerErrorAfterExec => _httpResponsesAfterExec.Value.ServerError;

        public virtual IEnumerable<string> ExpectedObjFilesAfterRestore => new[]
        {
            $"{Name}.csproj.nuget.cache",
            $"{Name}.csproj.nuget.g.props",
            $"{Name}.csproj.nuget.g.targets",
            "project.assets.json",
        };

        public virtual IEnumerable<string> ExpectedObjFilesAfterBuild => ExpectedObjFilesAfterRestore;

        public abstract IEnumerable<string> ExpectedBinFilesAfterBuild { get; }

        public abstract IEnumerable<string> ExpectedFilesAfterPublish { get; }

        private IEnumerable<string> GetObjFilesAfterRestore()
        {
            Directory.CreateDirectory(TempDir);
            DotNetUtil.New(Name, TempDir);
            DotNetUtil.Restore(TempDir, NuGetPackageSource, RuntimeIdentifier);
            return IOUtil.GetFiles(Path.Combine(TempDir, "obj"));
        }

        private (IEnumerable<string> ObjFiles, IEnumerable<string> BinFiles) GetFilesAfterBuild()
        {
            // Build depends on Restore
            _ = ObjFilesAfterRestore;

            DotNetUtil.Build(TempDir, RuntimeIdentifier);
            return (IOUtil.GetFiles(Path.Combine(TempDir, "obj")), IOUtil.GetFiles(Path.Combine(TempDir, "bin")));
        }

        private IEnumerable<string> GetFilesAfterPublish()
        {
            // Publish depends on Build
            _ = BinFilesAfterBuild;

            DotNetUtil.Publish(TempDir, RuntimeIdentifier);
            return IOUtil.GetFiles(Path.Combine(TempDir, DotNetUtil.PublishOutput));
        }

        private (HttpResponseMessage Http, HttpResponseMessage Https, string ServerOutput, string ServerError) GetHttpResponsesAfterRun()
        {
            // Run depends on Build
            _ = BinFilesAfterBuild;

            return GetHttpResponses(DotNetUtil.Run(TempDir, RuntimeIdentifier));
        }

        private (HttpResponseMessage Http, HttpResponseMessage Https, string ServerOutput, string ServerError) GetHttpResponsesAfterExec()
        {
            // Exec depends on Publish
            _ = FilesAfterPublish;

            return GetHttpResponses(DotNetUtil.Exec(TempDir, Name, RuntimeIdentifier));
        }

        private (HttpResponseMessage Http, HttpResponseMessage Https, string ServerOutput, string ServerError) GetHttpResponses(
            (Process Process, ConcurrentStringBuilder OutputBuilder, ConcurrentStringBuilder ErrorBuilder) process)
        {
            try
            {
                var (httpUrl, httpsUrl) = ScrapeUrls(process);
                return (
                    Get(new Uri(new Uri(httpUrl), RelativeUrl)),
                    Get(new Uri(new Uri(httpsUrl), RelativeUrl)),
                    process.OutputBuilder.ToString(),
                    process.ErrorBuilder.ToString()
                    );
            }
            finally
            {
                DotNetUtil.StopProcess(process, throwOnError: false);
            }
        }

        private (string HttpUrl, string HttpsUrl) ScrapeUrls(
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

        private HttpResponseMessage Get(Uri requestUri)
        {
            while (true)
            {
                try
                {
                    return _httpClient.GetAsync(requestUri).Result;
                }
                catch
                {
                    Thread.Sleep(_sleepBetweenHttpRequests);
                }
            }
        }
    }
}
