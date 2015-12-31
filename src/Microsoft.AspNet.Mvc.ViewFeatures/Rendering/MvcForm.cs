// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using Microsoft.AspNet.Html;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// An HTML form element in an MVC view.
    /// </summary>
    public class MvcForm : IDisposable
    {
        private readonly ViewContext _viewContext;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="MvcForm"/>.
        /// </summary>
        /// <param name="viewContext">The <see cref="ViewContext"/>.</param>
        public MvcForm(ViewContext viewContext)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            _viewContext = viewContext;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                GenerateEndForm();
            }
        }

        /// <summary>
        /// Renders the &lt;/form&gt; end tag to the response.
        /// </summary>
        public void EndForm()
        {
            Dispose();
        }

        /// <summary>
        /// Renders <see cref="ViewFeatures.FormContext.EndOfFormContent"/> and
        /// the &lt;/form&gt;.
        /// </summary>
        protected virtual void GenerateEndForm()
        {
            RenderEndOfFormContent();
            _viewContext.Writer.Write("</form>");
            _viewContext.FormContext = null;
        }

        private void RenderEndOfFormContent()
        {
            var formContext = _viewContext.FormContext;
            if (formContext.HasEndOfFormContent)
            {
                var writer = _viewContext.Writer;
                var htmlWriter = writer as HtmlTextWriter;

                HtmlEncoder htmlEncoder = null;
                if (htmlWriter == null)
                {
                    htmlEncoder = _viewContext.HttpContext.RequestServices.GetRequiredService<HtmlEncoder>();
                }

                foreach (var content in formContext.EndOfFormContent)
                {
                    if (htmlWriter == null)
                    {
                        content.WriteTo(writer, htmlEncoder);
                    }
                    else
                    {
                        htmlWriter.Write(content);
                    }
                }
            }
        }
    }
}
