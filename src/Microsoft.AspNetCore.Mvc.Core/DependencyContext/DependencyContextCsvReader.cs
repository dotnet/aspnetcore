// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.Extensions.DependencyModel
{
    internal class DependencyContextCsvReader: IDependencyContextReader
    {
        public DependencyContext Read(Stream stream)
        {
            var lines = new List<DepsFileLine>();
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var line = new DepsFileLine();
                    line.LibraryType = ReadValue(reader);
                    line.PackageName = ReadValue(reader);
                    line.PackageVersion = ReadValue(reader);
                    line.PackageHash = ReadValue(reader);
                    line.AssetType = ReadValue(reader);
                    line.AssetName = ReadValue(reader);
                    line.AssetPath = ReadValue(reader);

                    if (line.AssetType == "runtime" &&
                        !line.AssetPath.EndsWith(".ni.dll"))
                    {
                        lines.Add(line);
                    }
                    SkipWhitespace(reader);
                }
            }

            var runtimeLibraries = new List<RuntimeLibrary>();
            var packageGroups = lines.GroupBy(PackageIdentity);
            foreach (var packageGroup in packageGroups)
            {
                var identity = packageGroup.Key;
                runtimeLibraries.Add(new RuntimeLibrary(
                    type: identity.Item1,
                    name: identity.Item2,
                    version: identity.Item3,
                    hash: identity.Item4,
                    assemblies: packageGroup.Select(l => RuntimeAssembly.Create(l.AssetPath)),
                    nativeLibraries: Enumerable.Empty<string>(),
                    resourceAssemblies: Enumerable.Empty<ResourceAssembly>(),
                    subTargets: Enumerable.Empty<RuntimeTarget>(),
                    dependencies: Enumerable.Empty<Dependency>(),
                    serviceable: false
                    ));
            }

            return new DependencyContext(
                targetFramework: string.Empty,
                runtime: string.Empty,
                isPortable: false,
                compilationOptions: CompilationOptions.Default,
                compileLibraries: Enumerable.Empty<CompilationLibrary>(),
                runtimeLibraries: runtimeLibraries.ToArray(),
                runtimeGraph: Enumerable.Empty<RuntimeFallbacks>());
        }

        private Tuple<string, string, string, string> PackageIdentity(DepsFileLine line)
        {
            return Tuple.Create(line.LibraryType, line.PackageName, line.PackageVersion, line.PackageHash);
        }

        private void SkipWhitespace(StreamReader reader)
        {
            // skip all whitespace
            while (!reader.EndOfStream && char.IsWhiteSpace((char)reader.Peek()))
            {
                reader.Read();
            }
        }

        private string ReadValue(StreamReader reader)
        {
            SkipWhitespace(reader);

            var c = ReadSucceed(reader.Read());
            if (c != '"')
            {
                throw new FormatException("Deps file value should start with '\"'");
            }

            var value = new StringBuilder();
            while (ReadSucceed(reader.Peek()) != '"')
            {
                c = ReadSucceed(reader.Read());
                if (c == '\\')
                {
                    value.Append(ReadSucceed(reader.Read()));
                }
                else
                {
                    value.Append(c);
                }
            }
            // Read last "
            ReadSucceed(reader.Read());
            // Read comment
            if (reader.Peek() == ',')
            {
                reader.Read();
            }
            return value.ToString();
        }

        private char ReadSucceed(int c)
        {
            if (c == -1)
            {
                throw new FormatException("Unexpected end of file");
            }
            return (char) c;
        }

        private struct DepsFileLine
        {
            public string LibraryType;
            public string PackageName;
            public string PackageVersion;
            public string PackageHash;
            public string AssetType;
            public string AssetName;
            public string AssetPath;
        }
    }
}