// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics.Views;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Http;
using Microsoft.Dnx.Compilation;
using Microsoft.Dnx.Runtime;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Diagnostics
{
    /// <summary>
    /// Captures synchronous and asynchronous exceptions from the pipeline and generates HTML error responses.
    /// </summary>
    public class DeveloperExceptionPageMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ErrorPageOptions _options;
        private static readonly bool IsMono = Type.GetType("Mono.Runtime") != null;
        private readonly ILogger _logger;
        private readonly IFileProvider _fileProvider;
        private readonly TelemetrySource _telemetrySource;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeveloperExceptionPageMiddleware"/> class
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        public DeveloperExceptionPageMiddleware(
            RequestDelegate next,
            ErrorPageOptions options,
            ILoggerFactory loggerFactory,
            IApplicationEnvironment appEnvironment,
            TelemetrySource telemetrySource)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _next = next;
            _options = options;
            _logger = loggerFactory.CreateLogger<DeveloperExceptionPageMiddleware>();
            _fileProvider = options.FileProvider ?? new PhysicalFileProvider(appEnvironment.ApplicationBasePath);
            _telemetrySource = telemetrySource;
        }

        /// <summary>
        /// Process an individual request.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "For diagnostics")]
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError("An unhandled exception has occurred while executing the request", ex);

                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("The response has already started, the error page middleware will not be executed.");
                    throw;
                }

                try
                {
                    context.Response.Clear();
                    context.Response.StatusCode = 500;

                    await DisplayException(context, ex);

                    _telemetrySource.WriteTelemetry("Microsoft.AspNet.Diagnostics.UnhandledException", new { httpContext = context, exception = ex });

                    return;
                }
                catch (Exception ex2)
                {
                    // If there's a Exception while generating the error page, re-throw the original exception.
                    _logger.LogError("An exception was thrown attempting to display the error page.", ex2);
                }
                throw;
            }
        }

        // Assumes the response headers have not been sent.  If they have, still attempt to write to the body.
        private Task DisplayException(HttpContext context, Exception ex)
        {
            var compilationException = ex as ICompilationException;
            if (compilationException != null)
            {
                return DisplayCompilationException(context, compilationException);
            }

            return DisplayRuntimeException(context, ex);
        }

        private Task DisplayCompilationException(HttpContext context,
                                                 ICompilationException compilationException)
        {
            var model = new CompilationErrorPageModel
            {
                Options = _options,
            };

            foreach (var compilationFailure in compilationException.CompilationFailures)
            {
                var stackFrames = new List<StackFrame>();
                var errorDetails = new ErrorDetails
                {
                    StackFrames = stackFrames
                };
                var fileContent = compilationFailure.SourceFileContent
                                                    .Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                foreach (var item in compilationFailure.Messages)
                {
                    var frame = new StackFrame
                    {
                        File = compilationFailure.SourceFilePath,
                        Line = item.StartLine,
                        Function = string.Empty
                    };

                    ReadFrameContent(frame, fileContent, item.StartLine, item.EndLine);
                    frame.ErrorDetails = item.Message;

                    stackFrames.Add(frame);
                }

                model.ErrorDetails.Add(errorDetails);
            }

            var errorPage = new CompilationErrorPage
            {
                Model = model
            };

            return errorPage.ExecuteAsync(context);
        }

        private Task DisplayRuntimeException(HttpContext context, Exception ex)
        {
            var request = context.Request;

            var model = new ErrorPageModel
            {
                Options = _options,
                ErrorDetails = GetErrorDetails(ex).Reverse(),
                Query = request.Query,
                Cookies = request.Cookies,
                Headers = request.Headers
            };

            var errorPage = new ErrorPage(model);
            return errorPage.ExecuteAsync(context);
        }

        private IEnumerable<ErrorDetails> GetErrorDetails(Exception ex)
        {
            for (var scan = ex; scan != null; scan = scan.InnerException)
            {
                yield return new ErrorDetails
                {
                    Error = scan,
                    StackFrames = StackFrames(scan)
                };
            }
        }

        private IEnumerable<StackFrame> StackFrames(Exception ex)
        {
            var stackTrace = ex.StackTrace;
            if (!string.IsNullOrEmpty(stackTrace))
            {
                var heap = new Chunk { Text = stackTrace + Environment.NewLine, End = stackTrace.Length + Environment.NewLine.Length };
                for (var line = heap.Advance(Environment.NewLine); line.HasValue; line = heap.Advance(Environment.NewLine))
                {
                    yield return StackFrame(line);
                }
            }
        }

        private StackFrame StackFrame(Chunk line)
        {
            line.Advance("  at ");
            string function = line.Advance(" in ").ToString();

            //exception message line format differences in .net and mono
            //On .net : at ConsoleApplication.Program.Main(String[] args) in D:\Program.cs:line 16
            //On Mono : at ConsoleApplication.Program.Main(String[] args) in d:\Program.cs:16
            string file = !IsMono ?
                line.Advance(":line ").ToString() :
                line.Advance(":").ToString();

            int lineNumber = line.ToInt32();

            if (string.IsNullOrEmpty(file))
            {
                return GetStackFrame(
                    // Handle stack trace lines like
                    // "--- End of stack trace from previous location where exception from thrown ---"
                    string.IsNullOrEmpty(function) ? line.ToString() : function,
                    file: string.Empty,
                    lineNumber: 0);
            }
            else
            {
                return GetStackFrame(function, file, lineNumber);
            }
        }

        // make it internal to enable unit testing
        internal StackFrame GetStackFrame(string function, string file, int lineNumber)
        {
            var frame = new StackFrame { Function = function, File = file, Line = lineNumber };

            if (string.IsNullOrEmpty(file))
            {
                return frame;
            }

            IEnumerable<string> lines = null;
            if (File.Exists(file))
            {
                lines = File.ReadLines(file);
            }
            else
            {
                // Handle relative paths and embedded files
                var fileInfo = _fileProvider.GetFileInfo(file);
                if (fileInfo.Exists)
                {
                    // ReadLines doesn't accept a stream. Use ReadLines as its more efficient
                    // relative to reading lines via stream reader
                    if (!string.IsNullOrEmpty(fileInfo.PhysicalPath))
                    {
                        lines = File.ReadLines(fileInfo.PhysicalPath);
                    }
                    else
                    {
                        lines = ReadLines(fileInfo);
                    }
                }
            }

            if (lines != null)
            {
                ReadFrameContent(frame, lines, lineNumber, lineNumber);
            }

            return frame;
        }

        // make it internal to enable unit testing
        internal void ReadFrameContent(
            StackFrame frame,
            IEnumerable<string> allLines,
            int errorStartLineNumberInFile,
            int errorEndLineNumberInFile)
        {
            // Get the line boundaries in the file to be read and read all these lines at once into an array.
            var preErrorLineNumberInFile = Math.Max(errorStartLineNumberInFile - _options.SourceCodeLineCount, 1);
            var postErrorLineNumberInFile = errorEndLineNumberInFile + _options.SourceCodeLineCount;
            var codeBlock = allLines
                .Skip(preErrorLineNumberInFile - 1)
                .Take(postErrorLineNumberInFile - preErrorLineNumberInFile + 1)
                .ToArray();

            var numOfErrorLines = (errorEndLineNumberInFile - errorStartLineNumberInFile) + 1;
            var errorStartLineNumberInArray = errorStartLineNumberInFile - preErrorLineNumberInFile;

            frame.PreContextLine = preErrorLineNumberInFile;
            frame.PreContextCode = codeBlock.Take(errorStartLineNumberInArray).ToArray();
            frame.ContextCode = codeBlock
                .Skip(errorStartLineNumberInArray)
                .Take(numOfErrorLines)
                .ToArray();
            frame.PostContextCode = codeBlock
                .Skip(errorStartLineNumberInArray + numOfErrorLines)
                .ToArray();
        }

        private static IEnumerable<string> ReadLines(IFileInfo fileInfo)
        {
            using (var reader = new StreamReader(fileInfo.CreateReadStream()))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        internal class Chunk
        {
            public string Text { get; set; }
            public int Start { get; set; }
            public int End { get; set; }

            public bool HasValue => Text != null;

            public Chunk Advance(string delimiter)
            {
                int indexOf = HasValue ? Text.IndexOf(delimiter, Start, End - Start, StringComparison.Ordinal) : -1;
                if (indexOf < 0)
                {
                    return new Chunk();
                }

                var chunk = new Chunk { Text = Text, Start = Start, End = indexOf };
                Start = indexOf + delimiter.Length;
                return chunk;
            }

            public override string ToString()
            {
                return HasValue ? Text.Substring(Start, End - Start) : string.Empty;
            }

            public int ToInt32()
            {
                int value;
                return HasValue && int.TryParse(
                    Text.Substring(Start, End - Start),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out value) ? value : 0;
            }
        }
    }
}
