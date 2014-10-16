// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Represents the base type for an <see cref="ActionResult"/> that renders a view to the response.
    /// </summary>
    public abstract class ViewResultBase : ActionResult
    {
        private const int BufferSize = 1024;

        public string ViewName { get; set; }

        public ViewDataDictionary ViewData { get; set; }

        public IViewEngine ViewEngine { get; set; }

        public override async Task ExecuteResultAsync([NotNull] ActionContext context)
        {
            var viewEngine = ViewEngine ??
                             context.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();

            var viewName = ViewName ?? context.ActionDescriptor.Name;
            var view = FindViewInternal(viewEngine, context, viewName);

            using (view as IDisposable)
            {
                context.HttpContext.Response.ContentType = "text/html; charset=utf-8";
                var wrappedStream = new StreamWrapper(context.HttpContext.Response.Body);
                var encoding = Encodings.UTF8EncodingWithoutBOM;
                using (var writer = new StreamWriter(wrappedStream, encoding, BufferSize, leaveOpen: true))
                {
                    try
                    {
                        var viewContext = new ViewContext(context, view, ViewData, writer);
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
        }

        /// <summary>
        /// Attempts to locate the view named <paramref name="viewName"/> using the specified 
        /// <paramref name="viewEngine"/>.
        /// </summary>
        /// <param name="viewEngine">The <see cref="IViewEngine"/> used to locate the view.</param>
        /// <param name="context">The <see cref="ActionContext"/> for the executing action.</param>
        /// <param name="viewName">The view to find.</param>
        /// <returns></returns>
        protected abstract ViewEngineResult FindView(IViewEngine viewEngine,
                                                     ActionContext context,
                                                     string viewName);

        private IView FindViewInternal(IViewEngine viewEngine, ActionContext context, string viewName)
        {
            var result = FindView(viewEngine, context, viewName);
            if (!result.Success)
            {
                var locations = string.Empty;
                if (result.SearchedLocations != null)
                {
                    locations = Environment.NewLine +
                        string.Join(Environment.NewLine, result.SearchedLocations);
                }

                throw new InvalidOperationException(Resources.FormatViewEngine_ViewNotFound(viewName, locations));
            }

            return result.View;
        }

        private class StreamWrapper : Stream
        {
            private readonly Stream _wrappedStream;

            public StreamWrapper([NotNull] Stream stream)
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
