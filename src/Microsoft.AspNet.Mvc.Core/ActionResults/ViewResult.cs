using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public class ViewResult : ActionResult
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewEngine _viewEngine;

        public ViewResult([NotNull] IServiceProvider serviceProvider, [NotNull] IViewEngine viewEngine)
        {
            _serviceProvider = serviceProvider;
            _viewEngine = viewEngine;
        }

        public string ViewName { get; set; }

        public ViewDataDictionary ViewData { get; set; }

        public override async Task ExecuteResultAsync([NotNull] ActionContext context)
        {
            var viewName = ViewName ?? context.ActionDescriptor.Name;
            var view = FindView(context.RouteValues, viewName);

            using (view as IDisposable)
            {
                context.HttpContext.Response.ContentType = "text/html; charset=utf-8";
                var wrappedStream = new StreamWrapper(context.HttpContext.Response.Body);
                using (var writer = new StreamWriter(wrappedStream, new UTF8Encoding(false), 1024, leaveOpen: true))
                {
                    try
                    {
                        var viewContext = new ViewContext(context, view, ViewData, writer);
                        await view.RenderAsync(viewContext);
                    }
                    catch
                    {
                        // Need to prevent writes/flushes on dispose because the StreamWriter will flush even if nothing
                        // got written. This leads to a response going out on the wire prematurely in case an exception
                        // is being thrown inside the try catch block.
                        wrappedStream.BlockWrites = true;
                        throw;
                    }
                }
            }
        }

        private IView FindView([NotNull] IDictionary<string, object> context, [NotNull] string viewName)
        {
            var result = _viewEngine.FindView(context, viewName);
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

            public bool BlockWrites { get; set;}

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
