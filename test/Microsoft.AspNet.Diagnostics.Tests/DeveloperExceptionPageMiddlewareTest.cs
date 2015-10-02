// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics.Views;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.TestHost;
using Microsoft.AspNet.Testing;
using Microsoft.Dnx.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNet.Diagnostics
{
    public class DeveloperExceptionPageMiddlewareTest
    {
        public static TheoryData RelativePathsData
        {
            get
            {
                var data = new TheoryData<string>
                {
                    "TestFiles/SourceFile.txt"
                };

                if (!TestPlatformHelper.IsMono)
                {
                    data.Add(@"TestFiles\SourceFile.txt");
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(RelativePathsData))]
        public void UsesDefaultFileProvider_IfNotProvidedOnOptions(string relativePath)
        {
            // Arrange & Act
            var middleware = GetErrorPageMiddleware(fileProvider: null);
            var stackFrame = middleware.GetStackFrame("func1", relativePath, lineNumber: 10);

            // Assert
            // Lines 4-16 (inclusive) is the code block
            Assert.Equal(4, stackFrame.PreContextLine);
            Assert.Equal(GetCodeLines(4, 9), stackFrame.PreContextCode);
            Assert.Equal(GetCodeLines(10, 10), stackFrame.ContextCode);
            Assert.Equal(GetCodeLines(11, 16), stackFrame.PostContextCode);
        }

        public static TheoryData<string> AbsolutePathsData
        {
            get
            {
                var rootPath = Directory.GetCurrentDirectory();

                var data = new TheoryData<string>()
                {
                    Path.Combine(rootPath, "TestFiles/SourceFile.txt")
                };

                if (!TestPlatformHelper.IsMono)
                {
                    Path.Combine(rootPath, @"TestFiles\SourceFile.txt");
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(AbsolutePathsData))]
        public void DisplaysSourceCodeLines_ForAbsolutePaths(string absoluteFilePath)
        {
            // Arrange
            var rootPath = Directory.GetCurrentDirectory();
            // PhysicalFileProvider handles only relative paths but we fall back to work with absolute paths too
            var provider = new PhysicalFileProvider(rootPath);

            // Act
            var middleware = GetErrorPageMiddleware(provider);
            var stackFrame = middleware.GetStackFrame("func1", absoluteFilePath, lineNumber: 10);

            // Assert
            // Lines 4-16 (inclusive) is the code block
            Assert.Equal(4, stackFrame.PreContextLine);
            Assert.Equal(GetCodeLines(4, 9), stackFrame.PreContextCode);
            Assert.Equal(GetCodeLines(10, 10), stackFrame.ContextCode);
            Assert.Equal(GetCodeLines(11, 16), stackFrame.PostContextCode);
        }

        [Theory]
        [MemberData(nameof(RelativePathsData))]
        public void DisplaysSourceCodeLines_ForRelativePaths(string relativePath)
        {
            // Arrange
            var rootPath = Directory.GetCurrentDirectory();
            var provider = new PhysicalFileProvider(rootPath);

            // Act
            var middleware = GetErrorPageMiddleware(provider);
            var stackFrame = middleware.GetStackFrame("func1", relativePath, lineNumber: 10);

            // Assert
            // Lines 4-16 (inclusive) is the code block
            Assert.Equal(4, stackFrame.PreContextLine);
            Assert.Equal(GetCodeLines(4, 9), stackFrame.PreContextCode);
            Assert.Equal(GetCodeLines(10, 10), stackFrame.ContextCode);
            Assert.Equal(GetCodeLines(11, 16), stackFrame.PostContextCode);
        }

        [Theory]
        [InlineData("TestFiles/EmbeddedSourceFile.txt")]
        //[InlineData(@"TestFiles\EmbeddedSourceFile.txt")]
        public void DisplaysSourceCodeLines_ForRelativeEmbeddedPaths(string relativePath)
        {
            // Arrange
            var provider = new EmbeddedFileProvider(
                GetType().GetTypeInfo().Assembly,
                baseNamespace: $"{typeof(DeveloperExceptionPageMiddlewareTest).GetTypeInfo().Assembly.GetName().Name}.Resources");

            // Act
            var middleware = GetErrorPageMiddleware(provider);
            var stackFrame = middleware.GetStackFrame("func1", relativePath, lineNumber: 10);

            // Assert
            // Lines 4-16 (inclusive) is the code block
            Assert.Equal(4, stackFrame.PreContextLine);
            Assert.Equal(GetCodeLines(4, 9), stackFrame.PreContextCode);
            Assert.Equal(GetCodeLines(10, 10), stackFrame.ContextCode);
            Assert.Equal(GetCodeLines(11, 16), stackFrame.PostContextCode);
        }

        public static TheoryData<ErrorData> DisplaysSourceCodeLines_PreAndPostErrorLineData
        {
            get
            {
                return new TheoryData<ErrorData>()
                {
                    new ErrorData()
                    {
                        AllLines = GetCodeLines(1, 30),
                        ErrorStartLine = 10,
                        ErrorEndLine = 10,
                        ExpectedPreContextLine = 4,
                        ExpectedPreErrorCode = GetCodeLines(4, 9),
                        ExpectedErrorCode = GetCodeLines(10, 10),
                        ExpectedPostErrorCode = GetCodeLines(11, 16)
                    },
                    new ErrorData()
                    {
                        AllLines = GetCodeLines(1, 30),
                        ErrorStartLine = 10,
                        ErrorEndLine = 13, // multi-line error
                        ExpectedPreContextLine = 4,
                        ExpectedPreErrorCode = GetCodeLines(4, 9),
                        ExpectedErrorCode = GetCodeLines(10, 13),
                        ExpectedPostErrorCode = GetCodeLines(14, 19)
                    },

                    // PreErrorCode less than source code line count
                    new ErrorData()
                    {
                        AllLines = GetCodeLines(1, 10),
                        ErrorStartLine = 1,
                        ErrorEndLine = 1,
                        ExpectedPreContextLine = 1,
                        ExpectedPreErrorCode = Enumerable.Empty<string>(),
                        ExpectedErrorCode = GetCodeLines(1, 1),
                        ExpectedPostErrorCode = GetCodeLines(2, 7)
                    },
                    new ErrorData()
                    {
                        AllLines = GetCodeLines(1, 10),
                        ErrorStartLine = 3,
                        ErrorEndLine = 5,
                        ExpectedPreContextLine = 1,
                        ExpectedPreErrorCode = GetCodeLines(1, 2),
                        ExpectedErrorCode = GetCodeLines(3, 5),
                        ExpectedPostErrorCode = GetCodeLines(6, 10)
                    },

                    // PostErrorCode less than source code line count
                    new ErrorData()
                    {
                        AllLines = GetCodeLines(1, 10),
                        ErrorStartLine = 10,
                        ErrorEndLine = 10,
                        ExpectedPreContextLine = 4,
                        ExpectedPreErrorCode = GetCodeLines(4, 9),
                        ExpectedErrorCode = GetCodeLines(10, 10),
                        ExpectedPostErrorCode = Enumerable.Empty<string>()
                    },
                    new ErrorData()
                    {
                        AllLines = GetCodeLines(1, 10),
                        ErrorStartLine = 7,
                        ErrorEndLine = 10,
                        ExpectedPreContextLine = 1,
                        ExpectedPreErrorCode = GetCodeLines(1, 6),
                        ExpectedErrorCode = GetCodeLines(7, 10),
                        ExpectedPostErrorCode = Enumerable.Empty<string>()
                    },
                    new ErrorData()
                    {
                        AllLines = GetCodeLines(1, 10),
                        ErrorStartLine = 5,
                        ErrorEndLine = 8,
                        ExpectedPreContextLine = 1,
                        ExpectedPreErrorCode = GetCodeLines(1, 4),
                        ExpectedErrorCode = GetCodeLines(5, 8),
                        ExpectedPostErrorCode = GetCodeLines(9, 10)
                    },

                    // Pre and Post error code less than source code line count
                    new ErrorData()
                    {
                        AllLines = GetCodeLines(1, 4),
                        ErrorStartLine = 2,
                        ErrorEndLine = 3,
                        ExpectedPreContextLine = 1,
                        ExpectedPreErrorCode = GetCodeLines(1, 1),
                        ExpectedErrorCode = GetCodeLines(2, 3),
                        ExpectedPostErrorCode = GetCodeLines(4, 4)
                    },
                    new ErrorData()
                    {
                        AllLines = GetCodeLines(1, 4),
                        ErrorStartLine = 1,
                        ErrorEndLine = 4,
                        ExpectedPreContextLine = 1,
                        ExpectedPreErrorCode = Enumerable.Empty<string>(),
                        ExpectedErrorCode = GetCodeLines(1, 4),
                        ExpectedPostErrorCode = Enumerable.Empty<string>()
                    },

                    // change source code line count
                    new ErrorData()
                    {
                        SourceCodeLineCount = 1,
                        AllLines = GetCodeLines(1, 1),
                        ErrorStartLine = 1,
                        ErrorEndLine = 1,
                        ExpectedPreContextLine = 1,
                        ExpectedPreErrorCode = Enumerable.Empty<string>(),
                        ExpectedErrorCode = GetCodeLines(1, 1),
                        ExpectedPostErrorCode = Enumerable.Empty<string>()
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(DisplaysSourceCodeLines_PreAndPostErrorLineData))]
        public void DisplaysSourceCodeLines_PreAndPostErrorLine(ErrorData errorData)
        {
            // Arrange
            var middleware = GetErrorPageMiddleware();
            var stackFrame = new StackFrame();

            // Act
            middleware.ReadFrameContent(
                stackFrame, errorData.AllLines, errorData.ErrorStartLine, errorData.ErrorEndLine);

            // Assert
            Assert.Equal(errorData.ExpectedPreContextLine, stackFrame.PreContextLine);
            Assert.Equal(errorData.ExpectedPreErrorCode, stackFrame.PreContextCode);
            Assert.Equal(errorData.ExpectedErrorCode, stackFrame.ContextCode);
            Assert.Equal(errorData.ExpectedPostErrorCode, stackFrame.PostContextCode);
        }

        private static IEnumerable<string> GetCodeLines(int fromLine, int toLine)
        {
            var start = fromLine;
            var count = toLine - fromLine + 1;
            return Enumerable.Range(start, count).Select(i => string.Format("Line{0}", i));
        }

        private DeveloperExceptionPageMiddleware GetErrorPageMiddleware(
            IFileProvider fileProvider = null, int sourceCodeLineCount = 6)
        {
            var errorPageOptions = new ErrorPageOptions();
            errorPageOptions.SourceCodeLineCount = sourceCodeLineCount;

            if (fileProvider != null)
            {
                errorPageOptions.FileProvider = fileProvider;
            }

            var middleware = new DeveloperExceptionPageMiddleware(
                (httpContext) => { return Task.FromResult(0); },
                errorPageOptions,
                new LoggerFactory(),
                new TestApplicationEnvironment(),
                new TelemetryListener("Microsoft.Aspnet"));

            return middleware;
        }

        private class TestApplicationEnvironment : IApplicationEnvironment
        {
            public string ApplicationBasePath
            {
                get
                {
                    return Directory.GetCurrentDirectory();
                }
            }

            public string ApplicationName
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string ApplicationVersion
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string Configuration
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public FrameworkName RuntimeFramework
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string Version
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public object GetData(string name)
            {
                throw new NotImplementedException();
            }

            public void SetData(string name, object value)
            {
                throw new NotImplementedException();
            }
        }

        private class TestFileProvider : IFileProvider
        {
            private readonly IEnumerable<string> _sourceCodeLines;

            public TestFileProvider(IEnumerable<string> sourceCodeLines)
            {
                _sourceCodeLines = sourceCodeLines;
            }

            public IDirectoryContents GetDirectoryContents(string subpath)
            {
                throw new NotImplementedException();
            }

            public IFileInfo GetFileInfo(string subpath)
            {
                return new TestFileInfo(_sourceCodeLines);
            }

            public IChangeToken Watch(string filter)
            {
                throw new NotImplementedException();
            }
        }

        private class TestFileInfo : IFileInfo
        {
            private readonly MemoryStream _stream;

            public TestFileInfo(IEnumerable<string> sourceCodeLines)
            {
                _stream = new MemoryStream();
                using (var writer = new StreamWriter(_stream, Encoding.UTF8, 1024, leaveOpen: true))
                {
                    foreach (var line in sourceCodeLines)
                    {
                        writer.WriteLine(line);
                    }
                }
                _stream.Seek(0, SeekOrigin.Begin);
            }

            public bool Exists
            {
                get
                {
                    return true;
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
                    return null;
                }
            }

            public Stream CreateReadStream()
            {
                return _stream;
            }
        }

        public class ErrorData
        {
            public int SourceCodeLineCount { get; set; } = 6;
            public IEnumerable<string> AllLines { get; set; }
            public int ErrorStartLine { get; set; }
            public int ErrorEndLine { get; set; }
            public int ExpectedPreContextLine { get; set; }
            public IEnumerable<string> ExpectedPreErrorCode { get; set; }
            public IEnumerable<string> ExpectedErrorCode { get; set; }
            public IEnumerable<string> ExpectedPostErrorCode { get; set; }
        }

        [Fact]
        public async Task UnhandledErrorsWriteToDiagnosticTelemetryWhenUsingExceptionPage()
        {
            // Arrange
            TelemetryListener telemetryListener = null;
            var server = TestServer.Create(app =>
            {
                telemetryListener = app.ApplicationServices.GetRequiredService<TelemetryListener>();
                app.UseDeveloperExceptionPage();
                app.Run(context =>
                {
                    throw new Exception("Test exception");
                });
            });
            var listener = new TestTelemetryListener();
            telemetryListener.SubscribeWithAdapter(listener);

            // Act
            await server.CreateClient().GetAsync("/path");

            // Assert
            Assert.NotNull(listener.EndRequest?.HttpContext);
            Assert.Null(listener.HostingUnhandledException?.HttpContext);
            Assert.Null(listener.HostingUnhandledException?.Exception);
            Assert.NotNull(listener.DiagnosticUnhandledException?.HttpContext);
            Assert.NotNull(listener.DiagnosticUnhandledException?.Exception);
            Assert.Null(listener.DiagnosticHandledException?.HttpContext);
            Assert.Null(listener.DiagnosticHandledException?.Exception);
        }
    }
}
