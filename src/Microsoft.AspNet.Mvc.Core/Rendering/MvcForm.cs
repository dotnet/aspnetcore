// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class MvcForm : IDisposable
    {
        private readonly ViewContext _viewContext;
        private bool _disposed;

        public MvcForm([NotNull] ViewContext viewContext)
        {
            _viewContext = viewContext;

            // Push the new FormContext; GenerateEndForm() does the corresponding pop.
            _viewContext.FormContext = new FormContext();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Renders the &lt;/form&gt; end tag to the response.
        /// </summary>
        public void EndForm()
        {
            Dispose(disposing: true);
        }

        protected virtual void GenerateEndForm()
        {
            _viewContext.Writer.Write("</form>");
            _viewContext.FormContext = null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                GenerateEndForm();
            }
        }
    }
}
