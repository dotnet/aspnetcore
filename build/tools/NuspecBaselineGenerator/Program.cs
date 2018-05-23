using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using McMaster.Extensions.CommandLineUtils;
using NuGet.Packaging;

namespace NuspecBaselineGenerator
{
    class Program
    {
        static void Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        [Required]
        [DirectoryExists]
        [Argument(0)]
        public string[] Directories { get; }

        [Required]
        [Option]
        [FileExists]
        public string Artifacts { get; }

        private void OnExecute()
        {
            var doc = XDocument.Load(Artifacts);
            var versions = new List<(string, string)>();
            foreach (var dir in Directories)
            {
                foreach (var nupkg in Directory.EnumerateFiles(dir, "*.nupkg"))
                {
                    using (var reader = new PackageArchiveReader(nupkg))
                    {
                        var identity = reader.GetIdentity();
                        versions.Add((identity.Id, identity.Version.ToNormalizedString()));
                    }
                }
            }

            void WriteAttribute(XElement element, string attr)
            {
                var attribute = element.Attribute(attr);
                if (attribute != null)
                {
                    Console.Write($" {attr}=\"{attribute.Value}\"");
                }
            }

            foreach (var item in versions.OrderBy(i => i.Item1))
            {
                var element = doc
                    .Descendants("PackageArtifact")
                    .SingleOrDefault(p => p.Attribute("Include")?.Value == item.Item1);


                Console.Write($"<ExternalDependency Include=\"{item.Item1}\" Version=\"{item.Item2}\"");
                if (element != null)
                {
                    WriteAttribute(element, "Analyzer");
                    WriteAttribute(element, "AllMetapackage");
                    WriteAttribute(element, "AppMetapackage");
                    WriteAttribute(element, "LZMA");
                    WriteAttribute(element, "PackageType");
                    WriteAttribute(element, "Category");
                }
                Console.WriteLine(" />");
            }
        }
    }
}
