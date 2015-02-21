// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Utility type for rendering a <see cref="IView"/> to the response.
    /// </summary>
    public static class ViewExecutor
    {
        private const int BufferSize = 1024;
        private const string ContentType = "text/html; charset=utf-8";

        /// <summary>
        /// Asynchronously renders the specified <paramref name="view"/> to the response body.
        /// </summary>
        /// <param name="view">The <see cref="IView"/> to render.</param>
        /// <param name="actionContext">The <see cref="ActionContext"/> for the current executing action.</param>
        /// <param name="viewData">The <see cref="ViewDataDictionary"/> for the view being rendered.</param>
        /// <returns>A <see cref="Task"/> that represents the asychronous rendering.</returns>
        public static async Task ExecuteAsync([NotNull] IView view,
                                              [NotNull] ActionContext actionContext,
                                              [NotNull] ViewDataDictionary viewData,
                                              string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                contentType = ContentType;
            }

            actionContext.HttpContext.Response.ContentType = contentType;
            var wrappedStream = new StreamWrapper(actionContext.HttpContext.Response.Body);
            var encoding = Encodings.UTF8EncodingWithoutBOM;
            using (var writer = new StreamWriter(wrappedStream, encoding, BufferSize, leaveOpen: true))
            {
                try
                {
                    var viewContext = new ViewContext(actionContext, view, viewData, writer);
                    await view.RenderAsync(viewContext);
                }
                catch
                {
                    // Need to prevent writes/flushes on dispose because the StreamWriter will flush even if
                    // nothing got written. This leads to a response going out on the wire prematurely in case an
                    // exception is being thrown inside the try catch block.
                    wrappedStream.BlockWrites = true;
                    throw;
                }
            }
        }

        private class StreamWrapper : Stream
        {
            private readonly Stream _wrappedStream;

            public StreamWrapper(Stream stream)
            {
                _wrappedStream = stream;
            }

            public bool BlockWrites { get; set; }

            public override bool CanRead
            {
                get { return _wrappedStream.CanRead; }
            }

            public override bool CanSeek
            {
                get { return _wrappedStream.CanSeek; }
            }

            public override bool CanWrite
            {
                get { return _wrappedStream.CanWrite; }
            }

            public override void Flush()
            {
                if (!BlockWrites)
                {
                    _wrappedStream.Flush();
                }
            }

            public override long Length
            {
                get { return _wrappedStream.Length; }
            }

            public override long Position
            {
                get
                {
                    return _wrappedStream.Position;
                }
                set
                {
                    _wrappedStream.Position = value;
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _wrappedStream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (!BlockWrites)
                {
                    _wrappedStream.Write(buffer, offset, count);
                }
            }
        }
    }
}