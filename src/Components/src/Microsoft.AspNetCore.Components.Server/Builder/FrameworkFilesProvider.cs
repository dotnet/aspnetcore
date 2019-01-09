// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Builder
{
    internal class FrameworkFilesProvider : IFileProvider
    {
        private static Lazy<string> BlazorServerJsContent = new Lazy<string>(() =>
        {
            var thisAssembly = typeof(RazorComponentsApplicationBuilderExtensions).Assembly;
            var resourceStream = thisAssembly.GetManifestResourceStream("blazor.server.js");
            using (var streamReader = new StreamReader(resourceStream))
            {
                return streamReader.ReadToEnd();
            }
        });

        private Dictionary<string, FrameworkFileInfo> _frameworkFiles = new[]
        {
            new FrameworkFileInfo("/blazor.server.js", BlazorServerJsContent.Value),

            // This is needed temporarily until we implement a proper version of
            // library-embedded static resources for Razor Components apps.
            new FrameworkFileInfo("/blazor.boot.json", @"{ ""cssReferences"": [], ""jsReferences"": [] }")
        }.ToDictionary(file => file.PhysicalPath, file => file);

        // Not needed
        public IDirectoryContents GetDirectoryContents(string subpath)
            => throw new NotImplementedException();

        // Not needed
        public IChangeToken Watch(string filter)
            => throw new NotImplementedException();

        public IFileInfo GetFileInfo(string subpath)
        {
            return _frameworkFiles.TryGetValue(subpath, out var fileInfo)
                ? (IFileInfo)fileInfo
                : NonExistingFileInfo.Instance;
        }

        class NonExistingFileInfo : IFileInfo
        {
            public static NonExistingFileInfo Instance = new NonExistingFileInfo();

            public bool Exists => false;

            public long Length => throw new NotImplementedException();

            public string PhysicalPath => throw new NotImplementedException();

            public string Name => throw new NotImplementedException();

            public DateTimeOffset LastModified => throw new NotImplementedException();

            public bool IsDirectory => false;

            public Stream CreateReadStream() => throw new NotImplementedException();
        }

        class FrameworkFileInfo : IFileInfo
        {
            private readonly byte[] _textContentUtf8;

            public FrameworkFileInfo(string path, string textContent)
            {
                _textContentUtf8 = Encoding.UTF8.GetBytes(textContent);
                PhysicalPath = path;
                Name = Path.GetFileName(path);
            }

            public bool Exists => true;

            public long Length => _textContentUtf8.Length;

            public string PhysicalPath { get; }

            public string Name { get; }

            public DateTimeOffset LastModified => new DateTime(2000, 1, 1); // Any fixed past value

            public bool IsDirectory => false;

            public Stream CreateReadStream()
            {
                return new MemoryStream(_textContentUtf8);
            }
        }
    }
}
