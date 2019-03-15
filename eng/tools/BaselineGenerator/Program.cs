// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Packaging;
using NuGet.Packaging.Core;

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
        private readonly CommandOption _feedv3;

        public Program()
        {
            _source = Option("-s|--source <SOURCE>", "The NuGet v2 source of the package to fetch", CommandOptionType.SingleValue);
            _output = Option("-o|--output <OUT>", "The generated file output path", CommandOptionType.SingleValue);
            _feedv3 = Option("--v3", "Sources is nuget v3", CommandOptionType.NoValue);

            Invoke = () => Run().GetAwaiter().GetResult();
        }

        private async Task<int> Run()
        {
            var source = _source.HasValue()
                ? _source.Value()
                : "https://www.nuget.org/api/v2/package";

            var packageCache = Environment.GetEnvironmentVariable("NUGET_PACKAGES") != null
                ? Environment.GetEnvironmentVariable("NUGET_PACKAGES")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

            var tempDir = Path.Combine(Directory.GetCurrentDirectory(), "obj", "tmp");
            Directory.CreateDirectory(tempDir);

            var input = XDocument.Load(Path.Combine(Directory.GetCurrentDirectory(), "Baseline.xml"));

            var output = _output.HasValue()
                ? _output.Value()
                : Path.Combine(Directory.GetCurrentDirectory(), "Baseline.Designer.props");

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
                    var url = _feedv3.HasValue()
                        ? $"{source}/{id.ToLowerInvariant()}/{version}/{id.ToLowerInvariant()}.{version}.nupkg"
                        : $"{source}/{id}/{version}";
                    Console.WriteLine($"Downloading {url}");

                    var response = await client.GetStreamAsync(url);

                    using (var file = File.Create(nupkgPath))
                    {
                        await response.CopyToAsync(file);
                    }
                }


                using (var reader = new PackageArchiveReader(nupkgPath))
                {
                    doc.Root.Add(new XComment($" Package: {id}"));

                    var propertyGroup = new XElement("PropertyGroup",
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
    }
}
