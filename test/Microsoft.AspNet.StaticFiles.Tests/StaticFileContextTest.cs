// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Primitives;
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
            options.FileProvider = new TestFileProvider();
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
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile("/foo.txt", new TestFileInfo
            {
                LastModified = new DateTimeOffset(2014, 1, 2, 3, 4, 5, TimeSpan.Zero)
            });
            options.FileProvider = fileProvider;
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

        private sealed class TestFileProvider : IFileProvider
        {
            private readonly Dictionary<string, IFileInfo> _files = new Dictionary<string, IFileInfo>(StringComparer.Ordinal);

            public void AddFile(string path, IFileInfo fileInfo)
            {
                _files[path] = fileInfo;
            }

            public IDirectoryContents GetDirectoryContents(string subpath)
            {
                throw new NotImplementedException();
            }

            public IFileInfo GetFileInfo(string subpath)
            {
                IFileInfo result;
                if (_files.TryGetValue(subpath, out result))
                {
                    return result;
                }

                return new NotFoundFileInfo();
            }

            public IChangeToken Watch(string filter)
            {
                throw new NotSupportedException();
            }

            private class NotFoundFileInfo : IFileInfo
            {
                public bool Exists
                {
                    get
                    {
                        return false;
                    }
                }

                public bool IsDirectory
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                public DateTimeOffset LastModified
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                public long Length
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                public string Name
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                public string PhysicalPath
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                public Stream CreateReadStream()
                {
                    throw new NotImplementedException();
                }
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

            public DateTimeOffset LastModified { get; set; }

            public long Length { get; set; }

            public string Name { get; set; }

            public string PhysicalPath { get; set; }

            public Stream CreateReadStream()
            {
                throw new NotImplementedException();
            }
        }
    }
}