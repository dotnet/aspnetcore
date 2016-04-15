// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PushCoherence
{
    class Program
    {
        public static void Main(string[] args)
        {
            var nugetFeed = Environment.GetEnvironmentVariable("NUGET_FEED");
            var dropRoot = Environment.GetEnvironmentVariable("DROP_ROOT");
            var apiKey = Environment.GetEnvironmentVariable("APIKEY");
            var nugetExe = Environment.GetEnvironmentVariable("PUSH_NUGET_EXE");

            if (string.IsNullOrEmpty(nugetFeed))
            {
                throw new Exception("NUGET_FEED not specified");
            }

            if (string.IsNullOrEmpty(dropRoot))
            {
                throw new Exception("DROP_ROOT not specified");
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("APIKEY not specified");
            }

            if (string.IsNullOrEmpty(nugetExe))
            {
                throw new Exception("PUSH_NUGET_EXE not specified");
            }

            var artifactsDir = Path.Combine(Directory.GetCurrentDirectory(), "artifacts");
            var packagesDir = Path.Combine(artifactsDir, "Signed", "Packages");

            var packagesToPush = new[]
            {
                packagesDir,
                Path.Combine(artifactsDir, "coherence", "noship"),
                Path.Combine(artifactsDir, "coherence", "ext"),
                Path.Combine(artifactsDir, "coherence", "ship-ext"),
            }.SelectMany(d => Directory.EnumerateFiles(d, "*.nupkg"));
            Console.WriteLine("Pushing packages from {0} to feed {1}", packagesDir, nugetFeed);

            Parallel.ForEach(packagesToPush, new ParallelOptions { MaxDegreeOfParallelism = 5 }, package =>
            {
                Retry(() => PushPackage(nugetFeed, apiKey, nugetExe, package));
            });

            var nonTimeStampedDir = Path.Combine(artifactsDir, "Signed", "Packages-NoTimeStamp");
            Directory.CreateDirectory(nonTimeStampedDir);
            foreach (var file in Directory.EnumerateFiles(packagesDir))
            {
                CreateTimeStampFreePackage(nonTimeStampedDir, file);
            }
        }

        private static void CreateTimeStampFreePackage(string outDir, string packagePath)
        {
            var packageInfo = PackageInfo.FromPath(packagePath);
            var targetPath = Path.Combine(outDir, packageInfo.Id + '.' + StripBuildVersion(packageInfo.Version) + ".nupkg");
            Console.WriteLine("Creating timestamp free version at {0}", targetPath);
            File.Copy(packagePath, targetPath);

            using (var fileStream = File.Open(targetPath, FileMode.Open, FileAccess.ReadWrite))
            using (var package = new ZipArchive(fileStream, ZipArchiveMode.Update))
            {
                var manifest = package.Entries.First(f => f.FullName.EndsWith(".nuspec"));

                using (var stream = manifest.Open())
                {
                    var xdoc = XDocument.Load(stream);
                    var ns = xdoc.Root.Name.NamespaceName;

                    var version = xdoc.Descendants(XName.Get("version", ns)).First();
                    version.Value = StripBuildVersion(version.Value);

                    var dependencies = xdoc.Descendants(XName.Get("dependency", ns));
                    foreach (var dependency in dependencies)
                    {
                        var attr = dependency.Attribute("version");
                        attr.Value = StripBuildVersion(attr.Value);
                    }

                    stream.Position = 0;
                    stream.SetLength(0);
                    xdoc.Save(stream);
                }
            }
        }

        private static string StripBuildVersion(string version)
        {
            if (Regex.IsMatch(version, @"(alpha|beta|rc)\d-\d+$"))
            {
                var timeStampFreeVersion = Environment.GetEnvironmentVariable("TIMESTAMP_FREE_VERSION");
                if (string.IsNullOrEmpty(timeStampFreeVersion))
                {
                    timeStampFreeVersion = "final";
                }

                if (!timeStampFreeVersion.StartsWith("-"))
                {
                    timeStampFreeVersion = "-" + timeStampFreeVersion;
                }

                // E.g. change version 2.5.0-rc2-123123 to 2.5.0-rc2-final.
                var index = version.LastIndexOf('-');
                if (index != -1)
                {
                    return version.Substring(0, index) + timeStampFreeVersion;
                }
            }
            else if (Regex.IsMatch(version, @"rtm-\d+$"))
            {
                // E.g. change version 2.5.0-rtm-123123 to 2.5.0.
                var index = version.LastIndexOf('-');
                if (index != -1)
                {
                    return version.Substring(0, index);
                }
            }

            return version;
        }

        private static void Retry(Action pushPackage)
        {
            int attempts = 5;
            while (attempts-- > 0)
            {
                try
                {
                    pushPackage();
                    break;
                }
                catch (Exception)
                {
                    if (attempts == 1)
                    {
                        throw;
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                    Console.WriteLine("Retry ({0})", attempts);
                }
            }
        }

        private static void PushPackage(string nugetFeed, string apiKey, string nugetExe, string packagePath)
        {
            var nugetExeArgs = string.Format(
                CultureInfo.InvariantCulture, "push -Source {0} -ApiKey {1} {2}",
                nugetFeed,
                apiKey,
                packagePath);
            var packageName = Path.GetFileNameWithoutExtension(packagePath);
            Console.WriteLine("Pushing package {0}", packageName);
            var psi = new ProcessStartInfo(nugetExe, nugetExeArgs)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using (var p = Process.Start(psi))
            {
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    var message = string.Format("Pushing package {0} failed. Exit code from nuget.exe: {1}", packageName, p.ExitCode);
                    throw new Exception(message);
                }
            }
            Console.WriteLine("Pushed package {0}", packageName);
        }

        private class PackageInfo
        {
            private static readonly Regex _packageNameRegex = new Regex(@"^(?<id>.+?)\.(?<version>[0-9].*)$");

            public string Id { get; set; }

            public string Version { get; set; }

            public static PackageInfo FromPath(string path)
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                var match = _packageNameRegex.Match(fileName);
                if (!match.Success)
                {
                    throw new InvalidOperationException("Can't parse file " + path);
                }
                return new PackageInfo
                {
                    Id = match.Groups["id"].Value,
                    Version = match.Groups["version"].Value
                };
            }
        }
    }
}
