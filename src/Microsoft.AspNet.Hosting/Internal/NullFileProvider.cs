// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNet.Hosting.Internal
{
    internal class NullFileProvider : IFileProvider
    {
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return new NullDirectoryContents();
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            return new NullFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            return new NullChangeToken();
        }

        private class NullDirectoryContents : IDirectoryContents
        {
            public bool Exists => false;

            public IEnumerator<IFileInfo> GetEnumerator()
            {
                yield break;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        internal class NullFileInfo : IFileInfo
        {
            public NullFileInfo(string name)
            {
                Name = name;
            }

            public bool Exists => false;

            public bool IsDirectory => false;

            public DateTimeOffset LastModified => DateTimeOffset.MinValue;

            public long Length => -1;

            public string Name { get; }

            public string PhysicalPath => null;

            public Stream CreateReadStream()
            {
                throw new FileNotFoundException(string.Format($"{nameof(NullFileProvider)} does not support reading files.", Name));
            }
        }

        private class NullChangeToken : IChangeToken
        {
            public bool HasChanged => false;

            public bool ActiveChangeCallbacks => false;

            public IDisposable RegisterChangeCallback(Action<object> callback, object state)
            {
                throw new NotSupportedException($"{nameof(NullFileProvider)} does not support registering change notifications.");
            }
        }
    }
}
