// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// Represents a deferred write operation in a <see cref="RazorPage"/>.
    /// </summary>
    public class HelperResult : IHtmlContent
    {
        private readonly Func<TextWriter, Task> _asyncAction;

        /// <summary>
        /// Creates a new instance of <see cref="HelperResult"/>.
        /// </summary>
        /// <param name="asyncAction">The asynchronous delegate to invoke when
        /// <see cref="WriteTo(TextWriter, HtmlEncoder)"/> is called.</param>
        /// <remarks>Calls to <see cref="WriteTo(TextWriter, HtmlEncoder)"/> result in a blocking invocation of
        /// <paramref name="asyncAction"/>.</remarks>
        public HelperResult(Func<TextWriter, Task> asyncAction)
        {
            if (asyncAction == null)
            {
                throw new ArgumentNullException(nameof(asyncAction));
            }

            _asyncAction = asyncAction;
        }

        /// <summary>
        /// Gets the asynchronous delegate to invoke when <see cref="WriteTo(TextWriter, HtmlEncoder)"/> is called.
        /// </summary>
        public Func<TextWriter, Task> WriteAction => _asyncAction;

        /// <summary>
        /// Method invoked to produce content from the <see cref="HelperResult"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> instance to write to.</param>
        /// <param name="encoder">The <see cref="HtmlEncoder"/> to encode the content.</param>
        public virtual void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            _asyncAction(writer).GetAwaiter().GetResult();
        }
    }
}
