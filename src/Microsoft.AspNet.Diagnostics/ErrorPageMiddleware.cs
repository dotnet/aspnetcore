// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

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

namespace Microsoft.AspNet.Diagnostics
{
    /// <summary>
    /// Captures synchronous and asynchronous exceptions from the pipeline and generates HTML error responses.
    /// </summary>
    public class ErrorPageMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ErrorPageOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorPageMiddleware"/> class
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        /// <param name="isDevMode"></param>
        public ErrorPageMiddleware(RequestDelegate next, ErrorPageOptions options, bool isDevMode)
        {
            if (next == null)
            {
                throw new ArgumentNullException("next");
            }
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            if (isDevMode)
            {
                options.SetDefaultVisibility(isVisible: true);
            }
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
        private async Task DisplayException(HttpContext context, Exception ex)
        {
            var request = context.Request;

            ErrorPageModel model = new ErrorPageModel()
            {
                Options = _options,
            };

            if (_options.ShowExceptionDetails)
            {
                model.ErrorDetails = GetErrorDetails(ex, _options.ShowSourceCode).Reverse();
            }
            if (_options.ShowQuery)
            {
                model.Query = request.Query;
            }/* TODO:
            if (_options.ShowCookies)
            {
                model.Cookies = request.Cookies;
            }*/
            if (_options.ShowHeaders)
            {
                model.Headers = request.Headers;
            }/* TODO:
            if (_options.ShowEnvironment)
            {
                model.Environment = context;
            }*/

            var errorPage = new ErrorPage() { Model = model };
            await errorPage.ExecuteAsync(context);
        }

        private IEnumerable<ErrorDetails> GetErrorDetails(Exception ex, bool showSource)
        {
            for (Exception scan = ex; scan != null; scan = scan.InnerException)
            {
                yield return new ErrorDetails
                {
                    Error = scan,
                    StackFrames = StackFrames(scan, showSource)
                };
            }
        }

        private IEnumerable<StackFrame> StackFrames(Exception ex, bool showSource)
        {
            var stackTrace = ex.StackTrace;
            if (!string.IsNullOrEmpty(stackTrace))
            {
                var heap = new Chunk { Text = stackTrace + Environment.NewLine, End = stackTrace.Length + 2 };
                for (Chunk line = heap.Advance(Environment.NewLine); line.HasValue; line = heap.Advance(Environment.NewLine))
                {
                    yield return StackFrame(line, showSource);
                }
            }
        }

        private StackFrame StackFrame(Chunk line, bool showSource)
        {
            line.Advance("  at ");
            string function = line.Advance(" in ").ToString();
            string file = line.Advance(":line ").ToString();
            int lineNumber = line.ToInt32();

            return string.IsNullOrEmpty(file)
                ? LoadFrame(line.ToString(), string.Empty, 0, showSource)
                : LoadFrame(function, file, lineNumber, showSource);
        }

        private StackFrame LoadFrame(string function, string file, int lineNumber, bool showSource)
        {
            var frame = new StackFrame { Function = function, File = file, Line = lineNumber };
            if (showSource && File.Exists(file))
            {
                IEnumerable<string> code = File.ReadLines(file);
                frame.PreContextLine = Math.Max(lineNumber - _options.SourceCodeLineCount, 1);
                frame.PreContextCode = code.Skip(frame.PreContextLine - 1).Take(lineNumber - frame.PreContextLine).ToArray();
                frame.ContextCode = code.Skip(lineNumber - 1).FirstOrDefault();
                frame.PostContextCode = code.Skip(lineNumber).Take(_options.SourceCodeLineCount).ToArray();
            }
            return frame;
        }

        internal class Chunk
        {
            public string Text { get; set; }
            public int Start { get; set; }
            public int End { get; set; }

            public bool HasValue
            {
                get { return Text != null; }
            }

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
                return HasValue && Int32.TryParse(
                    Text.Substring(Start, End - Start),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out value) ? value : 0;
            }
        }
    }
}
