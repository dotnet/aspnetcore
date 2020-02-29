// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    /// <summary>
    /// An HTML form element in an MVC view.
    /// </summary>
    public class MvcForm : IDisposable
    {
        private readonly ViewContext _viewContext;
        private readonly HtmlEncoder _htmlEncoder;

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="MvcForm"/>.
        /// </summary>
        /// <param name="viewContext">The <see cref="ViewContext"/>.</param>
        /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/>.</param>
        public MvcForm(ViewContext viewContext, HtmlEncoder htmlEncoder)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            if (htmlEncoder == null)
            {
                throw new ArgumentNullException(nameof(htmlEncoder));
            }

            _viewContext = viewContext;
            _htmlEncoder = htmlEncoder;
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
            _viewContext.FormContext = new FormContext();
        }

        private void RenderEndOfFormContent()
        {
            var formContext = _viewContext.FormContext;
            if (!formContext.HasEndOfFormContent)
            {
                return;
            }

            if (_viewContext.Writer is ViewBufferTextWriter viewBufferWriter)
            {
                foreach (var content in formContext.EndOfFormContent)
                {
                    viewBufferWriter.Write(content);
                }
            }
            else
            {
                foreach (var content in formContext.EndOfFormContent)
                {
                    content.WriteTo(_viewContext.Writer, _htmlEncoder);
                }
            }
        }
    }
}
