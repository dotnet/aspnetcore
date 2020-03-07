// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.FileProviders
{
    public partial class EmbeddedFileProvider : Microsoft.Extensions.FileProviders.IFileProvider
    {
        public EmbeddedFileProvider(System.Reflection.Assembly assembly) { }
        public EmbeddedFileProvider(System.Reflection.Assembly assembly, string baseNamespace) { }
        public Microsoft.Extensions.FileProviders.IDirectoryContents GetDirectoryContents(string subpath) { throw null; }
        public Microsoft.Extensions.FileProviders.IFileInfo GetFileInfo(string subpath) { throw null; }
        public Microsoft.Extensions.Primitives.IChangeToken Watch(string pattern) { throw null; }
    }
    public partial class ManifestEmbeddedFileProvider : Microsoft.Extensions.FileProviders.IFileProvider
    {
        public ManifestEmbeddedFileProvider(System.Reflection.Assembly assembly) { }
        public ManifestEmbeddedFileProvider(System.Reflection.Assembly assembly, string root) { }
        public ManifestEmbeddedFileProvider(System.Reflection.Assembly assembly, string root, System.DateTimeOffset lastModified) { }
        public ManifestEmbeddedFileProvider(System.Reflection.Assembly assembly, string root, string manifestName, System.DateTimeOffset lastModified) { }
        public System.Reflection.Assembly Assembly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.Extensions.FileProviders.IDirectoryContents GetDirectoryContents(string subpath) { throw null; }
        public Microsoft.Extensions.FileProviders.IFileInfo GetFileInfo(string subpath) { throw null; }
        public Microsoft.Extensions.Primitives.IChangeToken Watch(string filter) { throw null; }
    }
}
namespace Microsoft.Extensions.FileProviders.Embedded
{
    public partial class EmbeddedResourceFileInfo : Microsoft.Extensions.FileProviders.IFileInfo
    {
        public EmbeddedResourceFileInfo(System.Reflection.Assembly assembly, string resourcePath, string name, System.DateTimeOffset lastModified) { }
        public bool Exists { get { throw null; } }
        public bool IsDirectory { get { throw null; } }
        public System.DateTimeOffset LastModified { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public long Length { get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string PhysicalPath { get { throw null; } }
        public System.IO.Stream CreateReadStream() { throw null; }
    }
}
