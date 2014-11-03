// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
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

            var lastUploadedFile = Path.Combine(dropRoot, "uploaded-files");
            var packagesDir = Path.Combine(Directory.GetCurrentDirectory(), "bin", "Signed", "Packages");
            var nugetExe = Path.Combine(Directory.GetCurrentDirectory(), "tools", "nuget.exe");

            var nonTimeStampedDir = Path.Combine(Directory.GetCurrentDirectory(), "bin", "Signed", "Packages-NoTimeStamp");
            Directory.CreateDirectory(nonTimeStampedDir);

            var packages = Directory.EnumerateFiles(packagesDir, "*.nupkg");
            Console.WriteLine("Pushing packages from {0} to feed {1}", packagesDir, nugetFeed);

            using (var fileList = new UploadedFileList(lastUploadedFile))
            {
                while (!fileList.TryRead())
                {
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                }

                Parallel.ForEach(packages, new ParallelOptions { MaxDegreeOfParallelism = 5 }, package =>
                {
                    var packageInfo = PackageInfo.FromPath(package);
                    CreateTimeStampFreePackage(nonTimeStampedDir, packageInfo);

                    PackageInfo existing;
                    if (fileList.Infos.TryGetValue(packageInfo.Id, out existing) &&
                        existing.Equals(packageInfo))
                    {
                        return;
                    }

                    fileList.Infos[packageInfo.Id] = packageInfo;
                    Retry(() => PushPackage(nugetFeed, apiKey, nugetExe, packageInfo));
                    Console.WriteLine("Pushed package {0}", Path.GetFileNameWithoutExtension(packageInfo.Path));
                });
            }
        }

        private static void CreateTimeStampFreePackage(string outDir, PackageInfo packageInfo)
        {
            var targetPath = Path.Combine(outDir, packageInfo.Id + '.' + StripBuildVersion(packageInfo.Version) + ".nupkg");
            Console.WriteLine("Creating timestamp free version at {0}", targetPath);
            File.Copy(packageInfo.Path, targetPath);

            using (var package = Package.Open(targetPath))
            {
                var relationshipType = package.GetRelationshipsByType("http://schemas.microsoft.com/packaging/2010/07/manifest");


                var manifest = package.GetPart(relationshipType.SingleOrDefault().TargetUri);

                using (var stream = manifest.GetStream(FileMode.Open, FileAccess.ReadWrite))
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
            if (Regex.IsMatch(version, @"(alpha|beta)\d-\d+$"))
            {
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
            int attempts = 3;
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

        private static void PushPackage(string nugetFeed, string apiKey, string nugetExe, PackageInfo packageInfo)
        {
            var nugetExeArgs = string.Format(CultureInfo.InvariantCulture, "push -Source {0} -ApiKey {1} {2}",
                                     nugetFeed,
                                     apiKey,
                                     packageInfo.Path);
            Console.WriteLine("Pushing package {0} {1}", packageInfo.Id, packageInfo.Version);
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
                    string message = string.Format("Pushing package {0} failed. Exit code from nuget.exe: {1}",
                                                   Path.GetFileNameWithoutExtension(packageInfo.Path),
                                                   p.ExitCode);
                    throw new Exception(message);
                }
            }
        }

        private sealed class UploadedFileList : IDisposable
        {
            private readonly string _path;
            private Stream _fileStream;

            public UploadedFileList(string path)
            {
                _path = path;
            }

            public IDictionary<string, PackageInfo> Infos { get; private set; }

            public bool TryRead()
            {
                try
                {
                    _fileStream = File.Open(_path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    ReadContents();
                    return true;
                }
                catch (IOException)
                {
                    // The file must be locked by another concurrent CI run.
                }
                return false;
            }

            private void ReadContents()
            {
                Infos = new ConcurrentDictionary<string, PackageInfo>(StringComparer.OrdinalIgnoreCase);
                var reader = new StreamReader(_fileStream);
                var contents = reader.ReadToEnd();
                foreach (var line in contents.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var tabs = line.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    if (tabs.Length == 3)
                    {
                        Infos.Add(tabs[0], new PackageInfo { Id = tabs[0], Version = tabs[1], Hash = tabs[2] });
                    }
                }
            }

            private void UpdateContents()
            {
                _fileStream.Position = 0;
                var writer = new StreamWriter(_fileStream);
                foreach (var item in Infos.Values)
                {
                    writer.WriteLine("{0},{1},{2}", item.Id, item.Version, item.Hash);
                }
                writer.Flush();
            }

            public void Dispose()
            {
                if (_fileStream != null)
                {
                    UpdateContents();
                    _fileStream.Dispose();
                }
            }
        }

        private class PackageInfo : IEquatable<PackageInfo>
        {
            private static readonly Regex _packageNameRegex = new Regex(@"^(?<id>.+?)\.(?<version>[0-9].*)$");
            private string _hash;

            public string Id { get; set; }

            public string Version { get; set; }

            public string Path { get; private set; }

            public string Hash
            {
                get
                {
                    if (_hash == null)
                    {
                        using (var md5 = System.Security.Cryptography.MD5.Create())
                        using (var fileStream = File.OpenRead(Path))
                        {
                            _hash = Convert.ToBase64String(md5.ComputeHash(fileStream));
                        }
                    }
                    return _hash;
                }
                set
                {
                    _hash = value;
                }
            }

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
                    Version = match.Groups["version"].Value,
                    Path = path
                };
            }

            public bool Equals(PackageInfo other)
            {
                return string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(Version, other.Version, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(Hash, other.Hash, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
