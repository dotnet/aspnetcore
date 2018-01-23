// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.LanguageServices.Razor;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal class DiscoverCommand : CommandBase
    {
        public DiscoverCommand(Application parent)
            : base(parent, "discover")
        {
            Assemblies = Argument("assemblies", "assemblies to search for tag helpers", multipleValues: true);
            TagHelperManifest = Option("-o", "output file", CommandOptionType.SingleValue);
            ProjectDirectory = Option("-p", "project root directory", CommandOptionType.SingleValue);
        }

        public CommandArgument Assemblies { get; }

        public CommandOption TagHelperManifest { get; }

        public CommandOption ProjectDirectory { get; }

        protected override bool ValidateArguments()
        {
            if (string.IsNullOrEmpty(TagHelperManifest.Value()))
            {
                Error.WriteLine($"{TagHelperManifest.ValueName} must be specified.");
                return false;
            }

            if (Assemblies.Values.Count == 0)
            {
                Error.WriteLine($"{Assemblies.Name} must have at least one value.");
                return false;
            }

            if (string.IsNullOrEmpty(ProjectDirectory.Value()))
            {
                ProjectDirectory.Values.Add(Environment.CurrentDirectory);
            }

            return true;
        }

        protected override Task<int> ExecuteCoreAsync()
        {
            var result = ExecuteCore(
                projectDirectory: ProjectDirectory.Value(),
                outputFilePath: TagHelperManifest.Value(),
                assemblies: Assemblies.Values.ToArray());

            return Task.FromResult(result);
        }

        private int ExecuteCore(string projectDirectory, string outputFilePath, string[] assemblies)
        {
            outputFilePath = Path.Combine(projectDirectory, outputFilePath);

            var metadataReferences = new MetadataReference[assemblies.Length];
            for (var i = 0; i < assemblies.Length; i++)
            {
                metadataReferences[i] = MetadataReference.CreateFromFile(assemblies[i]);
            }

            var engine = RazorEngine.Create((b) =>
            {
                RazorExtensions.Register(b);

                b.Features.Add(new DefaultMetadataReferenceFeature() { References = metadataReferences });
                b.Features.Add(new CompilationTagHelperFeature());

                // TagHelperDescriptorProviders (actually do tag helper discovery)
                b.Features.Add(new DefaultTagHelperDescriptorProvider());
                b.Features.Add(new ViewComponentTagHelperDescriptorProvider());
            });

            var feature = engine.Features.OfType<ITagHelperFeature>().Single();
            var tagHelpers = feature.GetDescriptors();

            using (var stream = new MemoryStream())
            {
                Serialize(stream, tagHelpers);

                stream.Position = 0L;

                var newHash = Hash(stream);
                var existingHash = Hash(outputFilePath);

                if (!HashesEqual(newHash, existingHash))
                {
                    stream.Position = 0;
                    using (var output = File.OpenWrite(outputFilePath))
                    {
                        stream.CopyTo(output);
                    }
                }
            }

            return 0;
        }

        private static byte[] Hash(string path)
        {
            if (!File.Exists(path))
            {
                return Array.Empty<byte>();
            }

            using (var stream = File.OpenRead(path))
            {
                return Hash(stream);
            }
        }

        private static byte[] Hash(Stream stream)
        {
            using (var sha = SHA256.Create())
            {
                sha.ComputeHash(stream);
                return sha.Hash;
            }
        }

        private bool HashesEqual(byte[] x, byte[] y)
        {
            if (x.Length != y.Length)
            {
                return false;
            }

            for (var i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static void Serialize(Stream stream, IReadOnlyList<TagHelperDescriptor> tagHelpers)
        {
            using (var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 4096, leaveOpen: true))
            {
                var serializer = new JsonSerializer();
                serializer.Converters.Add(new TagHelperDescriptorJsonConverter());
                serializer.Converters.Add(new RazorDiagnosticJsonConverter());

                serializer.Serialize(writer, tagHelpers);
            }
        }
    }
}