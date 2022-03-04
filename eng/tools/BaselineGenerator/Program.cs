// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace PackageBaselineGenerator
{
    /// <summary>
    /// This generates Baseline.props with information about the last RTM release.
    /// </summary>
    class Program : CommandLineApplication
    {
        static void Main(string[] args)
        {
            new Program().Execute(args);
        }

        private readonly CommandOption _source;
        private readonly CommandOption _output;
        private readonly CommandOption _update;

        public Program()
        {
            _source = Option("-s|--package-source <SOURCE>", "The NuGet source of packages to fetch", CommandOptionType.SingleValue);
            _output = Option("-o|--output <OUT>", "The generated file output path", CommandOptionType.SingleValue);
            _update = Option("-u|--update", "Regenerate the input (Baseline.xml) file.", CommandOptionType.NoValue);

            Invoke = () => Run().GetAwaiter().GetResult();
        }

        private async Task<int> Run()
        {
            var source = _source.HasValue()
                ? _source.Value().TrimEnd('/')
                : "https://api.nuget.org/v3/index.json";
            if (_output.HasValue() && _update.HasValue())
            {
                await Error.WriteLineAsync("'--output' and '--update' options must not be used together.");
                return 1;
            }

            var inputPath = Path.Combine(Directory.GetCurrentDirectory(), "Baseline.xml");
            var input = XDocument.Load(inputPath);
            var packageSource = new PackageSource(source);
            var providers = Repository.Provider.GetCoreV3(); // Get v2 and v3 API support
            var sourceRepository = new SourceRepository(packageSource, providers);
            if (_update.HasValue())
            {
                var updateResult = await RunUpdateAsync(inputPath, input, sourceRepository);
                if (updateResult != 0)
                {
                    return updateResult;
                }
            }

            var feedType = await sourceRepository.GetFeedType(CancellationToken.None);
            var feedV3 = feedType == FeedType.HttpV3;
            var packageBase = source + "/package";
            if (feedV3)
            {
                var resources = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
                packageBase = resources.GetServiceEntryUri(ServiceTypes.PackageBaseAddress).ToString().TrimEnd('/');
            }

            var output = _output.HasValue()
                ? _output.Value()
                : Path.Combine(Directory.GetCurrentDirectory(), "Baseline.Designer.props");

            var packageCache = Environment.GetEnvironmentVariable("NUGET_PACKAGES") ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

            var tempDir = Path.Combine(Directory.GetCurrentDirectory(), "obj", "tmp");
            Directory.CreateDirectory(tempDir);

            var baselineVersion = input.Root.Attribute("Version").Value;

            var doc = new XDocument(
                new XComment(" Auto generated. Do not edit manually, use eng/tools/BaselineGenerator/ to recreate. "),
                new XElement("Project",
                    new XElement("PropertyGroup",
                        new XElement("MSBuildAllProjects", "$(MSBuildAllProjects);$(MSBuildThisFileFullPath)"),
                        new XElement("AspNetCoreBaselineVersion", baselineVersion))));

            var client = new HttpClient();
            foreach (var pkg in input.Root.Descendants("Package"))
            {
                var id = pkg.Attribute("Id").Value;
                var version = pkg.Attribute("Version").Value;
                var packageFileName = $"{id}.{version}.nupkg";
                var nupkgPath = Path.Combine(packageCache, id.ToLowerInvariant(), version, packageFileName);
                if (!File.Exists(nupkgPath))
                {
                    nupkgPath = Path.Combine(tempDir, packageFileName);
                }

                if (!File.Exists(nupkgPath))
                {
                    var url = feedV3 ?
                        $"{packageBase}/{id.ToLowerInvariant()}/{version}/{id.ToLowerInvariant()}.{version}.nupkg" :
                        $"{packageBase}/{id}/{version}";

                    Console.WriteLine($"Downloading {url}");
                    using (var response = await client.GetStreamAsync(url))
                    {
                        using (var file = File.Create(nupkgPath))
                        {
                            await response.CopyToAsync(file);
                        }
                    }
                }

                using (var reader = new PackageArchiveReader(nupkgPath))
                {
                    doc.Root.Add(new XComment($" Package: {id}"));

                    var propertyGroup = new XElement(
                        "PropertyGroup",
                        new XAttribute("Condition", $" '$(PackageId)' == '{id}' "),
                        new XElement("BaselinePackageVersion", version));
                    doc.Root.Add(propertyGroup);

                    foreach (var group in reader.NuspecReader.GetDependencyGroups())
                    {
                        var itemGroup = new XElement("ItemGroup", new XAttribute("Condition", $" '$(PackageId)' == '{id}' AND '$(TargetFramework)' == '{group.TargetFramework.GetShortFolderName()}' "));
                        doc.Root.Add(itemGroup);

                        foreach (var dependency in group.Packages)
                        {
                            itemGroup.Add(new XElement("BaselinePackageReference", new XAttribute("Include", dependency.Id), new XAttribute("Version", dependency.VersionRange.ToString())));
                        }
                    }
                }
            }

            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Encoding = Encoding.UTF8,
                Indent = true,
            };

            using (var writer = XmlWriter.Create(output, settings))
            {
                doc.Save(writer);
            }

            Console.WriteLine($"Generated file in {output}");

            return 0;
        }

        private async Task<int> RunUpdateAsync(
            string documentPath,
            XDocument document,
            SourceRepository sourceRepository)
        {
            var packageMetadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();
            var logger = new Logger(Error, Out);
            var hasChanged = false;
            using (var cacheContext = new SourceCacheContext { NoCache = true })
            {
                var versionAttribute = document.Root.Attribute("Version");
                hasChanged = await TryUpdateVersionAsync(
                    versionAttribute,
                    "Microsoft.AspNetCore.App",
                    packageMetadataResource,
                    logger,
                    cacheContext);

                foreach (var package in document.Root.Descendants("Package"))
                {
                    var id = package.Attribute("Id").Value;
                    versionAttribute = package.Attribute("Version");
                    var attributeChanged = await TryUpdateVersionAsync(
                        versionAttribute,
                        id,
                        packageMetadataResource,
                        logger,
                        cacheContext);

                    hasChanged |= attributeChanged;
                }
            }

            if (hasChanged)
            {
                await Out.WriteLineAsync($"Updating {documentPath}.");

                var settings = new XmlWriterSettings
                {
                    Async = true,
                    CheckCharacters = true,
                    CloseOutput = false,
                    Encoding = Encoding.UTF8,
                    Indent = true,
                    IndentChars = "  ",
                    NewLineOnAttributes = false,
                    OmitXmlDeclaration = true,
                    WriteEndDocumentOnClose = true,
                };

                using (var stream = File.OpenWrite(documentPath))
                {
                    using (var writer = XmlWriter.Create(stream, settings))
                    {
                        await document.SaveAsync(writer, CancellationToken.None);
                    }
                }
            }
            else
            {
                await Out.WriteLineAsync("No new versions found");
            }

            return 0;
        }

        private static async Task<bool> TryUpdateVersionAsync(
            XAttribute versionAttribute,
            string packageId,
            PackageMetadataResource packageMetadataResource,
            ILogger logger,
            SourceCacheContext cacheContext)
        {
            var searchMetadata = await packageMetadataResource.GetMetadataAsync(
                packageId,
                includePrerelease: false,
                includeUnlisted: true, // Microsoft.AspNetCore.DataOrotection.Redis package is not listed.
                sourceCacheContext: cacheContext,
                log: logger,
                token: CancellationToken.None);

            var currentVersion = NuGetVersion.Parse(versionAttribute.Value);
            var versionRange = new VersionRange(
                currentVersion,
                new FloatRange(NuGetVersionFloatBehavior.Patch, currentVersion));

            var latestVersion = versionRange.FindBestMatch(
                searchMetadata.Select(metadata => metadata.Identity.Version));

            if (latestVersion == null)
            {
                logger.LogWarning($"Unable to find latest version of '{packageId}'.");
                return false;
            }

            var hasChanged = false;
            if (latestVersion != currentVersion)
            {
                hasChanged = true;
                versionAttribute.Value = latestVersion.ToNormalizedString();
            }

            return hasChanged;
        }

        private class Logger : ILogger
        {
            private readonly TextWriter _error;
            private readonly TextWriter _out;

            public Logger(TextWriter error, TextWriter @out)
            {
                _error = error;
                _out = @out;
            }

            public void Log(LogLevel level, string data)
            {
                switch (level)
                {
                    case LogLevel.Debug:
                        LogDebug(data);
                        break;
                    case LogLevel.Error:
                        LogError(data);
                        break;
                    case LogLevel.Information:
                        LogInformation(data);
                        break;
                    case LogLevel.Minimal:
                        LogMinimal(data);
                        break;
                    case LogLevel.Verbose:
                        LogVerbose(data);
                        break;
                    case LogLevel.Warning:
                        LogWarning(data);
                        break;
                }
            }

            public void Log(ILogMessage message) => Log(message.Level, message.Message);

            public Task LogAsync(LogLevel level, string data)
            {
                Log(level, data);
                return Task.CompletedTask;
            }

            public Task LogAsync(ILogMessage message) => LogAsync(message.Level, message.Message);

            public void LogDebug(string data) => _out.WriteLine($"Debug: {data}");

            public void LogError(string data) => _error.WriteLine($"Error: {data}");

            public void LogInformation(string data) => _out.WriteLine($"Information: {data}");

            public void LogInformationSummary(string data) => _out.WriteLine($"Summary: {data}");

            public void LogMinimal(string data) => _out.WriteLine($"Minimal: {data}");

            public void LogVerbose(string data) => _out.WriteLine($"Verbose: {data}");

            public void LogWarning(string data) => _out.WriteLine($"Warning: {data}");
        }
    }
}
