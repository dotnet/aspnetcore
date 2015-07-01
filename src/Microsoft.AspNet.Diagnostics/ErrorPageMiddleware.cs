// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics.Views;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Diagnostics
{
    /// <summary>
    /// Captures synchronous and asynchronous exceptions from the pipeline and generates HTML error responses.
    /// </summary>
    public class ErrorPageMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ErrorPageOptions _options;
        private static readonly bool IsMono = Type.GetType("Mono.Runtime") != null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorPageMiddleware"/> class
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        public ErrorPageMiddleware([NotNull] RequestDelegate next, [NotNull] ErrorPageOptions options)
        {
            _next = next;
            _options = options;
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
                try
                {
                    await DisplayException(context, ex);
                    return;
                }
                catch (Exception)
                {
                    // If there's a Exception while generating the error page, re-throw the original exception.
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

            return string.IsNullOrEmpty(file)
                ? LoadFrame(string.IsNullOrEmpty(function) ? line.ToString() : function, string.Empty, 0)
                : LoadFrame(function, file, lineNumber);
        }

        private StackFrame LoadFrame(string function, string file, int lineNumber)
        {
            var frame = new StackFrame { Function = function, File = file, Line = lineNumber };
            if (File.Exists(file))
            {
                var code = File.ReadLines(file);
                ReadFrameContent(frame, code, lineNumber, lineNumber);
            }
            return frame;
        }

        private void ReadFrameContent(StackFrame frame,
                                      IEnumerable<string> code,
                                      int startLineNumber,
                                      int endLineNumber)
        {
            frame.PreContextLine = Math.Max(startLineNumber - _options.SourceCodeLineCount, 1);
            frame.PreContextCode = code.Skip(frame.PreContextLine - 1).Take(startLineNumber - frame.PreContextLine).ToArray();
            frame.ContextCode = code.Skip(startLineNumber - 1).Take(1 + Math.Max(0, endLineNumber - startLineNumber));
            frame.PostContextCode = code.Skip(startLineNumber).Take(_options.SourceCodeLineCount).ToArray();
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
