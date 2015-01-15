// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core;
using Microsoft.Framework.Expiration.Interfaces;
using Microsoft.Framework.Logging;
using Xunit;

namespace Microsoft.AspNet.StaticFiles
{
    public class StaticFileContextTest
    {
        [Fact]
        public void LookupFileInfo_ReturnsFalse_IfFileDoesNotExist()
        {
            // Arrange
            var options = new StaticFileOptions();
            options.FileSystem = new TestFileSystem();
            var context = new StaticFileContext(new DefaultHttpContext(), options, PathString.Empty, NullLogger.Instance);

            // Act
            var validateResult = context.ValidatePath();
            var lookupResult = context.LookupFileInfo();

            // Assert
            Assert.True(validateResult);
            Assert.False(lookupResult);
        }

        [Fact]
        public void LookupFileInfo_ReturnsTrue_IfFileExists()
        {
            // Arrange
            var options = new StaticFileOptions();
            var fileSystem = new TestFileSystem();
            fileSystem.AddFile("/foo.txt", new TestFileInfo
            {
                LastModified = new DateTimeOffset(2014, 1, 2, 3, 4, 5, TimeSpan.Zero)
            });
            options.FileSystem = fileSystem;
            var pathString = new PathString("/test");
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = new PathString("/test/foo.txt");
            var context = new StaticFileContext(httpContext, options, pathString, NullLogger.Instance);

            // Act
            context.ValidatePath();
            var result = context.LookupFileInfo();

            // Assert
            Assert.True(result);
        }

        private sealed class TestFileSystem : IFileSystem
        {
            private readonly Dictionary<string, IFileInfo> _files = new Dictionary<string, IFileInfo>(StringComparer.Ordinal);

            public void AddFile(string path, IFileInfo fileInfo)
            {
                _files[path] = fileInfo;
            }

            public IDirectoryContents GetDirectoryContents(string subpath)
            {
                return new NotFoundDirectoryContents();
            }

            public IFileInfo GetFileInfo(string subpath)
            {
                IFileInfo result;
                if (_files.TryGetValue(subpath, out result))
                {
                    return result;
                }

                return new NotFoundFileInfo(subpath);
            }

            public IExpirationTrigger Watch(string filter)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class TestFileInfo : IFileInfo
        {
            public bool Exists
            {
                get { return true; }
            }

            public bool IsDirectory
            {
                get { return false; }
            }

            public bool IsReadOnly
            {
                get { return false;  }
            }

            public DateTimeOffset LastModified { get; set; }

            public long Length { get; set; }

            public string Name { get; set; }

            public string PhysicalPath { get; set; }

            public Stream CreateReadStream()
            {
                throw new NotImplementedException();
            }

            public void Delete()
            {
                throw new NotImplementedException();
            }

            public void WriteContent(byte[] content)
            {
                throw new NotImplementedException();
            }
        }
    }
}