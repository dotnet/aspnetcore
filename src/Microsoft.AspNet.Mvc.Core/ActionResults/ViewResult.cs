// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class ViewResult : ActionResult
    {
        private const int BufferSize = 1024;

        public string ViewName { get; set; }

        public ViewDataDictionary ViewData { get; set; }

        public IViewEngine ViewEngine { get; set; }

        public override async Task ExecuteResultAsync([NotNull] ActionContext context)
        {
            var viewEngine = ViewEngine ?? context.HttpContext.RequestServices.GetService<ICompositeViewEngine>();

            var viewName = ViewName ?? context.ActionDescriptor.Name;
            var view = FindView(viewEngine, context.RouteData.Values, viewName);

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

        private static IView FindView(
            [NotNull] IViewEngine viewEngine, 
            [NotNull] IDictionary<string, object> context, 
            [NotNull] string viewName)
        {
            var result = viewEngine.FindView(context, viewName);
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
