using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc.Razor
{
    public abstract class RazorView : IView
    {
        public HttpContext Context { get; set; }

        public string Layout { get; set; }

        protected TextWriter Output { get; set; }

        private string BodyContent { get; set; }

        public virtual async Task RenderAsync(ViewContext context, TextWriter writer)
        {
            var contentBuilder = new StringBuilder(1024);
            using (var bodyWriter = new StringWriter(contentBuilder))
            {
                Output = bodyWriter;
                Execute();
            }

            string bodyContent = contentBuilder.ToString();
            if (!String.IsNullOrEmpty(Layout))
            {
                await RenderLayoutAsync(context, writer, bodyContent);
            }
            else
            {
                await writer.WriteAsync(bodyContent);
            }
        }

        private async Task RenderLayoutAsync(ViewContext context, TextWriter writer, string bodyContent)
        {
            var virtualPathFactory = context.ServiceProvider.GetService<IVirtualPathViewFactory>();
            RazorView layoutView = (RazorView)(await virtualPathFactory.CreateInstance(Layout));
            if (layoutView == null)
            {
                string message = String.Format(CultureInfo.CurrentCulture, "The layout view '{0}' could not be located.", Layout);
                throw new InvalidOperationException(message);
            }

            layoutView.BodyContent = bodyContent;
            await layoutView.RenderAsync(context, writer);
        }

        public abstract void Execute();

        public virtual void Write(object value)
        {
            WriteTo(Output, value);
        }

        public virtual void WriteTo(TextWriter writer, object content)
        {
            if (content != null)
            {
                var htmlString = content as HtmlString;
                if (htmlString != null)
                {
                    writer.Write(content.ToString());
                }
                else
                {
#if NET45
                    WebUtility.HtmlEncode(content.ToString(), writer);
#endif
                }
            }
        }

        public void WriteLiteral(object value)
        {
            WriteLiteralTo(Output, value);
        }

        public virtual void WriteLiteralTo(TextWriter writer, object text)
        {
            if (text != null)
            {
                writer.Write(text.ToString());
            }
        }

        protected virtual HtmlString RenderBody()
        {
            if (BodyContent == null)
            {
                throw new InvalidOperationException("RenderBody cannot be called at this point because you're not executing a layout");
            }
            return new HtmlString(BodyContent);
        }
    }
}
